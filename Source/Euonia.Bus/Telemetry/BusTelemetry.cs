using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Bus.Telemetry;

/// <summary>
/// 消息总线的可观测性入口：集中定义 <see cref="ActivitySource"/>、<see cref="Meter"/>
/// 以及各类计数器与耗时直方图。
/// </summary>
/// <remarks>
/// 这些类型位于 .NET 共享框架内，无需额外包引用即可直接接入 OpenTelemetry：
/// <code>
/// builder.AddOpenTelemetry()
///        .WithMetrics(m =&gt; m.AddMeter(BusTelemetry.MeterName))
///        .WithTracing(t =&gt; t.AddSource(BusTelemetry.ActivitySourceName));
/// </code>
/// 无监听者时 <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> 返回 <c>null</c>，
/// 计数器的写入开销也可忽略，因此所有埋点都不改变控制流。
/// </remarks>
internal static class BusTelemetry
{
	/// <summary>
	/// 计量表名称。
	/// </summary>
	public const string MeterName = "Nerosoft.Euonia.Bus";

	/// <summary>
	/// 追踪源名称。
	/// </summary>
	public const string ActivitySourceName = "Nerosoft.Euonia.Bus";

	/// <summary>
	/// 用于创建总线操作 Activity 的追踪源。
	/// </summary>
	public static readonly ActivitySource Source = new(ActivitySourceName);

	private static readonly Meter _meter = new(MeterName);

	/// <summary>
	/// 已发布的（多播）消息数量。
	/// </summary>
	public static readonly Counter<long> Published = _meter.CreateCounter<long>("bus.messages.published", "{message}", "Number of multicast messages published.");

	/// <summary>
	/// 已发送的（单播）消息数量。
	/// </summary>
	public static readonly Counter<long> Sent = _meter.CreateCounter<long>("bus.messages.sent", "{message}", "Number of unicast messages sent.");

	/// <summary>
	/// 已发起的请求-响应调用数量。
	/// </summary>
	public static readonly Counter<long> Called = _meter.CreateCounter<long>("bus.messages.called", "{message}", "Number of request/response calls issued.");

	/// <summary>
	/// 分发失败次数。
	/// </summary>
	public static readonly Counter<long> Failed = _meter.CreateCounter<long>("bus.messages.failed", "{message}", "Number of message dispatch failures.");

	/// <summary>
	/// 消息分发的耗时分布（毫秒）。
	/// </summary>
	public static readonly Histogram<double> Duration = _meter.CreateHistogram<double>("bus.messages.duration", "ms", "Duration of message dispatch in milliseconds.");

	/// <summary>
	/// 发件箱重投递次数。
	/// </summary>
	public static readonly Counter<long> OutboxRetries = _meter.CreateCounter<long>("bus.outbox.retries", "{retry}", "Number of outbox redelivery attempts.");

	/// <summary>
	/// 发件箱转入死信的记录数。
	/// </summary>
	public static readonly Counter<long> OutboxDeadLettered = _meter.CreateCounter<long>("bus.outbox.deadlettered", "{message}", "Number of outbox records moved to the dead letter store.");

	/// <summary>
	/// 收件箱重复执行次数。
	/// </summary>
	public static readonly Counter<long> InboxRetries = _meter.CreateCounter<long>("bus.inbox.retries", "{retry}", "Number of inbox re-execution attempts.");

	/// <summary>
	/// 收件箱转入死信的记录数。
	/// </summary>
	public static readonly Counter<long> InboxDeadLettered = _meter.CreateCounter<long>("bus.inbox.deadlettered", "{message}", "Number of inbox records moved to the dead letter store.");

	/// <summary>
	/// 创建并启动一个总线操作的 Activity；无监听者时返回 <c>null</c>。
	/// </summary>
	/// <param name="name">Activity 名称，例如 <c>bus.publish</c>。</param>
	/// <param name="envelope">消息信封，用于填充标签。</param>
	/// <param name="transport">目标传输器名称；未知时可为 <c>null</c>。</param>
	/// <returns>已启动的 <see cref="Activity"/>；无监听者时为 <c>null</c>。</returns>
	public static Activity StartActivity(string name, IMessageEnvelope envelope, string transport = null)
	{
		var activity = Source.StartActivity(name, ActivityKind.Producer);

		if (activity == null)
		{
			return null;
		}

		SetTag(activity, "messaging.message.id", envelope?.MessageId);
		SetTag(activity, "messaging.channel", envelope?.Channel);
		SetTag(activity, "messaging.correlation_id", envelope?.CorrelationId);
		SetTag(activity, "messaging.conversation_id", envelope?.ConversationId);
		SetTag(activity, "messaging.request_trace_id", envelope?.RequestTraceId);
		SetTag(activity, "messaging.destination.name", transport);
		SetTag(activity, "messaging.message.type", envelope?.Payload?.GetType().FullName);

		return activity;
	}

	/// <summary>
	/// 创建带通道与消息类型标签的计数器标签集。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="messageType">消息运行时类型。</param>
	/// <param name="transport">传输器名称；未知时可为 <c>null</c>。</param>
	/// <returns>可直接用于计数器的标签集。</returns>
	public static TagList Tags(string channel, Type messageType, string transport = null)
	{
		var tags = new TagList
		{
			{ "messaging.channel", channel },
			{ "messaging.message.type", messageType?.FullName },
		};

		if (!string.IsNullOrEmpty(transport))
		{
			tags.Add("messaging.destination.name", transport);
		}

		return tags;
	}

	/// <summary>
	/// 在标签值非空时写入标签，避免产生空值标签。
	/// </summary>
	private static void SetTag(Activity activity, string key, string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			activity.SetTag(key, value);
		}
	}
}
