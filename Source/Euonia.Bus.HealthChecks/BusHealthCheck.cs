using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.HealthChecks;

/// <summary>
/// 消息总线健康检查：报告已配置的传输器数量，以及发件箱、收件箱与死信的积压情况。
/// </summary>
/// <remarks>
/// 所有依赖均为可选：未注册对应存储时不参与判定，也不会因此报错。
/// 只有当某项积压超过 <see cref="BusHealthCheckOptions"/> 中的阈值时才判定为
/// <see cref="HealthStatus.Unhealthy"/>，否则为 <see cref="HealthStatus.Healthy"/>。
/// </remarks>
public sealed class BusHealthCheck : IHealthCheck
{
	private readonly IServiceProvider _provider;
	private readonly BusHealthCheckOptions _options;

	/// <summary>
	/// 初始化 <see cref="BusHealthCheck"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析总线相关服务的服务提供程序。</param>
	/// <param name="options">健康检查配置选项。</param>
	public BusHealthCheck(IServiceProvider provider, IOptions<BusHealthCheckOptions> options)
	{
		_provider = provider;
		_options = options.Value;
	}

	/// <inheritdoc/>
	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		var data = new Dictionary<string, object>();
		var failures = new List<string>();

		// 用 IConfigurator 而非 IBus 判断总线是否装配：解析 IBus 会真正构造 MessageBus，
		// 从而启动发件箱轮询定时器等后台工作——健康检查不应产生这种副作用。
		var configurator = _provider.GetService<IConfigurator>();
		if (configurator == null)
		{
			failures.Add(Resources.IDS_BUS_NOT_REGISTERED);
		}

		var transportCount = configurator?.StrategyAssignedTypes.Count ?? 0;
		data["transports"] = transportCount;

		if (_options.RequireTransporter && transportCount == 0)
		{
			failures.Add(Resources.IDS_NO_TRANSPORTER_CONFIGURED);
		}

		// 死信是重试耗尽的终态，默认只要出现即视为不健康。
		var deadLetters = _provider.GetService<IDeadLetterStore>()?.GetAll().Count;
		if (deadLetters.HasValue)
		{
			data["deadLetters"] = deadLetters.Value;

			if (deadLetters.Value > _options.MaxDeadLetters)
			{
				failures.Add(string.Format(Resources.IDS_DEAD_LETTER_BACKLOG_EXCEEDS, deadLetters.Value, _options.MaxDeadLetters));
			}
		}

		var outboxFailed = _provider.GetService<IOutboxStore>()?.GetFailedMessages().Count;
		if (outboxFailed.HasValue)
		{
			data["outbox.failed"] = outboxFailed.Value;

			if (_options.MaxOutboxFailed >= 0 && outboxFailed.Value > _options.MaxOutboxFailed)
			{
				failures.Add(string.Format(Resources.IDS_OUTBOX_FAILED_BACKLOG_EXCEEDS, outboxFailed.Value, _options.MaxOutboxFailed));
			}
		}

		var inboxFailed = _provider.GetService<IInboxStore>()?.GetFailedMessages().Count;
		if (inboxFailed.HasValue)
		{
			data["inbox.failed"] = inboxFailed.Value;

			if (_options.MaxInboxFailed >= 0 && inboxFailed.Value > _options.MaxInboxFailed)
			{
				failures.Add(string.Format(Resources.IDS_INBOX_FAILED_BACKLOG_EXCEEDS, inboxFailed.Value, _options.MaxInboxFailed));
			}
		}

		if (failures.Count > 0)
		{
			return Task.FromResult(HealthCheckResult.Unhealthy(string.Join(" ", failures), data: data));
		}

		return Task.FromResult(HealthCheckResult.Healthy(Resources.IDS_BUS_HEALTHY, data));
	}
}
