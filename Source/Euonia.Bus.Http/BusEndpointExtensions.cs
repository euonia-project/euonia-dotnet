using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 提供在 ASP.NET Core 应用中挂载 HTTP 消息总线端点的扩展方法。
/// </summary>
public static class BusEndpointExtensions
{
	/// <summary>
	/// 映射消息总线请求-响应（CallAsync）端点。
	/// 接收经 <see cref="HttpTransporter"/> 发送的请求负载并调用 <see cref="IHandlerContext"/> 处理消息。
	/// 路由默认为 <see cref="HttpBusOptions.Route"/>（缺省 <c>/bus/call</c>）。
	/// </summary>
	/// <param name="endpoints">用于构建路由端点的构建器。</param>
	/// <returns>原始端点构建器，以支持链式调用。</returns>
	public static IEndpointRouteBuilder MapBusEndpoint(this IEndpointRouteBuilder endpoints)
	{
		var route = ResolveRoute(endpoints);

		// 提前构造 IHandlerContext（DefaultHandlerContext 仅在构造时订阅渠道注册事件），
		// 使应用启动阶段（模块初始化）的 RegisterChannel 注册不会被遗漏。
		_ = endpoints.ServiceProvider?.GetService<IHandlerContext>();

		endpoints.MapPost(route, async (HttpContext context) =>
		{
			var options = context.RequestServices.GetRequiredService<IOptions<HttpBusOptions>>().Value;
			var serializer = context.RequestServices.GetKeyedService<IMessageSerializer>(options.SerializerProvider);
			if (serializer == null)
			{
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsync($"Message serializer '{options.SerializerProvider}' is not registered.", context.RequestAborted);
				return;
			}

			var handler = context.RequestServices.GetRequiredService<IHandlerContext>();

			using var reader = new StreamReader(context.Request.Body ?? Stream.Null);
			var body = await reader.ReadToEndAsync(context.RequestAborted);

			string reply;
			try
			{
				reply = await RemoteReceiver.ReceiveAsync(serializer, handler, body, context.RequestAborted);
			}
			catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
			{
				// 客户端已断开，无需也无法再写入响应。
				return;
			}
			catch (Exception exception)
			{
				// 端点级异常边界：避免向上抛出导致空 500，改为返回结构化的失败回复。
				reply = serializer.Serialize(RemoteReply<object>.Failure(RemoteError.Create(exception)));
			}

			context.Response.ContentType = "application/json; charset=utf-8";
			await context.Response.WriteAsync(reply, context.RequestAborted);
		});

		return endpoints;
	}

	private static string ResolveRoute(IEndpointRouteBuilder endpoints)
	{
		var options = endpoints.ServiceProvider?.GetService<IOptions<HttpBusOptions>>()?.Value;
		return string.IsNullOrWhiteSpace(options?.Route) ? "/bus/call" : options.Route;
	}
}