using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

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
	/// 该码只决定取值来源，其本身不会出现在编译出的表达式里。
	/// </remarks>
	internal string ScopeKey { get; }

	/// <summary>
	/// 获取本次编译使用的模型描述（叶子描述据此区分单值维度与子表维度）。
	/// </summary>
	internal ScopeModelDescriptor Descriptor => _descriptor;

	/// <summary>
	/// 生成「资源在指定维度上的取值集合，与用户在该维度上被授予的集合相交非空」这一条件。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <returns>条件表达式 body。</returns>
	/// <exception cref="InvalidOperationException">当该维度未在资源模型中映射时抛出。</exception>
	/// <remarks>
	/// 单值维度（<c>x =&gt; x.DeptId</c>）产出集合成员判断，下推为 <c>IN (...)</c>；
	/// 集合维度（<c>x =&gt; x.Members.Select(m =&gt; m.UserId)</c>，见 <see cref="ScopeModelBuilder{T}.MapMany"/>）
	/// 产出「子集合中存在一个值被授予」，下推为 <c>EXISTS</c> 子查询。两者语义一致，单值是后者的退化情形。
	/// </remarks>
	internal Expression GrantCondition(string dimension)
	{
		// 先取映射：即便用户在该维度上没有任何授予，未映射的维度也必须暴露为错误
		var mapping = _descriptor.GetDimension(dimension);

		// 只按「当前码 → 默认键」取值，绝不做权限码通配回落（见 ScopeSubjectSet 的查找规则）
		var values = _subjects.ValuesOf(ScopeKey, dimension);
		if (values.Count == 0)
		{
			// 用户在该维度上未被授予任何值：直接产出恒假。
			// 不生成空 IN ()，也不会生成空 EXISTS，避免落入各提供程序对空集合翻译的差异。
			return ScopePolicyNode<T>.False;
		}

		// 用户侧取值：烘成常量集合（去重由 ScopeSubjectSet 保证）
		var list = values is List<string> existing ? existing : new List<string>(values);
		var granted = Expression.Constant(list, typeof(List<string>));

		return mapping.IsCollection
			? CollectionCondition(mapping, granted)
			: Contains(granted, Rebind(mapping.Value));
	}

	/// <summary>
	/// 生成集合维度的条件：<c>x.Members.Where(f).Any(v =&gt; values.Contains(v.UserId))</c>。
	/// </summary>
	/// <remarks>
	/// 刻意产出「导航集合 + 过滤 + 元素谓词」这一种形状：它就是人类手写、提供程序确定能翻译成
	/// <c>EXISTS</c> 子查询的形状。选择器里的 <c>Select</c> 之所以被拆掉，是因为
	/// 「对投影结果求 <c>Any</c>」的形状能否翻译取决于提供程序，而这里不允许把可翻译性押在猜测上。
	/// </remarks>
	private Expression CollectionCondition(ScopeDimensionMapping mapping, Expression granted)
	{
		var element = Expression.Parameter(mapping.ElementType, "v");
		var source = Rebind(mapping.Collection);

		foreach (var filter in mapping.Filters)
		{
			source = Expression.Call(
				PredicateMethod(nameof(Enumerable.Where), mapping.ElementType),
				source,
				Expression.Lambda(ScopeParameterReplacer.Replace(filter, element), element));
		}

		var value = mapping.Value == null ? element : ScopeParameterReplacer.Replace(mapping.Value, element);

		return Expression.Call(
			PredicateMethod(nameof(Enumerable.Any), mapping.ElementType),
			source,
			Expression.Lambda(Contains(granted, value), element));
	}

	/// <summary>
	/// 生成 <c>Enumerable.Contains(values, value)</c>，即「value 属于授予集合」。
	/// </summary>
	private static Expression Contains(Expression values, Expression value)
	{
		return Expression.Call(ContainsMethod, values, value);
	}

	/// <summary>
	/// 构建集合维度的加载探测（供单行判定在求值前检查对象图是否完整）。
	/// </summary>
	/// <returns>探测列表；模型没有集合维度时为空。</returns>
	internal IReadOnlyList<ScopeLoadGuard<T>> CreateLoadGuards()
	{
		var guards = new List<ScopeLoadGuard<T>>();

		foreach (var dimension in _descriptor.CollectionDimensions)
		{
			var mapping = _descriptor.GetDimension(dimension);
			var probe = mapping.CreateLoadProbe();

			if (probe != null)
			{
				guards.Add(new ScopeLoadGuard<T>(dimension, mapping.Path, Lambda(Rebind(probe))));
			}
		}

		return guards;
	}

	/// <summary>
	/// 取「集合 + 元素谓词」形态的 <see cref="Enumerable"/> 方法（<c>Where</c> / <c>Any</c>）。
	/// </summary>
	/// <remarks>
	/// 不能用按名字查找的重载：<see cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
	/// 还有一个同元数的 <c>Func&lt;TSource, int, bool&gt;</c> 版本，按名字查会歧义。
	/// 结果按（方法名，元素类型）缓存——编译路径是热点，不该反复反射。
	/// </remarks>
	private static MethodInfo PredicateMethod(string name, Type elementType)
	{
		return Methods.GetOrAdd((name, elementType), key => typeof(Enumerable)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(method => method.Name == key.Name
			                  && method.IsGenericMethodDefinition
			                  && method.GetParameters().Length == 2
			                  && method.GetParameters()[1].ParameterType.IsGenericType
			                  && method.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
			.MakeGenericMethod(key.ElementType));
	}

	private static readonly ConcurrentDictionary<(string Name, Type ElementType), MethodInfo> Methods = new();

	private static readonly MethodInfo ContainsMethod = typeof(Enumerable)
		.GetMethods(BindingFlags.Public | BindingFlags.Static)
		.Single(method => method.Name == nameof(Enumerable.Contains) && method.GetParameters().Length == 2)
		.MakeGenericMethod(typeof(string));

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
