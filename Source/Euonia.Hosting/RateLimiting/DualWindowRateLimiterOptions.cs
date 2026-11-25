namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// <see cref="DualWindowRateLimiter"/> 的窗口配置。
/// </summary>
public sealed class DualWindowRateLimiterOptions
{
	/// <summary>
	/// 获取或设置分钟窗口内允许的请求数；默认 <c>5</c>。
	/// </summary>
	public int MinutePermitLimit { get; set; } = 5;

	/// <summary>
	/// 获取或设置分钟窗口长度；默认 1 分钟。
	/// </summary>
	public TimeSpan MinuteWindow { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// 获取或设置小时窗口内允许的请求数；默认 <c>10</c>。
	/// </summary>
	public int HourPermitLimit { get; set; } = 10;

	/// <summary>
	/// 获取或设置小时窗口长度；默认 1 小时。
	/// </summary>
	public TimeSpan HourWindow { get; set; } = TimeSpan.FromHours(1);

	/// <summary>
	/// 获取或设置每个窗口允许排队的请求数；默认 <c>0</c>（不排队）。
	/// </summary>
	public int QueueLimit { get; set; }

	/// <summary>
	/// 获取或设置是否自动补充令牌；默认 <c>true</c>。
	/// </summary>
	/// <remarks>
	/// 设为 <c>false</c> 时窗口到期不会自动或惰性重置计数，
	/// 调用方必须在窗口过期后定期调用 <see cref="DualWindowRateLimiter.TryReplenish"/>，否则许可耗尽后将一直拒绝请求。
	/// </remarks>
	public bool AutoReplenishment { get; set; } = true;

	/// <summary>
	/// 校验窗口与请求数配置。
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">窗口或请求数配置非法。</exception>
	public void Validate()
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinutePermitLimit);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(HourPermitLimit);
		ArgumentOutOfRangeException.ThrowIfNegative(QueueLimit);

		if (MinuteWindow <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(MinuteWindow), "分钟窗口必须大于零。");
		}

		if (HourWindow <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(HourWindow), "小时窗口必须大于零。");
		}
	}
}
