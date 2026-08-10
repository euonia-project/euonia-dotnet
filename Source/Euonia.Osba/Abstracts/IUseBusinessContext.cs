namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示业务对象应使用业务上下文的契约。
/// </summary>
public interface IUseBusinessContext
{
	/// <summary>
	/// 获取或设置业务上下文。
	/// </summary>
	BusinessContext BusinessContext { get; set; }
}