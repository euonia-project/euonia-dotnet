using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 策略编译上下文：持有本次编译共用的规范参数与用户主体集合。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 所有叶子条件都被重绑定到<b>同一个</b> <see cref="Parameter"/> 实例上，因此后续在 body 层做
/// <c>AndAlso</c>/<c>OrElse</c>/<c>Not</c> 组合时不需要再做参数重绑定——
/// 这既简化了归约实现，也避免了多份参数实例导致的「参数未绑定」错误。
/// </para>
/// <para>
/// 用户被授予的值在此处被<b>烘成表达式常量</b>（<c>ids.Contains(x.DeptId)</c>），
/// 这正是判定既能下推又能内存求值的原因。
/// </para>
/// </remarks>
internal sealed class ScopeCompileContext<T>
	where T : class
{
	private readonly ScopeModelDescriptor _descriptor;
	private readonly ScopeSubjectSet _subjects;

	internal ScopeCompileContext(ScopeModelDescriptor descriptor, ScopeSubjectSet subjects, string scopeKey)
	{
		_descriptor = descriptor;
		_subjects = subjects;
		ScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? ScopeKeys.Default : scopeKey;
		Parameter = Expression.Parameter(typeof(T), "x");
	}

	/// <summary>
	/// 获取本次编译共用的规范参数。
	/// </summary>
	internal ParameterExpression Parameter { get; }

	/// <summary>
	/// 获取本次编译使用的权限码（策略键）。
	/// </summary>
	/// <remarks>
	/// <c>Grant(dimension)</c> 取的是「用户<b>在该码下</b>于该维度被授予的值」。
	/// 该值只影响取值来源，不会出现在编译出的表达式里。
	/// </remarks>
	internal string ScopeKey { get; }

	/// <summary>
	/// 生成「资源在指定维度上的值，属于用户在该维度上被授予的集合」这一条件。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <returns>条件表达式 body。</returns>
	/// <exception cref="InvalidOperationException">当该维度未在资源模型中映射时抛出。</exception>
	internal Expression GrantCondition(string dimension)
	{
		// 先取映射：即便用户在该维度上没有任何授予，未映射的维度也必须暴露为错误
		var selector = _descriptor.GetDimensionSelector(dimension);

		// 只按「当前码 → 默认键」取值，绝不做权限码通配回落（见 ScopeSubjectSet 的查找规则）
		var values = _subjects.ValuesOf(ScopeKey, dimension);
		if (values.Count == 0)
		{
			// 用户在该维度上未被授予任何值：直接产出恒假。
			// 不生成空 IN ()，避免落入各提供程序对空集合翻译的差异。
			return ScopePolicyNode<T>.False;
		}

		// 资源侧取值：x => x.DeptId，重绑定到规范参数
		var value = Rebind(selector);

		// 用户侧取值：烘成常量集合（去重由 ScopeSubjectSet 保证）
		var list = values is List<string> existing ? existing : new List<string>(values);

		// 生成 Enumerable.Contains(values, x.DeptId) —— EF Core 会翻译成 IN (...)
		return Expression.Call(
			typeof(Enumerable),
			nameof(Enumerable.Contains),
			[typeof(string)],
			Expression.Constant(list, typeof(List<string>)),
			value);
	}

	/// <summary>
	/// 将谓词重绑定到规范参数上，返回其 body。
	/// </summary>
	/// <param name="predicate">谓词。</param>
	/// <returns>重绑定后的 body。</returns>
	internal Expression Rebind(LambdaExpression predicate)
	{
		return ScopeParameterReplacer.Replace(predicate, Parameter);
	}

	/// <summary>
	/// 用规范参数把 body 包装成谓词。
	/// </summary>
	/// <param name="body">条件表达式 body。</param>
	/// <returns>谓词。</returns>
	internal Expression<Func<T, bool>> Lambda(Expression body)
	{
		return Expression.Lambda<Func<T, bool>>(body, Parameter);
	}
}
