namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Osba 的装配选项。
/// </summary>
/// <remarks>
/// 选项只影响<b>装配与诊断</b>，不改变业务对象的运行时语义。默认值偏向兼容：
/// 启动期校验需要显式开启，因为宿主可能把测试/设计期类型也扫描进来。
/// </remarks>
public sealed class OsbaOptions
{
	/// <summary>
	/// 默认选项。
	/// </summary>
	public static OsbaOptions Default { get; } = new();

	/// <summary>
	/// 获取或设置是否在 <c>AddBusinessObject</c> 时执行启动期校验。
	/// </summary>
	/// <remarks>
	/// 默认关闭：测试程序集里常有意为之的“半成品”业务对象，自动校验会让
	/// <c>AddBusinessObject(typeof(Startup).Assembly)</c> 直接失败。生产宿主应显式开启。
	/// </remarks>
	public bool ValidateOnStartup { get; set; }

	/// <summary>
	/// 获取或设置是否对缺少持久化工厂方法（Insert/Update/Delete）的对象抛错。
	/// </summary>
	/// <remarks>
	/// 默认开启。关闭后允许缺少持久化方法时调用基类空实现，只应在迁移期临时使用。
	/// </remarks>
	public bool StrictFactoryMethods { get; set; } = true;

	/// <summary>
	/// 获取或设置是否启用 <see cref="System.Diagnostics.ActivitySource"/> 与
	/// <see cref="System.Diagnostics.Metrics.Meter"/> 指标。
	/// </summary>
	public bool EnableDiagnostics { get; set; } = true;

	/// <summary>
	/// 获取或设置诊断日志的最低级别。
	/// </summary>
	public OsbaDiagnosticLevel DiagnosticLevel { get; set; } = OsbaDiagnosticLevel.Warning;
}

/// <summary>
/// Osba 诊断级别；与 <c>Microsoft.Extensions.Logging.LogLevel</c> 对齐但避免把日志包变成硬依赖。
/// </summary>
public enum OsbaDiagnosticLevel
{
	/// <summary>详细跟踪。</summary>
	Trace,

	/// <summary>调试信息。</summary>
	Debug,

	/// <summary>一般信息。</summary>
	Information,

	/// <summary>警告。</summary>
	Warning,

	/// <summary>错误。</summary>
	Error,

	/// <summary>关闭诊断日志。</summary>
	None
}
