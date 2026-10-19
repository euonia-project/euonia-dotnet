namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 描述一次消息处理执行环境的不可变描述符，承载于 <see cref="MessageContext"/>。
/// </summary>
/// <remarks>
/// 描述符只回答两个问题：服务提供程序从何处获取（新建作用域或复用调用方作用域），
/// 以及处理期间的 <see cref="RequestContext"/> 从何处获取（信封、发布方快照或不修改）。
/// 传输层只负责填写描述符；作用域的获取与请求上下文的安装/恢复统一由处理器上下文完成。
/// </remarks>
public readonly record struct MessageExecutionDescriptor
{
	private MessageExecutionDescriptor(
		MessageProviderLease providerLease,
		MessageRequestContextSource requestContextSource,
		IServiceProvider borrowedProvider,
		RequestContext publisherContext)
	{
		ProviderLease = providerLease;
		RequestContextSource = requestContextSource;
		BorrowedProvider = borrowedProvider;
		PublisherContext = publisherContext;
	}

	/// <summary>
	/// 获取服务提供程序的获取方式。
	/// </summary>
	public MessageProviderLease ProviderLease { get; }

	/// <summary>
	/// 获取处理期间请求上下文的来源。
	/// </summary>
	public MessageRequestContextSource RequestContextSource { get; }

	/// <summary>
	/// 获取复用的调用方服务提供程序；<see cref="MessageProviderLease.OwnScope"/> 时为 <c>null</c>。
	/// </summary>
	public IServiceProvider BorrowedProvider { get; }

	/// <summary>
	/// 获取发布时捕获的发布方请求上下文副本；非 <see cref="MessageRequestContextSource.PublisherSnapshot"/> 时为 <c>null</c>。
	/// </summary>
	public RequestContext PublisherContext { get; }

	/// <summary>
	/// 手动调用或无法获取请求作用域时的默认值：新建作用域；仅当当前流没有上下文时从信封恢复。
	/// </summary>
	public static MessageExecutionDescriptor RemoteInbound =>
		new(MessageProviderLease.OwnScope, MessageRequestContextSource.Envelope, null, null);

	/// <summary>
	/// HTTP/gRPC 入站请求：复用请求作用域；已有真实请求上下文时保留，否则从信封恢复。
	/// </summary>
	/// <param name="requestServices">入站请求的作用域服务提供程序；为 <c>null</c> 时退化为 <see cref="RemoteInbound"/>。</param>
	/// <returns>对应的执行描述符。</returns>
	public static MessageExecutionDescriptor RemoteInboundBorrowed(IServiceProvider requestServices)
	{
		return requestServices == null
			       ? RemoteInbound
			       : new MessageExecutionDescriptor(MessageProviderLease.Borrow, MessageRequestContextSource.Envelope, requestServices, null);
	}

	/// <summary>
	/// 跨进程消费者：新建作用域；始终以信封为准，不沿用当前流的上下文。
	/// </summary>
	public static MessageExecutionDescriptor DistributedInbound =>
		new(MessageProviderLease.OwnScope, MessageRequestContextSource.EnvelopeOverride, null, null);

	/// <summary>
	/// 死信重放与收件箱重执行：新建作用域；以原消息身份覆盖触发方的上下文。
	/// </summary>
	public static MessageExecutionDescriptor Replay =>
		new(MessageProviderLease.OwnScope, MessageRequestContextSource.EnvelopeOverride, null, null);

	/// <summary>
	/// 后台出站重投递：新建作用域；不修改当前请求上下文。
	/// </summary>
	public static MessageExecutionDescriptor OutboxDispatch =>
		new(MessageProviderLease.OwnScope, MessageRequestContextSource.Inherit, null, null);

	/// <summary>
	/// 进程内单播/请求：调用方等待处理完成时复用其显式绑定的作用域，并保留发布方完整请求上下文。
	/// </summary>
	/// <param name="provider">调用方显式绑定的服务提供程序；为 <c>null</c> 时新建作用域。</param>
	/// <param name="publisherContext">发布方请求上下文；为 <c>null</c> 时处理侧回退到消息信封。</param>
	/// <returns>对应的执行描述符。</returns>
	public static MessageExecutionDescriptor InProcessCall(IServiceProvider provider, RequestContext publisherContext)
	{
		return new MessageExecutionDescriptor(
			provider == null ? MessageProviderLease.OwnScope : MessageProviderLease.Borrow,
			MessageRequestContextSource.PublisherSnapshot,
			provider,
			Copy(publisherContext));
	}

	/// <summary>
	/// 进程内多播发布：新建消息专用作用域，但保留发布方完整请求上下文。
	/// </summary>
	/// <param name="publisherContext">发布方请求上下文；为 <c>null</c> 时处理侧回退到消息信封。</param>
	/// <returns>对应的执行描述符。</returns>
	public static MessageExecutionDescriptor InProcessPublish(RequestContext publisherContext)
	{
		return new MessageExecutionDescriptor(
			MessageProviderLease.OwnScope,
			MessageRequestContextSource.PublisherSnapshot,
			null,
			Copy(publisherContext));
	}

	/// <summary>
	/// 为本次执行创建独立的请求上下文副本，并把 <see cref="RequestContext.RequestServices"/> 绑定到执行作用域。
	/// </summary>
	/// <param name="requestServices">本次执行作用域的服务提供程序。</param>
	/// <returns>上下文副本；没有发布方上下文时返回 <c>null</c>。</returns>
	internal RequestContext CreateRequestContext(IServiceProvider requestServices)
	{
		if (PublisherContext == null)
		{
			return null;
		}

		var context = Copy(PublisherContext);
		context.RequestServices = requestServices;
		return context;
	}

	/// <summary>
	/// 复制请求上下文的信息字段；<see cref="RequestContext.RequestServices"/> 属于运行时定位器，不参与复制。
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
