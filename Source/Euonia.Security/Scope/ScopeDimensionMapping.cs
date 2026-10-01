using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 一个维度在资源侧的取值映射：单值（行内的列）或集合（子表/关系表）。
/// </summary>
/// <remarks>
/// <para>
/// 集合维度由 <see cref="ScopeModelBuilder{T}.MapMany"/> 声明，其选择器会被<b>分解</b>成
/// 「集合导航 + 可选过滤 + 元素取值」三段，而不是原样保留：这样编译期产出的始终是同一种表达式形状
/// （<c>x.Collection.Where(f).Any(v =&gt; values.Contains(v.Value))</c>），也就是人类手写的、
/// 提供程序（EF Core）确定能翻译成 <c>EXISTS</c> 的形状。
/// </para>
/// <para>
/// 分解同时提供了「子集合是否缺失」的探测表达式——单行判定在内存中求值，需要对象图完整。
/// </para>
/// </remarks>
internal sealed record ScopeDimensionMapping
{
	private ScopeDimensionMapping(string dimension, LambdaExpression value, LambdaExpression collection, IReadOnlyList<LambdaExpression> filters, string path, Type elementType)
	{
		Dimension = dimension;
		Value = value;
		Collection = collection;
		Filters = filters;
		Path = path;
		ElementType = elementType;
	}

	/// <summary>维度名。</summary>
	internal string Dimension { get; }

	/// <summary>
	/// 取值选择器：单值维度是资源上的一个值（<c>x =&gt; x.DeptId</c>）；
	/// 集合维度是<b>子项</b>上的一个值（<c>v =&gt; v.UserId</c>），子项本身是字符串时为恒等映射。
	/// </summary>
	internal LambdaExpression Value { get; }

	/// <summary>集合导航（<c>x =&gt; x.Members</c>）；单值维度为 <see langword="null"/>。</summary>
	internal LambdaExpression Collection { get; }

	/// <summary>集合导航上的过滤条件（<c>m =&gt; m.Status == "active"</c>），按书写顺序；单值维度为空。</summary>
	internal IReadOnlyList<LambdaExpression> Filters { get; }

	/// <summary>资源侧取值路径的可读描述（例如 <c>x.Members</c>），用于报错与审计。</summary>
	internal string Path { get; }

	/// <summary>集合的元素类型（子表行的类型）；单值维度为 <see cref="string"/>。</summary>
	internal Type ElementType { get; }

	/// <summary>是否为集合维度（取值来自子表）。</summary>
	internal bool IsCollection => Collection != null;

	/// <summary>
	/// 从 <see cref="ScopeModelBuilder{T}.Map"/> / <see cref="ScopeModelBuilder{T}.MapMany"/> 的选择器解析出映射。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="selector">取值表达式。</param>
	/// <returns>映射。</returns>
	/// <exception cref="InvalidOperationException">选择器的形状不受支持时抛出。</exception>
	internal static ScopeDimensionMapping Create(string dimension, LambdaExpression selector)
	{
		// 单值维度：资源行上的一个值
		if (selector.ReturnType == typeof(string))
		{
			return new ScopeDimensionMapping(dimension, selector, null, [], Strip(selector.Body).ToString(), typeof(string));
		}

		var body = Strip(selector.Body);
		LambdaExpression value = null;
		var filters = new List<LambdaExpression>();

		// 由外向内剥离 Select / Where，剩下的必须是以资源参数为根的成员访问链（导航集合）
		while (body is MethodCallExpression call && call.Method.DeclaringType == typeof(Enumerable))
		{
			if (call.Method.Name == nameof(Enumerable.Select) && call.Arguments.Count == 2 && value == null)
			{
				value = (LambdaExpression)Strip(call.Arguments[1]);
				body = Strip(call.Arguments[0]);
				continue;
			}

			if (call.Method.Name == nameof(Enumerable.Where) && call.Arguments.Count == 2)
			{
				filters.Insert(0, (LambdaExpression)Strip(call.Arguments[1]));
				body = Strip(call.Arguments[0]);
				continue;
			}

			break;
		}

		Check.Ensure(IsResourceMemberChain(body, selector.Parameters[0]), UnsupportedShape(dimension, selector));

		var elementType = ResolveElementType(body.Type);
		Check.Ensure(elementType != null, UnsupportedShape(dimension, selector));

		if (value == null)
		{
			// 集合元素自身就是维度值（例如 x => x.MemberIds）
			Check.Ensure(elementType == typeof(string), UnsupportedShape(dimension, selector));
		}
		else
		{
			// 元素上的取值选择器必须落在字符串上——维度值类型固定为 string（见 DESIGN §2.4）
			Check.Ensure(Strip(value.Body).Type == typeof(string), UnsupportedShape(dimension, selector));
		}

		return new ScopeDimensionMapping(
			dimension,
			value,
			Expression.Lambda(body, selector.Parameters[0]),
			filters,
			body.ToString(),
			elementType);
	}

	/// <summary>
	/// 生成「子集合是否缺失」的探测表达式（<c>x =&gt; (object)x.Members == null</c>）。
	/// </summary>
	/// <returns>探测表达式；集合为值类型（不可能为 null）时返回 <see langword="null"/>。</returns>
	/// <remarks>
	/// 探测只用于单行判定，绝不参与下推：查询路径由数据库求值，与内存对象图无关。
	/// </remarks>
	internal LambdaExpression CreateLoadProbe()
	{
		if (!IsCollection || Collection.Body.Type.IsValueType)
		{
			return null;
		}

		return Expression.Lambda(
			Expression.Equal(
				Expression.Convert(Collection.Body, typeof(object)),
				Expression.Constant(null)),
			Collection.Parameters[0]);
	}

	/// <summary>去掉编译器插入的转换节点，使形状判定面对的是表达式本身。</summary>
	private static Expression Strip(Expression expression)
	{
		return expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
			? Strip(convert.Operand)
			: expression;
	}

	/// <summary>判断表达式是否为「以资源参数为根的成员访问链」。</summary>
	private static bool IsResourceMemberChain(Expression expression, ParameterExpression resource)
	{
		while (true)
		{
			switch (Strip(expression))
			{
				case MemberExpression member:
					expression = member.Expression;
					continue;
				case ParameterExpression parameter:
					return parameter == resource;
				default:
					return false;
			}
		}
	}

	/// <summary>取集合的元素类型；不是集合时返回 <see langword="null"/>。</summary>
	private static Type ResolveElementType(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
			? type.GetGenericArguments()[0]
			: type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
	}

	private static string UnsupportedShape(string dimension, LambdaExpression selector)
	{
		return string.Format(Resources.IDS_SCOPE_DIMENSION_EXPRESSION_UNSUPPORTED, dimension, selector);
	}
}

/// <summary>
/// 集合维度在单行判定前的「子集合是否缺失」探测。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <param name="Dimension">维度名。</param>
/// <param name="Path">取值路径的可读描述（例如 <c>x.Members</c>）。</param>
/// <param name="IsMissing">探测表达式：资源实例上的子集合是否为空引用。</param>
/// <remarks>
/// 探测只在内存求值前执行，绝不参与下推——查询路径由数据库求值，与内存对象图无关。
/// </remarks>
internal sealed record ScopeLoadGuard<T>(string Dimension, string Path, Expression<Func<T, bool>> IsMissing)
	where T : class;
