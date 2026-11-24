using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 将函数上下文中的 HTTP 上下文注入 <see cref="IHttpContextAccessor"/>，并同步到请求上下文访问器。
/// </summary>
internal sealed class HttpContextAccessorMiddleware : IFunctionsWorkerMiddleware
{
	/// <inheritdoc />
	public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		var httpContext = context.GetHttpContext();

		if (httpContext is not null)
		{
			context.InstanceServices.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

			var user = context.InstanceServices.GetService<UserPrincipal>();

			if (user is not null)
			{
				context.Items["User"] = user;
			}

			var accessor = context.InstanceServices.GetService<DefaultRequestContextAccessor>();

			if (accessor is not null)
			{
				accessor.Context = new RequestContext
				{
					Headers = httpContext.Request.Headers.ToDictionary(item => item.Key, item => item.Value.ToString()),
					ConnectionId = httpContext.Connection.Id,
					User = new ClaimsPrincipal(httpContext.User),
					RemotePort = httpContext.Connection.RemotePort,
					RemoteIpAddress = httpContext.Connection.RemoteIpAddress,
					RequestAborted = httpContext.RequestAborted,
					IsWebSocketRequest = httpContext.WebSockets.IsWebSocketRequest,
					TraceIdentifier = httpContext.TraceIdentifier,
					RequestServices = httpContext.RequestServices
				};
			}
		}

		return next(context);
	}
}
