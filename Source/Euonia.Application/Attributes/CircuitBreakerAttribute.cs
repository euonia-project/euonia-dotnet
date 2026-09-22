namespace Nerosoft.Euonia.Application;

/// <summary>
/// 熔断器特性，可应用于方法（或类型，表示其中所有方法）以在连续失败达到阈值后快速失败。
/// </summary>
/// <remarks>
/// 状态机：
/// <list type="bullet">
/// <item><description>Closed（关闭）：允许全部调用。每次失败递增失败计数，达到 <see cref="MaxFailures"/> 后转为 Open；成功则重置失败计数。</description></item>
/// <item><description>Open（打开）：快速失败，不执行目标方法，抛出 <see cref="CircuitBreakerOpenException"/>；经过 <see cref="ResetTimeoutSeconds"/> 秒后转入 HalfOpen 放行探测请求。</description></item>
/// <item><description>HalfOpen（半开）：放行有限探测请求，成功次数达到 <see cref="SuccessThreshold"/> 即关闭；任意探测失败立即重新打开。</description></item>
/// </list>
/// </remarks>
public sealed class CircuitBreakerAttribute : Attribute
{
	/// <summary>
	/// 获取或设置触发打开状态的连续失败次数。默认值为 5。
	/// </summary>
	/// <exception cref="ArgumentException">当赋值小于或等于零时抛出。</exception>
	public int MaxFailures
	{
		get => _maxFailures;
		set
		{
			Check.Ensure(value > 0, nameof(MaxFailures), "Max failures must be greater than zero.");
			_maxFailures = value;
		}
	}

	private int _maxFailures = 5;

	/// <summary>
	/// 获取或设置打开状态下重试探测（转入半开）的等待秒数。默认值为 30。
	/// </summary>
	/// <exception cref="ArgumentException">当赋值小于或等于零时抛出。</exception>
	public double ResetTimeoutSeconds
	{
		get => _resetTimeoutSeconds;
		set
		{
			Check.Ensure(value > 0, nameof(ResetTimeoutSeconds), "Reset timeout must be greater than zero.");
			_resetTimeoutSeconds = value;
		}
	}

	private double _resetTimeoutSeconds = 30;

	/// <summary>
	/// 获取或设置半开状态下关闭所需的连续成功次数。默认值为 1。
	/// </summary>
	/// <exception cref="ArgumentException">当赋值小于或等于零时抛出。</exception>
	public int SuccessThreshold
	{
		get => _successThreshold;
		set
		{
			Check.Ensure(value > 0, nameof(SuccessThreshold), "Success threshold must be greater than zero.");
			_successThreshold = value;
		}
	}

	private int _successThreshold = 1;
}