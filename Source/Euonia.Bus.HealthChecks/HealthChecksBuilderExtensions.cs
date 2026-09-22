using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于注册消息总线健康检查的扩展方法。
/// </summary>
public static class HealthChecksBuilderExtensions
{
	/// <summary>
	/// 健康检查在 <see cref="IHealthChecksBuilder"/> 中的注册名称。
	/// </summary>
	public const string Name = "euonia-bus";

	/// <param name="builder">用于注册健康检查的构建器。</param>
	extension(IHealthChecksBuilder builder)
	{
		/// <summary>
		/// 注册消息总线健康检查：报告传输器数量以及发件箱、收件箱与死信的积压情况。
		/// </summary>
		/// <param name="configure">用于配置 <see cref="BusHealthCheckOptions"/> 的可选委托。</param>
		/// <param name="failureStatus">
		/// 检查失败时上报的状态；为 <c>null</c> 时使用 <see cref="HealthStatus.Unhealthy"/>。
		/// </param>
		/// <param name="tags">与该健康检查关联的标签。</param>
		/// <param name="timeout">允许的最长执行时间。</param>
		/// <returns>返回当前的 <see cref="IHealthChecksBuilder"/> 实例，以便进行链式调用。</returns>
		/// <example>
		/// <code>
		/// services.AddHealthChecks().AddEuoniaBusHealthChecks(options =&gt;
		/// {
		///     options.MaxDeadLetters = 10;
		///     options.MaxOutboxFailed = 100;
		/// });
		/// </code>
		/// </example>
		public IHealthChecksBuilder AddEuoniaBusHealthChecks(Action<BusHealthCheckOptions> configure = null, HealthStatus? failureStatus = null, IEnumerable<string> tags = null, TimeSpan? timeout = null)
		{
			if (configure != null)
			{
				builder.Services.Configure(configure);
			}

			return builder.Add(new HealthCheckRegistration(Name,
			                                              provider => new BusHealthCheck(provider, provider.GetRequiredService<IOptions<BusHealthCheckOptions>>()),
			                                              failureStatus,
			                                              tags,
			                                              timeout));
		}
	}
}
