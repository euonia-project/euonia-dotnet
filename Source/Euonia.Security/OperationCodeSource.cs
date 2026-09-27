using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 通用权限码来源：用「操作入口判定规则」回答「某类型在某操作上声明了哪些权限码」。
/// </summary>
/// <remarks>
/// <para>
/// 这是 <see cref="IPermissionCodeSource"/> 最常见的实现，规则是<b>数据</b>而非代码：
/// 「读取操作 = 打上 <c>FetchAttribute</c> 或名为 <c>Fetch</c>/<c>FetchAsync</c> 的方法」。
/// 换框架只是换一组规则，不需要换实现。
/// </para>
/// <para>
/// 收集范围与运行时判定保持一致：<b>类型级</b> <see cref="PermissionAttribute"/> 对本来源已声明的每个操作生效；
/// <b>方法级</b>只在该方法被本操作的规则识别为操作入口时才生效。
/// 否则以命名约定声明的入口方法上的权限声明会被静默忽略，权限形同虚设。
/// </para>
/// <para>
/// 操作词汇以 <see cref="AllOperations"/> 为准：<b>只有声明过规则的操作才属于本来源</b>，
/// 未声明规则的操作不会参与判定（此时方法级无从识别，类型级声明也不会被单独征用）。
/// 这是刻意的——否则「宿主没配这个操作」与「宿主配了这个操作但没人声明权限」将无法区分。
/// </para>
/// <para>
/// 结果按（类型，操作）缓存，因此反复调用不会重复反射。
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

	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（含类型级与方法级，保留特性上的角色等原始信息）。
	/// </summary>
	/// <param name="type">类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <returns>权限要求数组；结果按（类型，操作）缓存。</returns>
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
		Check.Ensure(_order.Count > 0, "没有声明任何操作入口规则。请至少调用一次 OnAttribute / OnMethodName / OnMethod；若本应用没有方法级权限码，请使用 EmptyCodeSource.Instance。");

		var rules = _rules.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyList<Func<MethodInfo, bool>>)[.. pair.Value],
			StringComparer.Ordinal);

		return new OperationCodeSource([.. _order], rules);
	}
}
