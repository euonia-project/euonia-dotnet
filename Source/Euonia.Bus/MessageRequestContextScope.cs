using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 在消息处理期间根据消息信封恢复 <see cref="RequestContext"/>，并在释放时清除。
/// </summary>
/// <remarks>
/// 网络传输（RabbitMQ、ActiveMQ、HTTP、gRPC）的发布与处理不属于同一个请求，
/// 处理器流中没有发布方的请求上下文；用户身份、授权信息与跟踪标识只能来自消息信封。
/// 仅当当前流尚无请求上下文时创建，因此不会覆盖 HTTP 入站请求或进程内发布方的真实上下文。
/// </remarks>
internal sealed class MessageRequestContextScope : IDisposable
{
	private readonly DefaultRequestContextAccessor _accessor;
	private readonly RequestContext _inherited;
	private bool _disposed;

	private MessageRequestContextScope(DefaultRequestContextAccessor accessor, RequestContext inherited)
	{
		_accessor = accessor;
		_inherited = inherited;
	}

	/// <summary>
	/// 使用消息携带的身份与跟踪信息创建请求上下文。
	/// </summary>
	/// <param name="scopedProvider">本次处理作用域的服务提供程序。</param>
	/// <param name="message">正在处理的消息上下文。</param>
	/// <param name="overwrite">
	/// <c>true</c> 表示即使当前流已有请求上下文也用消息信封覆盖（重放/重执行应使用原消息身份）；
	/// <c>false</c> 表示仅在当前流没有请求上下文时创建（保留 HTTP 入站请求或进程内发布方的真实上下文）。
	/// </param>
	/// <returns>释放时恢复请求上下文的对象；无需创建时为空操作。</returns>
	public static MessageRequestContextScope Establish(IServiceProvider scopedProvider, IMessageContext message, bool overwrite = false)
	{
		ArgumentNullException.ThrowIfNull(scopedProvider);

		if (message == null)
		{
			return new MessageRequestContextScope(null, null);
		}

		var accessor = scopedProvider.GetService<DefaultRequestContextAccessor>();
		if (accessor == null)
		{
			return new MessageRequestContextScope(null, null);
		}

		var inherited = accessor.Context;

		// 进程内传输按消息携带发布方快照：快照为空时也要覆盖，
		// 否则后台泵会沿用启动时继承自另一条消息的上下文。
		// 快照为空时回退到信封，保留发件箱重投等场景携带的原消息身份。
		if (message is MessageContext publisherContext && publisherContext.PublisherContext.Captured)
		{
			var snapshot = publisherContext.PublisherContext.Context;
			accessor.Context = snapshot != null
				                   ? Clone(snapshot, scopedProvider)
				                   : CreateFromEnvelope(message, scopedProvider);
			return new MessageRequestContextScope(accessor, inherited);
		}

		if (inherited != null && !overwrite)
		{
			return new MessageRequestContextScope(null, null);
		}

		var restored = CreateFromEnvelope(message, scopedProvider);
		if (restored == null)
		{
			return new MessageRequestContextScope(null, null);
		}

		accessor.Context = restored;
		return new MessageRequestContextScope(accessor, inherited);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed || _accessor == null)
		{
			return;
		}

		_disposed = true;
		_accessor.Context = _inherited;
	}

	/// <summary>
	/// 使用消息信封携带的身份与跟踪信息创建请求上下文。
	/// </summary>
	/// <returns>创建后的上下文；信封未携带任何可用信息时返回 <c>null</c>。</returns>
	private static RequestContext CreateFromEnvelope(IMessageContext message, IServiceProvider scopedProvider)
	{
		var authorization = message.Authorization;
		var user = message.User as ClaimsPrincipal;
		var traceId = message.RequestTraceId;

		if (string.IsNullOrWhiteSpace(authorization) && string.IsNullOrWhiteSpace(traceId) && user == null)
		{
			return null;
		}

		return new RequestContext
		{
			TraceIdentifier = traceId,
			User = user,
			RequestServices = scopedProvider,
			Headers = string.IsNullOrWhiteSpace(authorization)
				          ? null
				          : new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = authorization },
		};
	}

	/// <summary>
	/// 复制发布方上下文快照，并将服务提供程序替换为本次处理作用域，避免沿用已释放的请求作用域。
	/// </summary>
	private static RequestContext Clone(RequestContext source, IServiceProvider requestServices)
	{
		return new RequestContext
		{
			ConnectionId = source.ConnectionId,
			RemoteIpAddress = source.RemoteIpAddress,
			RemotePort = source.RemotePort,
			IsWebSocketRequest = source.IsWebSocketRequest,
			User = source.User,
			Headers = source.Headers == null ? null : new Dictionary<string, string>(source.Headers),
			Method = source.Method,
			Scheme = source.Scheme,
			Host = source.Host,
			Protocol = source.Protocol,
			Path = source.Path,
			RequestAborted = source.RequestAborted,
			TraceIdentifier = source.TraceIdentifier,
			RequestServices = requestServices,
		};
	}
}
