using System.Collections;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Nerosoft.Euonia.Endpoint;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 根据暴露特性构建 <see cref="WebApiActionDescriptor"/>。
/// </summary>
internal sealed class WebApiActionDescriptorBuilder
{
	private readonly HttpExposureOptions _options;

	/// <summary>
	/// 初始化 <see cref="WebApiActionDescriptorBuilder"/> 的新实例。
	/// </summary>
	/// <param name="options">HTTP 端点暴露选项。</param>
	public WebApiActionDescriptorBuilder(HttpExposureOptions options)
	{
		_options = options;
	}

	/// <summary>
	/// 构建端点描述符。
	/// </summary>
	/// <param name="exposed">扫描到的待暴露方法。</param>
	/// <param name="extraMetadata">来自实现类/方法的附加认证元数据。</param>
	/// <returns>端点描述符。</returns>
	/// <exception cref="InvalidOperationException">路由无效、参数数量不支持或多复杂参数时抛出。</exception>
	/// <exception cref="NotSupportedException">文件上传或流式返回等不支持的签名时抛出。</exception>
	public WebApiActionDescriptor Build(ExposedWebApiMethod exposed, IReadOnlyList<object>? extraMetadata = null)
	{
		var attribute = exposed.Attribute;
		var description = $"{exposed.ServiceType.FullName}.{exposed.Method.Name}";

		var prefix = string.IsNullOrWhiteSpace(attribute.Prefix)
			? WebApiNaming.CombinePath(_options.DefaultRoutePrefix, WebApiNaming.GetDefaultServiceName(exposed.ServiceType))
			: attribute.Prefix;
		var route = string.IsNullOrWhiteSpace(attribute.Route)
			? WebApiNaming.GetDefaultRouteName(exposed.Method)
			: attribute.Route;

		var routePattern = WebApiNaming.CombinePath(prefix, route).TrimStart('/');
		if (string.IsNullOrWhiteSpace(routePattern))
		{
			throw new InvalidOperationException($"Web API route cannot be empty for '{description}'.");
		}

		RoutePattern routePatternModel;
		try
		{
			routePatternModel = RoutePatternFactory.Parse(routePattern);
		}
		catch (Exception exception) when (exception is RoutePatternException or ArgumentException)
		{
			throw new InvalidOperationException($"Invalid Web API route '{routePattern}' for '{description}'.", exception);
		}

		var httpMethods = WebApiHttpMethods.Normalize(attribute.Method, description);
		var routeParameters = routePatternModel.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var parameters = BuildParameters(exposed, routeParameters);
		var (returnsVoid, resultType) = GetReturnType(exposed.Method);

		return new WebApiActionDescriptor
		{
			ServiceType = exposed.ServiceType,
			Method = exposed.Method,
			RoutePattern = routePattern,
			HttpMethods = httpMethods,
			Parameters = parameters,
			ReturnsVoid = returnsVoid,
			ResultType = resultType,
			Metadata = BuildMetadata(exposed.ServiceType, exposed.Method, extraMetadata)
		};
	}

	private static IReadOnlyList<WebApiParameterDescriptor> BuildParameters(ExposedWebApiMethod exposed, HashSet<string> routeParameters)
	{
		var result = new List<WebApiParameterDescriptor>();
		var bodyCount = 0;

		foreach (var parameter in exposed.Method.GetParameters())
		{
			if (parameter.ParameterType == typeof(IFormFile) || parameter.ParameterType == typeof(IFormFileCollection))
			{
				throw new NotSupportedException($"File upload parameter '{parameter.Name}' is not supported.");
			}

			if (parameter.ParameterType.IsByRefLike)
			{
				throw new NotSupportedException($"Ref struct parameter '{parameter.Name}' is not supported.");
			}

			var source = GetParameterSource(parameter, routeParameters);
			if (source == WebApiParameterSource.Body)
			{
				bodyCount++;
				if (bodyCount > 1)
				{
					throw new InvalidOperationException($"Method '{exposed.ServiceType.FullName}.{exposed.Method.Name}' has more than one complex parameter.");
				}
			}

			var (isCollection, elementType) = GetCollectionElementType(parameter.ParameterType);
			var valueType = isCollection && elementType != null
				? elementType
				: Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

			var isOptional = source == WebApiParameterSource.Body
				? parameter.HasDefaultValue
				  || Nullable.GetUnderlyingType(parameter.ParameterType) != null
				  || IsNullableReference(parameter)
				: IsOptional(parameter);

			result.Add(new WebApiParameterDescriptor
			{
				Parameter = parameter,
				Source = source,
				IsOptional = isOptional,
				DefaultValue = parameter.HasDefaultValue
					? parameter.DefaultValue is DBNull ? null : parameter.DefaultValue
					: null,
				IsCollection = isCollection,
				ElementType = elementType,
				ValueType = valueType,
				CollectionFactory = isCollection && elementType != null
					? CreateCollectionFactory(parameter.ParameterType, elementType)
					: null
			});
		}

		return result;
	}

	private static WebApiParameterSource GetParameterSource(ParameterInfo parameter, HashSet<string> routeParameters)
	{
		var type = parameter.ParameterType;
		if (type == typeof(CancellationToken))
		{
			return WebApiParameterSource.CancellationToken;
		}

		if (type == typeof(HttpContext) || type == typeof(HttpRequest) || type == typeof(HttpResponse)
		    || type == typeof(System.Security.Claims.ClaimsPrincipal) || type == typeof(Stream) || type == typeof(PipeReader))
		{
			return WebApiParameterSource.Special;
		}

		if (parameter.Name != null && routeParameters.Contains(parameter.Name))
		{
			return WebApiParameterSource.Route;
		}

		if (type == typeof(byte[]))
		{
			return WebApiParameterSource.Body;
		}

		var (isCollection, elementType) = GetCollectionElementType(type);
		if (isCollection && elementType != null && IsSimpleType(elementType))
		{
			return WebApiParameterSource.Query;
		}

		return IsSimpleType(type) ? WebApiParameterSource.Query : WebApiParameterSource.Body;
	}

	private static bool IsNullableReference(ParameterInfo parameter)
	{
		return !parameter.ParameterType.IsValueType
		       && new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable;
	}

	private static bool IsOptional(ParameterInfo parameter)
	{
		return parameter.HasDefaultValue
		       || Nullable.GetUnderlyingType(parameter.ParameterType) != null
		       || !parameter.ParameterType.IsValueType;
	}

	private static bool IsSimpleType(Type type)
	{
		var target = Nullable.GetUnderlyingType(type) ?? type;
		if (target.IsEnum)
		{
			return true;
		}

		if (target == typeof(string) || target == typeof(char) || target == typeof(Guid)
		    || target == typeof(DateTime) || target == typeof(DateTimeOffset)
		    || target == typeof(TimeSpan) || target == typeof(DateOnly) || target == typeof(TimeOnly)
		    || target == typeof(Uri) || target == typeof(IntPtr) || target == typeof(UIntPtr))
		{
			return true;
		}

		return Type.GetTypeCode(target) != TypeCode.Object;
	}

	private static Func<Array, object>? CreateCollectionFactory(Type targetType, Type elementType)
	{
		if (targetType.IsArray)
		{
			return static array => array;
		}

		if (!targetType.IsGenericType)
		{
			return null;
		}

		var definition = targetType.GetGenericTypeDefinition();
		if (definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
		{
			var setType = typeof(HashSet<>).MakeGenericType(elementType);
			var add = setType.GetMethod(nameof(HashSet<int>.Add))!;
			return array =>
			{
				var set = Activator.CreateInstance(setType)!;
				foreach (var item in array)
				{
					add.Invoke(set, new[] { item });
				}

				return set;
			};
		}

		if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>)
		    || definition == typeof(IList<>) || definition == typeof(IReadOnlyCollection<>)
		    || definition == typeof(IReadOnlyList<>) || definition == typeof(List<>))
		{
			var listType = typeof(List<>).MakeGenericType(elementType);
			return array =>
			{
				var list = (IList)Activator.CreateInstance(listType)!;
				foreach (var item in array)
				{
					list.Add(item);
				}

				return list;
			};
		}

		return null;
	}

	private static (bool IsCollection, Type? ElementType) GetCollectionElementType(Type type)
	{
		if (type == typeof(string) || type == typeof(byte[]))
		{
			return (false, null);
		}

		if (type.IsArray)
		{
			return (true, type.GetElementType());
		}

		if (!type.IsGenericType)
		{
			return (false, null);
		}

		var definition = type.GetGenericTypeDefinition();
		if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>)
		    || definition == typeof(IList<>) || definition == typeof(IReadOnlyCollection<>)
		    || definition == typeof(IReadOnlyList<>) || definition == typeof(List<>)
		    || definition == typeof(HashSet<>) || definition == typeof(ISet<>)
		    || definition == typeof(IReadOnlySet<>))
		{
			return (true, type.GetGenericArguments()[0]);
		}

		return (false, null);
	}

	private static (bool ReturnsVoid, Type? ResultType) GetReturnType(MethodInfo method)
	{
		var returnType = method.ReturnType;
		if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
		{
			return (true, null);
		}

		if (returnType.IsGenericType)
		{
			var definition = returnType.GetGenericTypeDefinition();
			if (definition == typeof(Task<>))
			{
				var resultType = returnType.GetGenericArguments()[0];
				ThrowIfAsyncEnumerable(resultType, method);
				return (false, resultType);
			}

			if (definition == typeof(ValueTask<>))
			{
				var resultType = returnType.GetGenericArguments()[0];
				ThrowIfAsyncEnumerable(resultType, method);
				return (false, resultType);
			}
		}

		ThrowIfAsyncEnumerable(returnType, method);
		return (false, returnType);
	}

	private static void ThrowIfAsyncEnumerable(Type type, MethodInfo method)
	{
		if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
		    || type.GetInterfaces().Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)))
		{
			throw new NotSupportedException($"Streaming return type is not supported by HTTP exposure: '{method.DeclaringType?.FullName}.{method.Name}'.");
		}
	}

	private static IReadOnlyList<object> BuildMetadata(Type serviceType, MethodInfo method, IReadOnlyList<object>? extraMetadata)
	{
		var metadata = new List<object>();
		metadata.AddRange(serviceType.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
		metadata.AddRange(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
		metadata.AddRange(serviceType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
		metadata.AddRange(method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
		if (extraMetadata is { Count: > 0 })
		{
			metadata.AddRange(extraMetadata);
		}

		return metadata;
	}
}
