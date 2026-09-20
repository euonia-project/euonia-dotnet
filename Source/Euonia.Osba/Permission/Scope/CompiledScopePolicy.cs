using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 已编译的数据权限策略：允许/拒绝一对谓词。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 这是数据权限的<b>单一真值来源</b>。下推与内存求值都基于这里的同一对表达式，
/// 因此「查询过滤掉了哪些行」与「单行判定放行哪些行」不可能得出不同结论。
/// </para>
/// <para>
/// 判定语义恒为 <c>Allow &amp;&amp; !Deny</c>。当策略只包含拒绝条件时，
/// <see cref="Allow"/> 恒真而 <see cref="HasAllow"/> 为 <see langword="false"/>。
/// </para>
/// </remarks>
public sealed class CompiledScopePolicy<T>
	where T : class
{
	private readonly Lazy<Func<T, bool>> _allow;
	private readonly Lazy<Func<T, bool>> _deny;

	internal CompiledScopePolicy(Expression<Func<T, bool>> allow, Expression<Func<T, bool>> deny, bool hasAllow, string scopeKey)
	{
		Allow = allow;
		Deny = deny;
		HasAllow = hasAllow;
		ScopeKey = scopeKey;

		// 委托按需编译并缓存：下推路径根本不求值，不应付出编译开销
		_allow = new Lazy<Func<T, bool>>(() => Allow.Compile());
		_deny = new Lazy<Func<T, bool>>(() => Deny.Compile());
	}

	/// <summary>
	/// 获取本次编译使用的权限码（策略键）。
	/// </summary>
	/// <remarks>仅用于审计与排障；它<b>不会</b>出现在 <see cref="Allow"/> / <see cref="Deny"/> 中。</remarks>
	public string ScopeKey { get; }

	/// <summary>
	/// 获取允许条件。
	/// </summary>
	/// <remarks>可直接交给 <c>IQueryable.Where</c> 下推到数据库。</remarks>
	public Expression<Func<T, bool>> Allow { get; }

	/// <summary>
	/// 获取拒绝条件。
	/// </summary>
	/// <remarks>
	/// 下推时应使用其否定形式：<c>source.Where(allow).Where(deny.Not())</c>。
	/// </remarks>
	public Expression<Func<T, bool>> Deny { get; }

	/// <summary>
	/// 获取策略是否提供了允许条件。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="false"/> 表示策略只含拒绝条件（拒绝清单语义）。本属性主要用于
	/// 启动期校验：没有任何允许条件的策略应当被显式确认，否则容易误配成「拒绝一切」。
	/// </remarks>
	public bool HasAllow { get; }

	/// <summary>
	/// 在内存中判定单个资源是否可访问。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	internal bool Evaluate(T resource)
	{
		return resource != null && _allow.Value(resource) && !_deny.Value(resource);
	}
}
