namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息发布时捕获的发布方请求上下文快照。
/// </summary>
/// <param name="Captured">
/// 是否执行过捕获。捕获流程中发布方没有请求上下文时为 <c>true</c>，且 <paramref name="Context"/> 为 <c>null</c>。
/// </param>
/// <param name="Context">发布方的请求上下文；未捕获或发布方没有上下文时为 <c>null</c>。</param>
/// <remarks>
/// 进程内传输在发布时捕获该快照，随消息一起入队，使处理程序获得对应消息的发布方上下文，
/// 而不是后台泵启动时继承的、可能属于另一条消息的上下文。
/// 快照复制发布方请求的全部信息字段，但不包含 <see cref="RequestContext.RequestServices"/>：
/// 该属性指向发布方可能已释放的请求作用域，处理时由框架绑定到本次处理作用域。
/// 网络传输不填充该快照，处理侧从消息信封恢复可跨进程传递的身份与跟踪信息。
/// </remarks>
public readonly record struct PublisherContextSnapshot(bool Captured, RequestContext Context)
{
	/// <summary>
	/// 未捕获发布方上下文（网络传输或手动构造的消息）。
	/// </summary>
	public static PublisherContextSnapshot NotCaptured => default;

	/// <summary>
	/// 捕获发布方请求上下文。快照在调用时复制请求信息，之后发布方的修改不会影响该快照。
	/// </summary>
	/// <param name="context">发布方的请求上下文；发布方没有上下文时为 <c>null</c>。</param>
	/// <returns>已标记为捕获的快照。</returns>
	public static PublisherContextSnapshot Capture(RequestContext context)
	{
		return new PublisherContextSnapshot(true, Copy(context));
	}

	/// <summary>
	/// 复制请求上下文的信息字段；<see cref="RequestContext.RequestServices"/> 不属于请求信息，不参与复制。
	/// </summary>
	private static RequestContext Copy(RequestContext source)
	{
		if (source == null)
		{
			return null;
		}

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
		};
	}
}
