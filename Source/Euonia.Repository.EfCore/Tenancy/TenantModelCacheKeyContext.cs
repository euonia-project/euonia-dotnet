using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 模型构建期可见的租户 schema 上下文。
/// </summary>
/// <remarks>
/// 正常路径由 <see cref="ITenantModelCacheKeySource"/> 从上下文读取租户；
/// 本类型仅作为设计时/手工构建模型时的兼容回退。
/// </remarks>
public static class TenantModelCacheKeyContext
{
	private static readonly AsyncLocal<State?> _current = new();

	/// <summary>
	/// 设置当前异步流的租户 schema。
	/// </summary>
	/// <param name="isolation">隔离模式。</param>
	/// <param name="schema">租户 schema。</param>
	public static void Set(TenantIsolation isolation, string? schema)
	{
		_current.Value = new State(isolation, schema);
	}

	/// <summary>
	/// 清除当前异步流的租户 schema。
	/// </summary>
	public static void Clear()
	{
		_current.Value = null;
	}

	/// <summary>
	/// 获取当前异步流的租户 schema。
	/// </summary>
	public static (TenantIsolation Isolation, string? Schema) Current =>
		_current.Value is { } state ? (state.Isolation, state.Schema) : (TenantIsolation.SharedTable, null);

	private sealed record State(TenantIsolation Isolation, string? Schema);
}
