namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 按类型持有的规则注册表。
/// </summary>
/// <remarks>
/// <para>
/// 规则的共享边界是<b>容器</b>，不是进程：每个 <c>AddBusinessObject</c> 注册一个
/// <see cref="RuleRegistry"/>，不同容器（测试宿主、租户、插件）的规则互不可见。
/// </para>
/// <para>
/// 没有 <see cref="BusinessContext"/> 的对象（手工 <c>new</c>、静态初始化期）回退到
/// <see cref="RuleManager"/> 的进程级默认注册表，保持旧用法可用。
/// </para>
/// </remarks>
public interface IRuleRegistry
{
	/// <summary>
	/// 获取指定类型的规则管理器。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <returns>规则管理器。</returns>
	RuleManager GetRules(Type type);

	/// <summary>
	/// 清除指定类型的规则；下次访问时重新执行 <c>AddRules</c>。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	void CleanRules(Type type);
}

/// <summary>
/// <see cref="IRuleRegistry"/> 的默认容器级实现。
/// </summary>
public sealed class RuleRegistry : IRuleRegistry
{
	private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, RuleManager> _rules = new();

	/// <inheritdoc />
	public RuleManager GetRules(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		return _rules.GetOrAdd(type, static _ => RuleManager.Create());
	}

	/// <inheritdoc />
	public void CleanRules(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		_rules.TryRemove(type, out _);
	}

	/// <summary>
	/// 静态默认注册表：无 DI / 无业务上下文时使用。
	/// </summary>
	internal static RuleRegistry Default { get; } = new();
}
