namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 表示一次租户作用域；释放时恢复进入前的租户上下文。
/// </summary>
public interface ITenantScope : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// 获取本次作用域的租户快照。
	/// </summary>
	TenantContext Context { get; }
}
