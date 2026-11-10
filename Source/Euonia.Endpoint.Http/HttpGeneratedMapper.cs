using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Endpoint;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 生成代码调用的 HTTP 映射入口。
/// </summary>
public static class HttpGeneratedMapper
{
	private static int _counter;

	/// <summary>
	/// 映射一个生成端点。
	/// </summary>
	/// <param name="endpoints">端点构建器。</param>
	/// <param name="serviceType">服务接口类型。</param>
	/// <param name="methodName">接口方法名称。</param>
	/// <param name="prefix">路由前缀；为空时使用默认前缀。</param>
	/// <param name="route">路由模板；为空时使用默认路由。</param>
	/// <param name="methods">HTTP 方法集合。</param>
	/// <param name="parameterTypes">方法参数类型，用于精确匹配重载。</param>
	/// <param name="handler">强类型请求委托。</param>
	/// <returns>端点构建器；开关关闭或服务未注册时返回 <c>null</c>。</returns>
	/// <exception cref="InvalidOperationException">当服务提供程序缺失、方法签名不匹配、注册表缺失或路由冲突且开启严格模式时抛出。</exception>
	public static IEndpointConventionBuilder? Map(
		IEndpointRouteBuilder endpoints,
		Type serviceType,
		string methodName,
		string prefix,
		string route,
		string[] methods,
		Type[] parameterTypes,
		Delegate handler)
	{
		var provider = endpoints.ServiceProvider ?? throw new InvalidOperationException("Missing service provider.");
		var options = provider.GetService<IOptions<HttpExposureOptions>>()?.Value ?? new HttpExposureOptions();
		if (!options.EnableExposeApplicationServiceAsWebApi)
		{
			return null;
		}

		if (!HttpExposureScope.IsIncluded(serviceType, options))
		{
			return null;
		}

		var method = serviceType.GetMethods().FirstOrDefault(item =>
			             item.Name == methodName
			             && item.GetParameters().Length == parameterTypes.Length
			             && item.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes))
		             ?? throw new InvalidOperationException($"Method '{methodName}' with the expected signature was not found on '{serviceType.FullName}'.");
		var attribute = new ExposeWebApiAttribute { Prefix = prefix, Route = route, Method = methods };
		var metadata = provider.GetService<HttpMetadataRegistry>()?.GetMetadata(serviceType, method);
		WebApiActionDescriptor descriptor;
		try
		{
			descriptor = new WebApiActionDescriptorBuilder(options).Build(new ExposedWebApiMethod(serviceType, method, attribute), metadata);
		}
		catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
		{
			var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Nerosoft.Euonia.Endpoint.Http");
			logger?.LogWarning(exception, "Skipped generated-incompatible HTTP endpoint {Service}.{Method}.", serviceType.FullName, method.Name);
			return null;
		}

		var isService = provider.GetService<IServiceProviderIsService>();
		if (isService?.IsService(serviceType) == false)
		{
			var message = $"Service '{serviceType.FullName}' is marked with ExposeWebApiAttribute but is not registered.";
			if (options.ThrowOnUnregisteredService) throw new InvalidOperationException(message);
			return null;
		}

		var registry = provider.GetService<HttpRegistry>() ?? throw new InvalidOperationException("HttpRegistry is not registered. Call AddDynamicHttpEndpoint first.");
		var reservation = registry.TryReserve(descriptor.RoutePattern, descriptor.HttpMethods, generated: true);
		if (reservation != HttpReservation.Reserved)
		{
			var message = $"HTTP endpoint '{descriptor.RoutePattern}' for '{serviceType.FullName}.{methodName}' conflicts with an existing endpoint; it may already be mapped by a previous MapDynamicHttpEndpoint call.";
			if (options.ThrowOnRouteConflict) throw new InvalidOperationException(message);
			return null;
		}

		var builder = endpoints.MapMethods(descriptor.RoutePattern, descriptor.HttpMethods, handler);
		builder.WithDisplayName($"{serviceType.Name}.{method.Name}");
		builder.WithName($"{serviceType.FullName ?? serviceType.Name}.{method.Name}.{string.Join("_", descriptor.HttpMethods)}.generated.{Interlocked.Increment(ref _counter)}");
		builder.WithTags(serviceType.Name);
		if (descriptor.Metadata.Count > 0)
		{
			builder.WithMetadata(descriptor.Metadata.ToArray());
		}

		if (options.RequireAuthorization)
		{
			builder.RequireAuthorization();
		}

		return builder;
	}

	/// <summary>
	/// 读取生成端点的 JSON 请求体，并统一空 body、非 JSON Content-Type、非法 JSON 与 <c>null</c> 的失败语义。
	/// </summary>
	/// <typeparam name="T">请求体类型。</typeparam>
	/// <param name="context">当前 HTTP 上下文。</param>
	/// <param name="required">请求体是否必填。</param>
	/// <returns>请求体绑定结果。</returns>
	public static async Task<HttpBodyBindingResult<T>> ReadBodyAsync<T>(HttpContext context, bool required)
	{
		if (context.Request.ContentLength == 0)
		{
			return new HttpBodyBindingResult<T>(!required, default);
		}

		var contentType = context.Request.ContentType;
		if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
		{
			return new HttpBodyBindingResult<T>(false, default);
		}

		try
		{
			var value = await context.Request.ReadFromJsonAsync<T>(context.RequestAborted).ConfigureAwait(false);
			if (value is null)
			{
				return new HttpBodyBindingResult<T>(!required, default);
			}

			return new HttpBodyBindingResult<T>(true, value);
		}
		catch (JsonException)
		{
			return new HttpBodyBindingResult<T>(false, default);
		}
		catch (InvalidOperationException)
		{
			return new HttpBodyBindingResult<T>(false, default);
		}
	}
}
