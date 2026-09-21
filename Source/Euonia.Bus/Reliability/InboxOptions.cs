namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件箱（Inbox）模式的相关配置选项。
/// </summary>
/// <remarks>
/// 收件箱模式通过在消费端对已接收消息进行去重与状态追踪，
/// 保证消息在 at-least-once 投递语义下不会被重复处理。
/// </remarks>
public class InboxOptions
{
	/// <summary>
	/// 获取或设置是否启用收件箱模式（全局开关）。
	/// </summary>
	/// <remarks>
	/// 默认关闭。启用后，多播消息在消费端会先写入收件箱存储做去重，
	/// 已存在的消息标识符将被跳过；处理器执行结果（成功 / 失败）逐处理器记录。
	/// <para>
	/// 这是唯一生效的开关：<see cref="ExtendableOptions.UseInbox"/> 仅为发送侧标记，
	/// 内置传输器不会将其透传到消费端，因此单独设置它对收件箱行为没有影响。
	/// </para>
	/// </remarks>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置后台调度器扫描失败消息并重试执行的轮询间隔。
	/// </summary>
	/// <value>默认值为 60 秒。</value>
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 获取或设置单条消息允许的最大执行（重试）次数。
	/// </summary>
	/// <remarks>
	/// <c>0</c> 或负数表示不限制重试次数；大于 <c>0</c> 时，
	/// 超过该次数的失败记录将被跳过（视为死信，日志警告并停止重试）。
	/// 该值统计的是重试轮次（<see cref="InboxHandler.RetryAttempts"/>）。
	/// </remarks>
	public int MaxRetryAttempts { get; set; }

	/// <summary>
	/// 获取或设置已完成（或已转入死信）条目的保留时长，超期后在调度器轮询时清理。
	/// </summary>
	/// <remarks>
	/// 默认保留 24 小时。<c>0</c> 或负数表示不清理。
	/// 时区约定与 <see cref="InboxEntry.CreatedAt"/> 一致，均为本地时间。
	/// </remarks>
	public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromHours(24);
}