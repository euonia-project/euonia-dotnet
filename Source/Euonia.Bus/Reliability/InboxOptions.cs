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
	/// 全局开关默认关闭。即使全局关闭，仍可通过消息级别选项
	/// <see cref="ExtendableOptions.UseInbox"/> 对单条消息启用收件箱记录。
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
}