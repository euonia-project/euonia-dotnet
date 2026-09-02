using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Hosting;

internal static class RequestContextExtensions
{
	extension(RequestContext)
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
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