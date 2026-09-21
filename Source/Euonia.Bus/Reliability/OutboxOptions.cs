namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发件箱（Outbox）模式的相关配置选项。
/// </summary>
/// <remarks>
/// 发件箱模式通过在发送方业务事务中原子性地记录待发送消息，
/// 保证发布操作至少会被投递一次（at-least-once）。开启后，发布（<see cref="IBus"/>.PublishAsync）的消息
/// 会先写入发件箱存储，再由后台调度器投递给各传输通道。
/// </remarks>
public class OutboxOptions
{
	/// <summary>
	/// 获取或设置是否启用发件箱模式（全局开关）。
	/// </summary>
	/// <remarks>
	/// 全局开关默认关闭。即使全局关闭，仍可通过消息级别选项
	/// <see cref="ExtendableOptions.UseOutbox"/> 对单条消息启用发件箱记录。
	/// </remarks>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置后台调度器扫描失败消息并重试投递的轮询间隔。
	/// </summary>
	/// <value>默认值为 60 秒。</value>
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 获取或设置单条消息允许的最大投递（重试）次数。
	/// </summary>
	/// <remarks>
	/// <c>0</c> 或负数表示不限制重试次数；大于 <c>0</c> 时，
	/// 超过该次数的失败记录将被跳过（视为死信，日志警告并停止重试）。
	/// 该值统计的是重试轮次（<see cref="OutboxTransport.RetryAttempts"/>）。
	/// </remarks>
	public int MaxRetryAttempts { get; set; }

	/// <summary>
	/// 获取或设置已完成（或已转入死信）条目的保留时长，超期后在调度器轮询时清理。
	/// </summary>
	/// <remarks>
	/// 默认保留 24 小时。<c>0</c> 或负数表示不清理。
	/// 时区约定与 <see cref="OutboxEntry.CreatedAt"/> 一致，均为本地时间。
	/// </remarks>
	public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromHours(24);
}