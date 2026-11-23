using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 将当前请求的追踪标识、客户端 IP、User-Agent、Referer 与客户端名称/版本写入消息元数据。
/// </summary>
/// <typeparam name="TMessage">由管道处理的消息类型。必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
/// <remarks>
/// 请求上下文不存在时直接调用下一个管道委托，不写入任何元数据。IP 地址优先从代理头读取，
/// 依次尝试 <c>X-Forwarded-For</c>、<c>x-client-ip</c>、<c>CF-Connecting-IP</c>、
/// <c>X-Forwarded-Client-Ip</c>、<c>X-Real-IP</c>、<c>CLIENT-IP</c>，均缺失时使用
/// <see cref="RequestContext.RemoteIpAddress"/>。
/// </remarks>
public class RequestInfoBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private const string RequestIdKey = "RequestId";
	private const string IpAddressKey = "IpAddress";
	private const string UserAgentKey = "UserAgent";
	private const string RefererKey = "Referer";
	private const string AppNameKey = "AppName";
	private const string AppVersionKey = "AppVersion";

	private static readonly string[] IpHeaderKeys =
	[
		"X-Forwarded-For",
		"x-client-ip",
		"CF-Connecting-IP",
		"X-Forwarded-Client-Ip",
		"X-Real-IP",
		"CLIENT-IP"
	];

	private readonly IRequestContextAccessor? _accessor;

	/// <summary>
	/// 初始化 <see cref="RequestInfoBehavior{TMessage,TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">服务提供程序，用于解析可选的 <see cref="IRequestContextAccessor"/>。</param>
	public RequestInfoBehavior(IServiceProvider provider)
	{
		_accessor = provider.GetService<IRequestContextAccessor>();
	}

	/// <summary>
	/// 将请求信息写入消息元数据，然后调用下一个管道行为。
	/// </summary>
	/// <param name="context">要写入元数据的消息。</param>
	/// <param name="next">管道中下一个要调用的行为。</param>
	/// <returns>表示异步操作的任务，其结果为处理后的响应。</returns>
	public Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		var requestContext = _accessor?.Context;

		if (requestContext is null)
		{
			return next(context);
		}

		SetMetadata(context, RequestIdKey, requestContext.TraceIdentifier);
		SetMetadata(context, IpAddressKey, GetRemoteIpAddress(requestContext));
		SetMetadata(context, UserAgentKey, GetHeaderValue(requestContext, "User-Agent"));
		SetMetadata(context, RefererKey, GetHeaderValue(requestContext, "Referer"));
		SetMetadata(context, AppNameKey, GetHeaderValue(requestContext, "x-client-name"));
		SetMetadata(context, AppVersionKey, GetHeaderValue(requestContext, "x-client-version"));

		return next(context);
	}

	private static void SetMetadata(TMessage context, string key, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			context.Metadata[key] = value;
		}
	}

	private static string? GetRemoteIpAddress(RequestContext requestContext)
	{
		var headers = requestContext.Headers;

		if (headers is not null)
		{
			foreach (var headerKey in IpHeaderKeys)
			{
				if (headers.TryGetValue(headerKey, out var ip) && !string.IsNullOrWhiteSpace(ip))
				{
					return ip;
				}
			}
		}

		return requestContext.RemoteIpAddress?.ToString();
	}

	private static string? GetHeaderValue(RequestContext requestContext, string headerName)
	{
		return requestContext.Headers is not null && requestContext.Headers.TryGetValue(headerName, out var value)
			       ? value
			       : null;
	}
}
