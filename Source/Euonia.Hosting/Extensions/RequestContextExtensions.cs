using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 提供由 HTTP 请求创建 <see cref="RequestContext"/> 的扩展方法。
/// </summary>
internal static class RequestContextExtensions
{
	/// <summary>
	/// 为 <see cref="RequestContext"/> 提供基于 HTTP 上下文的创建成员。
	/// </summary>
	extension(RequestContext)
	{
		/// <summary>
		/// 从 <see cref="HttpContext"/> 创建 <see cref="RequestContext"/> 实例。
		/// </summary>
		/// <param name="context">当前 HTTP 请求的上下文。</param>
		/// <returns>包含请求头、连接、用户、请求地址与跟踪标识等信息的 <see cref="RequestContext"/> 实例。</returns>
		public static RequestContext From(HttpContext context)
		{
			return new RequestContext
			{
				Headers = context.Request.Headers.ToDictionary(t => t.Key, t => t.Value.ToString()),
				ConnectionId = context.Connection.Id,
				User = new ClaimsPrincipal(context.User),
				RemotePort = context.Connection.RemotePort,
				RemoteIpAddress = context.Connection.RemoteIpAddress,
				RequestAborted = context.RequestAborted,
				IsWebSocketRequest = context.WebSockets.IsWebSocketRequest,
				TraceIdentifier = context.TraceIdentifier,
				RequestServices = context.RequestServices,
				Method = context.Request.Method,
				Scheme = context.Request.Scheme,
				Host = context.Request.Host.ToString(),
				Path = context.Request.Path.ToString(),
				Protocol = context.Request.Protocol
			};
		}
	}
}