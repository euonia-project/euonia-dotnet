using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 兼容入口：用「操作入口判定规则」按 <c>(类型, 操作)</c> 回答权限要求与权限码。
/// </summary>
/// <remarks>
/// <para>
/// 规则是数据：每个已声明的操作对应一组入口判定谓词，由 <c>OnAttributeOrName</c> / <c>OnMethod</c> 配置构造。
/// <b>类型级</b> <see cref="PermissionAttribute"/> 对本来源声明的每个操作生效；<b>方法级</b>声明只在
/// 该方法被本操作的规则命中时生效。
/// </para>
/// <para>
/// 操作集合以 <see cref="AllOperations"/> 为准：只有声明过规则的操作才属于本来源。
/// 按「类型 + 被调用的方法」收集要求请使用 <see cref="PermissionRequirements.For(Type, MethodInfo)"/>。
/// </para>
/// <para>
/// 结果按（类型，操作）缓存，反复调用不会重复反射。
/// </para>
/// </remarks>
[Obsolete("按 (type, operation) 查询的兼容类型。请使用 PermissionRequirements / IPermissionRequirementSource；本类型将在下个主版本移除。")]
internal sealed class OperationCodeSource : IPermissionCodeSource
{
	/// <summary>
	/// 方法可见性范围：与运行时查找操作入口的口径一致（含非公开的受保护方法）。
	/// </summary>
	private const BindingFlags MethodFlags =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private readonly IReadOnlyDictionary<string, IReadOnlyList<Func<MethodInfo, bool>>> _rules;

	private readonly ConcurrentDictionary<(Type Type, string Operation), IReadOnlyList<PermissionAttribute>> _cache = new();

	/// <summary>
	/// 用已声明的操作全集与入口判定规则初始化权限码来源。
	/// </summary>
	/// <param name="operations">本来源负责的操作全集，构造时复制为只读快照。</param>
	/// <param name="rules">操作名到入口判定规则列表的映射，由构造器保证已按忽略大小写归一。</param>
	/// <remarks>规则字典按引用持有，调用方在构造后不应再改动它。</remarks>
	internal OperationCodeSource(
		IReadOnlyList<string> operations,
		IReadOnlyDictionary<string, IReadOnlyList<Func<MethodInfo, bool>>> rules)
	{
		// 快照 + 只读包装：调用方传进来的可能是可变 List / 数组，强转即可改动本来源的操作全集
		AllOperations = Array.AsReadOnly([.. operations]);
		_rules = rules;
	}

	/// <summary>
	/// 开始构造一个权限码来源。
	/// </summary>
	/// <returns>构造器。</returns>
	public static OperationCodeSourceBuilder Create() => new();

	/// <summary>
	/// 判断构造器是否已声明任何操作入口规则。
	/// </summary>
	/// <param name="builder">规则构造器。</param>
	/// <returns>已声明则返回 <see langword="true"/>。</returns>
	internal static bool HasDeclarations(OperationCodeSourceBuilder builder)
	{
		return builder.HasRules;
	}

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations { get; }

	/// <inheritdoc />
	/// <remarks>
	/// 按 <c>(类型, 操作)</c> 收集，只读<b>被本操作规则命中的方法</b>。
	/// 按被调用的方法收集请使用 <see cref="PermissionRequirements.For(Type, MethodInfo)"/>。
	/// </remarks>
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		if (type == null || operation == null || !_rules.ContainsKey(operation))
		{
			return [];
		}

		return _cache.GetOrAdd((type, operation), key =>
		{
			var (type, operation) = key;

			var requirements = new List<PermissionAttribute>();
			requirements.AddRange(type.GetCustomAttributes<PermissionAttribute>(true));

			foreach (var method in type.GetMethods(MethodFlags))
			{
				if (_rules[operation].Any(rule => rule(method)))
				{
					requirements.AddRange(method.GetCustomAttributes<PermissionAttribute>(true));
				}
			}

			return Array.AsReadOnly(requirements.ToArray());
		});
	}

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return RequirementsFor(type, operation)
		       .Select(requirement => requirement.Permission)
		       .Where(permission => !string.IsNullOrEmpty(permission))
		       .Distinct(StringComparer.OrdinalIgnoreCase)
		       .ToArray();
	}
}

/// <summary>
/// <see cref="OperationCodeSource"/> 的构造器：把「操作入口判定」写成数据。
/// </summary>
internal sealed class OperationCodeSourceBuilder
{
	// 操作名按忽略大小写匹配：配置里写 "Read" 与运行时查询 "read" 必须命中同一组规则。
	private readonly Dictionary<string, List<Func<MethodInfo, bool>>> _rules = new(StringComparer.OrdinalIgnoreCase);

	private readonly List<string> _order = [];

	/// <summary>
	/// 获取一个值，指示是否已声明任何操作入口规则。
	/// </summary>
	internal bool HasRules => _order.Count > 0;

	/// <summary>
	/// 声明「打了指定特性的方法即该操作的入口」。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="attributeTypes">入口特性类型。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	public OperationCodeSourceBuilder OnAttribute(string operation, params Type[] attributeTypes)
	{
		ArgumentNullException.ThrowIfNull(attributeTypes);

		foreach (var attributeType in attributeTypes)
		{
			// 在这里挡住非法元素：否则规则会带着一个永远匹配不上（或求值时才炸）的类型活到判定期
			Check.Ensure(
				attributeType != null && typeof(Attribute).IsAssignableFrom(attributeType),
				Resources.IDS_OPERATION_ATTRIBUTE_TYPE_INVALID);

			// 上面的 Check.Ensure 已拒绝 null，闭包捕获的因此是确定非空的类型。
			OnMethod(operation, method => method.IsDefined(attributeType!, true));
		}

		return this;
	}

	/// <summary>
	/// 声明「方法名在约定集合内的方法即该操作的入口」。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="names">方法名；大小写敏感。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	/// <remarks>
	/// 约定名通常由 <see cref="OperationConventions.Names"/> 推导，不必手写。
	/// </remarks>
	public OperationCodeSourceBuilder OnMethodName(string operation, params string[] names)
	{
		ArgumentNullException.ThrowIfNull(names);

		var set = new HashSet<string>(names, StringComparer.Ordinal);
		return OnMethod(operation, method => set.Contains(method.Name));
	}

	/// <summary>
	/// 声明「打上指定特性<b>或</b>按其推导的约定名匹配的方法，即该操作的入口」。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="namePrefix">特性名中要剥离的前缀；为 <see langword="null"/> 时不去前缀。</param>
	/// <param name="attributeTypes">入口特性类型；一个操作可以有多个（如 <c>Create</c> 与 <c>Insert</c> 同义）。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	/// <remarks>
	/// 这是各框架最常见的形态，因此提供为一步到位的入口。特性与命名两条约定<b>并存</b>：
	/// 只按特性收集会让按命名约定声明的入口方法上的权限声明被静默忽略。
	/// </remarks>
	public OperationCodeSourceBuilder OnAttributeOrName(string operation, string namePrefix, params Type[] attributeTypes)
	{
		ArgumentNullException.ThrowIfNull(attributeTypes);

		OnAttribute(operation, attributeTypes);

		return OnMethodName(
			operation,
			[.. attributeTypes.SelectMany(type => OperationConventions.Names(type, namePrefix)).Distinct(StringComparer.Ordinal)]);
	}

	/// <summary>
	/// 声明任意入口判定规则，用于前面几种约定都不适用的框架。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="predicate">判定方法是否为该操作入口的谓词。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	public OperationCodeSourceBuilder OnMethod(string operation, Func<MethodInfo, bool> predicate)
	{
		ScopeKeys.ValidateOperation(operation);
		ArgumentNullException.ThrowIfNull(predicate);

		if (!_rules.TryGetValue(operation, out var list))
		{
			list = [];
			_rules[operation] = list;
			_order.Add(operation);
		}

		list.Add(predicate);

		return this;
	}

	/// <summary>
	/// 构造权限码来源。
	/// </summary>
	/// <returns>权限码来源。</returns>
	/// <exception cref="InvalidOperationException">未声明任何规则时抛出。</exception>
	/// <remarks>
	/// 拒绝「没有规则」的构造结果：这样的来源对任何操作都不回答要求，与
	/// <see cref="EmptyCodeSource"/> 是两件不同的事。确实没有方法级权限码时，显式使用
	/// <see cref="EmptyCodeSource.Instance"/>。
	/// </remarks>
	public OperationCodeSource Build()
	{
		Check.Ensure(
			_order.Count > 0,
			Resources.IDS_OPERATION_RULE_REQUIRED);

		var rules = _rules.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyList<Func<MethodInfo, bool>>)[.. pair.Value],
			StringComparer.OrdinalIgnoreCase);

		return new OperationCodeSource([.. _order], rules);
	}
}
