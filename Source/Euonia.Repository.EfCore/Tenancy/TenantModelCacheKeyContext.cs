using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 模型构建期可见的租户 schema 上下文。
/// </summary>
/// <remarks>
/// EF 的内部服务提供程序无法解析应用注册的 <see cref="ITenantAccessor"/>，
/// 因此由 options 配置回调在解析上下文前写入当前异步流的 schema，工厂只读该值。
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
	/// 获取当前异步流的租户 schema。
	/// </summary>
	public static (TenantIsolation Isolation, string? Schema) Current =>
		_current.Value is { } state ? (state.Isolation, state.Schema) : (TenantIsolation.SharedTable, null);

	private sealed record State(TenantIsolation Isolation, string? Schema);
}
