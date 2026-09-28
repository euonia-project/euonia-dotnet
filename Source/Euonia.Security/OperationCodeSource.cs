using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 通用权限码来源：用「操作入口判定规则」回答「某类型在某操作上声明了哪些权限码」。
/// </summary>
/// <remarks>
/// <para>
/// 这是 <see cref="IPermissionCodeSource"/> 最常见的实现，规则是<b>数据</b>而非代码：换框架只是换一组规则（见 README §3.3、§3.4）。
/// </para>
/// <para>
/// 收集范围与运行时判定一致：<b>类型级</b> <see cref="PermissionAttribute"/> 对本来源已声明的每个操作生效，
/// <b>方法级</b>只在该方法被本操作的规则识别为操作入口时才生效，否则入口方法上的权限声明会被静默忽略。
/// </para>
/// <para>
/// 操作词汇以 <see cref="AllOperations"/> 为准：<b>只有声明过规则的操作才属于本来源</b>，未声明规则的操作不参与判定。
/// </para>
/// <para>
/// 结果按（类型，操作）缓存，反复调用不会重复反射。
/// </para>
/// </remarks>
public sealed class OperationCodeSource : IPermissionCodeSource
{
	/// <summary>方法可见性范围：与运行时查找操作入口的口径一致（含非公开的受保护方法）。</summary>
	private const BindingFlags MethodFlags =
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private readonly IReadOnlyDictionary<string, IReadOnlyList<Func<MethodInfo, bool>>> _rules;

	private readonly ConcurrentDictionary<(Type Type, string Operation), PermissionAttribute[]> _cache = new();

	internal OperationCodeSource(
		IReadOnlyList<string> operations,
		IReadOnlyDictionary<string, IReadOnlyList<Func<MethodInfo, bool>>> rules)
	{
		AllOperations = operations;
		_rules = rules;
	}

	/// <summary>
	/// 开始构造一个权限码来源。
	/// </summary>
	/// <returns>构造器。</returns>
	public static OperationCodeSourceBuilder Create() => new();

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations { get; }

	/// <inheritdoc />
	/// <remarks>
	/// 供需要 <see cref="PermissionAttribute.Roles"/> 的运行期判定使用；
	/// 只关心权限码的调用方用 <see cref="CodesFor"/>。
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

			return requirements.ToArray();
		});
	}

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return RequirementsFor(type, operation)
		       .Select(requirement => requirement.Permission)
		       .Where(permission => !string.IsNullOrEmpty(permission))
		       .Distinct(StringComparer.Ordinal)
		       .ToArray();
	}
}

/// <summary>
/// <see cref="OperationCodeSource"/> 的构造器：把「操作入口判定」写成数据。
/// </summary>
public sealed class OperationCodeSourceBuilder
{
	private readonly Dictionary<string, List<Func<MethodInfo, bool>>> _rules = new(StringComparer.Ordinal);

	private readonly List<string> _order = [];

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
				"入口特性类型必须是非 null 的特性类型（Attribute 的派生类）。");

			OnMethod(operation, method => method.IsDefined(attributeType, true));
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
	/// 拒绝「没有规则」的来源：它会让所有方法级 <see cref="PermissionAttribute"/> 静默失效，
	/// 而这与 <see cref="EmptyCodeSource"/> 表达的是两件不同的事。
	/// 若确实没有方法级权限码，请显式使用 <see cref="EmptyCodeSource.Instance"/>。
	/// </remarks>
	public OperationCodeSource Build()
	{
		Check.Ensure(
			_order.Count > 0,
			"没有声明任何操作入口规则。请至少调用一次 OnAttribute / OnMethodName / OnAttributeOrName / OnMethod；"
			+ "若本应用确实没有方法级权限码，请改用 AddPermission(EmptyCodeSource.Instance, …) 显式断言；"
			+ "只想追加要扫描的程序集，请用 AddPermissionModels。");

		var rules = _rules.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyList<Func<MethodInfo, bool>>)[.. pair.Value],
			StringComparer.Ordinal);

		return new OperationCodeSource([.. _order], rules);
	}
}
