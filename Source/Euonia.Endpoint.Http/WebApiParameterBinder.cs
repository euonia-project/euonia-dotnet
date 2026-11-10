using System.Collections;
using System.Globalization;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 参数绑定结果。
/// </summary>
/// <param name="Succeeded">绑定是否成功。</param>
/// <param name="Value">绑定得到的值。</param>
/// <param name="ErrorName">失败参数名称。</param>
/// <param name="ErrorMessage">失败详情，仅用于日志。</param>
/// <param name="IsInternal">是否属于内部错误而非请求输入错误。</param>
internal readonly record struct WebApiBindingResult(bool Succeeded, object? Value, string? ErrorName, string? ErrorMessage, bool IsInternal)
{
	/// <summary>
	/// 创建成功结果。
	/// </summary>
	/// <param name="value">绑定值。</param>
	/// <returns>成功结果。</returns>
	public static WebApiBindingResult Success(object? value) => new(true, value, null, null, false);

	/// <summary>
	/// 创建失败结果。
	/// </summary>
	/// <param name="name">失败参数名称。</param>
	/// <param name="message">失败详情。</param>
	/// <param name="isInternal">是否为内部错误。</param>
	/// <returns>失败结果。</returns>
	public static WebApiBindingResult Failure(string? name, string message, bool isInternal = false) => new(false, null, name, message, isInternal);
}

/// <summary>
/// 把 HTTP 请求数据绑定到接口方法参数。
/// </summary>
internal static class WebApiParameterBinder
{
	/// <summary>
	/// 绑定单个参数。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文。</param>
	/// <param name="descriptor">参数描述符。</param>
	/// <param name="jsonOptions">JSON 序列化选项。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>绑定结果。</returns>
	public static async Task<WebApiBindingResult> BindAsync(HttpContext context, WebApiParameterDescriptor descriptor, JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
	{
		try
		{
			object? value = descriptor.Source switch
			{
				WebApiParameterSource.CancellationToken => context.RequestAborted,
				WebApiParameterSource.Special => GetSpecial(context, descriptor.Parameter.ParameterType),
				WebApiParameterSource.Route => BindRoute(context, descriptor),
				WebApiParameterSource.Query => BindQuery(context, descriptor),
				WebApiParameterSource.Body => await BindBodyAsync(context, descriptor, jsonOptions, cancellationToken).ConfigureAwait(false),
				_ => throw new InvalidOperationException($"Unsupported parameter source '{descriptor.Source}'.")
			};

			return WebApiBindingResult.Success(value);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			var isInternal = exception is not (FormatException or OverflowException or JsonException or InvalidOperationException or ArgumentException);
			return WebApiBindingResult.Failure(descriptor.Name, exception.Message, isInternal);
		}
	}

	private static object? BindRoute(HttpContext context, WebApiParameterDescriptor descriptor)
	{
		if (descriptor.Name != null && TryGetRouteValue(context, descriptor.Name, out var value))
		{
			return descriptor.IsCollection ? ConvertCollection([value], descriptor) : ConvertValue(value, descriptor.ValueType);
		}

		return GetMissing(descriptor);
	}

	private static object? BindQuery(HttpContext context, WebApiParameterDescriptor descriptor)
	{
		if (descriptor.Name != null && TryGetQueryValues(context, descriptor.Name, out var values) && values.Length > 0)
		{
			return descriptor.IsCollection ? ConvertCollection(values, descriptor) : ConvertValue(values[0], descriptor.ValueType);
		}

		return GetMissing(descriptor);
	}

	private static async Task<object?> BindBodyAsync(HttpContext context, WebApiParameterDescriptor descriptor, JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
	{
		if (context.Request.ContentLength == 0)
		{
			return GetMissing(descriptor);
		}

		var contentType = context.Request.ContentType;
		if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"Unsupported content type '{contentType}'; JSON is required.");
		}

		var value = await JsonSerializer.DeserializeAsync(context.Request.Body, descriptor.ValueType, jsonOptions, cancellationToken).ConfigureAwait(false);
		if (value == null && !descriptor.IsOptional)
		{
			throw new InvalidOperationException($"Request body cannot be null for parameter '{descriptor.Name}'.");
		}

		return value;
	}

	private static object? GetMissing(WebApiParameterDescriptor descriptor)
	{
		if (descriptor.IsOptional)
		{
			if (descriptor.IsCollection && descriptor.ElementType != null)
			{
				return CreateEmptyCollection(descriptor.Parameter.ParameterType, descriptor.ElementType);
			}

			return descriptor.DefaultValue;
		}

		throw new InvalidOperationException($"Missing required parameter '{descriptor.Name}'.");
	}

	private static object CreateEmptyCollection(Type type, Type elementType)
	{
		if (type.IsArray)
		{
			return Array.CreateInstance(elementType, 0);
		}

		if (type.IsGenericType)
		{
			var definition = type.GetGenericTypeDefinition();
			if (definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
			{
				return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType))!;
			}

			if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>)
			    || definition == typeof(IList<>) || definition == typeof(IReadOnlyCollection<>)
			    || definition == typeof(IReadOnlyList<>))
			{
				return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
			}
		}

		return Activator.CreateInstance(type)!;
	}

	private static object? GetSpecial(HttpContext context, Type type)
	{
		if (type == typeof(HttpContext)) return context;
		if (type == typeof(HttpRequest)) return context.Request;
		if (type == typeof(HttpResponse)) return context.Response;
		if (type == typeof(ClaimsPrincipal)) return context.User;
		if (type == typeof(Stream)) return context.Request.Body;
		if (type == typeof(PipeReader)) return context.Request.BodyReader;
		throw new InvalidOperationException($"Unsupported special parameter type '{type.FullName}'.");
	}

	private static bool TryGetRouteValue(HttpContext context, string name, out string value)
	{
		if (context.Request.RouteValues.TryGetValue(name, out var raw) && raw != null)
		{
			value = Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
			return true;
		}

		foreach (var pair in context.Request.RouteValues)
		{
			if (pair.Value != null && string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
				return true;
			}
		}

		value = string.Empty;
		return false;
	}

	private static bool TryGetQueryValues(HttpContext context, string name, out string[] values)
	{
		if (context.Request.Query.TryGetValue(name, out var raw) && raw.Count > 0)
		{
			values = raw.Where(value => value != null).Select(value => value!).ToArray();
			return true;
		}

		foreach (var pair in context.Request.Query)
		{
			if (pair.Value.Count > 0 && string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				values = pair.Value.Where(value => value != null).Select(value => value!).ToArray();
				return true;
			}
		}

		values = Array.Empty<string>();
		return false;
	}

	private static object ConvertCollection(IReadOnlyList<string> values, WebApiParameterDescriptor descriptor)
	{
		var elementType = descriptor.ElementType ?? descriptor.ValueType;
		var array = Array.CreateInstance(elementType, values.Count);
		for (var index = 0; index < values.Count; index++)
		{
			array.SetValue(ConvertValue(values[index], elementType), index);
		}

		var targetType = descriptor.Parameter.ParameterType;
		if (targetType.IsArray)
		{
			return array;
		}

		if (targetType.IsGenericType)
		{
			var definition = targetType.GetGenericTypeDefinition();
			if (definition == typeof(HashSet<>) || definition == typeof(ISet<>))
			{
				var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType))!;
				var add = set.GetType().GetMethod("Add")!;
				foreach (var item in array)
				{
					add.Invoke(set, [item]);
				}

				return set;
			}
		}

		var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
		foreach (var item in array)
		{
			list.Add(item);
		}

		return list;
	}

	private static object? ConvertValue(string? value, Type type)
	{
		var targetType = Nullable.GetUnderlyingType(type) ?? type;
		var isNullable = Nullable.GetUnderlyingType(type) != null;

		if (targetType == typeof(string)) return value ?? string.Empty;
		if (string.IsNullOrEmpty(value))
		{
			if (isNullable) return null;
			throw new InvalidOperationException($"Value for '{targetType.Name}' cannot be empty.");
		}

		if (targetType == typeof(object)) return value;
		if (targetType == typeof(char))
		{
			if (value.Length != 1)
			{
				throw new InvalidOperationException($"'{value}' is not a single character.");
			}

			return value[0];
		}
		if (targetType == typeof(bool))
		{
			if (bool.TryParse(value, out var boolean)) return boolean;
			return value switch { "1" => true, "0" => false, _ => throw new InvalidOperationException($"'{value}' is not a valid boolean.") };
		}

		if (targetType == typeof(Guid)) return Guid.Parse(value);
		if (targetType == typeof(DateTime)) return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
		if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
		if (targetType == typeof(DateOnly)) return DateOnly.Parse(value, CultureInfo.InvariantCulture);
		if (targetType == typeof(TimeOnly)) return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
		if (targetType == typeof(Uri)) return new Uri(value, UriKind.RelativeOrAbsolute);
		if (targetType.IsEnum) return Enum.Parse(targetType, value, ignoreCase: true);

		if (targetType == typeof(byte) || targetType == typeof(sbyte)
		    || targetType == typeof(short) || targetType == typeof(ushort)
		    || targetType == typeof(int) || targetType == typeof(uint)
		    || targetType == typeof(long) || targetType == typeof(ulong)
		    || targetType == typeof(float) || targetType == typeof(double)
		    || targetType == typeof(decimal))
		{
			return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		}

		throw new InvalidOperationException($"Cannot bind value '{value}' to type '{targetType.FullName}'.");
	}
}
