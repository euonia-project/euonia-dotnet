using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

// Adapt() 需要引用标记为 [Obsolete] 的 IPermissionCodeSource。
#pragma warning disable CS0618

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限声明读取器：直接读写在<b>类型</b>与<b>方法</b>上的 <see cref="PermissionAttribute"/>，不需要任何登记。
/// </summary>
/// <remarks>
/// <para>
/// 这是「码即声明」的落点：发现权限码不依赖操作集合，因此新增权限码或新增操作时，
/// 配置一行都不用改。判定路径只命中下面的缓存，不产生任何新的反射。
/// </para>
/// <para>
/// <b>规范化</b>：权限码按 <see cref="string.Trim()"/> 裁剪后保存（<c>" order:read"</c> 与 <c>"order:read"</c>
/// 必须是同一个码，否则它永远匹配不上授予）；裁剪后的副本保留原来的
/// <see cref="PermissionAttribute.Message"/>。
/// </para>
/// <para>
/// <b>缓存</b>：类型按 <see cref="Type"/> 缓存，方法按 <c>(Module, MetadataToken)</c> 缓存——反射不保证跨调用
/// 返回同一个 <see cref="MethodInfo"/> 实例，用引用相等作键会让缓存形同虚设。返回值是
/// <see cref="List{T}.AsReadOnly"/> 包装后的只读快照，调用方无法强转改写。
/// 「运行时类型 + 静态方法」的实际实现解析结果同样按 <c>(运行时类型, Module, MetadataToken)</c> 缓存，
/// 判定热路径不产生新的反射。
/// </para>
/// <para>
/// <b>运行时实现解析</b>：工厂按静态类型查找方法、反射调用却按运行时类型虚分派，派生 override 上的声明
/// 不在静态方法的基类链上。因此按「类型 + 方法」收集时，方法级要求取「静态方法声明 ∪ 运行时类型上实际实现声明」
/// 的并集：虚方法沿继承链用 <see cref="MethodInfo.GetBaseDefinition"/> 反向匹配实际 override，
/// <c>new</c> 遮蔽按名称 + 参数签名逐层查找。类型级要求一律按传入类型（调用方应传目标的运行时类型）收集。
/// </para>
/// <para>
/// <b>去重口径</b>：权限码按 <see cref="StringComparer.OrdinalIgnoreCase"/>、角色按
/// <see cref="StringComparer.Ordinal"/> 比较，保持首次出现顺序。
/// </para>
/// </remarks>
public static class PermissionRequirements
{
	/// <summary>
	/// 类型级声明缓存（含继承链）。
	/// </summary>
	private static readonly ConcurrentDictionary<Type, IReadOnlyList<PermissionAttribute>> TypeCache = new();

	/// <summary>
	/// 方法级声明缓存（含 override 链）：键用程序集模块与元数据标记，而不是 <see cref="MethodInfo"/> 引用。
	/// </summary>
	private static readonly ConcurrentDictionary<(Module Module, int MetadataToken), IReadOnlyList<PermissionAttribute>> MethodCache = new();

	/// <summary>
	/// 「运行时类型 + 静态方法」的方法级并集缓存：键用运行时类型与方法的模块 + 元数据标记，
	/// 值是「静态方法声明 ∪ 实际实现声明」的只读快照；解析只发生在首次访问。
	/// </summary>
	private static readonly ConcurrentDictionary<(Type RuntimeType, Module Module, int MetadataToken), IReadOnlyList<PermissionAttribute>> ImplementationCache = new();

	/// <summary>
	/// 按 <c>(type, operation)</c> 回答的来源的适配器缓存：同一来源实例只创建一个适配器。
	/// </summary>
	private static readonly ConditionalWeakTable<IPermissionCodeSource, IPermissionRequirementSource> Adapters = new();

	/// <summary>
	/// 实际实现解析的反射范围：只读当前层自己声明的方法（含非公开），逐层上溯由解析循环负责。
	/// </summary>
	private const BindingFlags ImplementationFlags =
		BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

	/// <summary>
	/// 获取指定类型上声明的全部权限要求（类型级，含继承链；不含任何方法级声明）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <returns>只读的权限要求快照；没有声明时返回空集合。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/> 时抛出。</exception>
	public static IReadOnlyList<PermissionAttribute> For(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		return TypeCache.GetOrAdd(type, static candidate => Snapshot(candidate.GetCustomAttributes<PermissionAttribute>(true)));
	}

	/// <summary>
	/// 获取指定方法上声明的全部权限要求（方法级，含 override 链；不含任何类型级声明）。
	/// </summary>
	/// <param name="method">要读取声明的方法。</param>
	/// <returns>只读的权限要求快照；没有声明时返回空集合。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="method"/> 为 <see langword="null"/> 时抛出。</exception>
	public static IReadOnlyList<PermissionAttribute> For(MethodInfo method)
	{
		ArgumentNullException.ThrowIfNull(method);

		return MethodCache.GetOrAdd((method.Module, method.MetadataToken), _ => Snapshot(method.GetCustomAttributes<PermissionAttribute>(true)));
	}

	/// <summary>
	/// 收集类型级与方法级声明的并集；<paramref name="method"/> 为 <see langword="null"/> 时只返回类型级。
	/// </summary>
	/// <param name="type">资源类型；工厂边界强制时应传入目标的<b>运行时类型</b>，派生类型上的声明才不会漏判。</param>
	/// <param name="method">将要执行的方法（可以是按静态类型查找到的声明）；为 <see langword="null"/> 表示只收集类型级要求。</param>
	/// <returns>按「码 + 角色」去重后的只读权限要求快照，保持类型级在前、方法级在后的首次出现顺序。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// <para>
	/// 这正是方法级隔离的实现口径：只读<b>这一个</b>方法上的声明，从不遍历同类型的其它方法，
	/// 因此 <c>UpdateAsync</c> 上的码不可能出现在 <c>ArchiveAsync</c> 的调用路径上。
	/// 类型级与方法级只有一侧存在时直接返回那一侧的缓存快照，不额外分配。
	/// </para>
	/// <para>
	/// 方法级要求按「静态方法声明 ∪ 实际实现声明」取并集：<paramref name="method"/> 未声明在
	/// <paramref name="type"/> 上时，先在 <paramref name="type"/> 的继承链上解析实际实现——虚方法用
	/// <see cref="MethodInfo.GetBaseDefinition"/> 反向匹配实际 override，<c>new</c> 遮蔽按名称 + 参数签名
	/// 逐层查找——再把实现上的声明（含其基类链）并入。反射调用按运行时类型虚分派，派生 override 上的
	/// <see cref="PermissionAttribute"/> 不在静态方法的基类链上，只读静态方法会静默放行。
	/// 解析结果按「运行时类型 + 方法」缓存，判定热路径不产生新的反射。
	/// </para>
	/// </remarks>
	public static IReadOnlyList<PermissionAttribute> For(Type type, MethodInfo? method)
	{
		var typeRequirements = For(type);

		if (method == null)
		{
			return typeRequirements;
		}

		var methodRequirements = ForImplementation(type, method);

		if (methodRequirements.Count == 0)
		{
			return typeRequirements;
		}

		return typeRequirements.Count == 0 ? methodRequirements : Merge(typeRequirements, methodRequirements);
	}

	/// <summary>
	/// 读取方法级声明：方法就声明在 <paramref name="runtimeType"/> 上时直接命中方法缓存；
	/// 否则解析运行时类型上的实际实现，取「静态方法声明 ∪ 实际实现声明」的并集（按运行时类型 + 方法缓存）。
	/// </summary>
	/// <param name="runtimeType">目标的运行时类型。</param>
	/// <param name="method">按静态类型查找到的方法声明。</param>
	/// <returns>只读的方法级要求快照。</returns>
	private static IReadOnlyList<PermissionAttribute> ForImplementation(Type runtimeType, MethodInfo method)
	{
		if (method.DeclaringType == runtimeType)
		{
			return For(method);
		}

		return ImplementationCache.GetOrAdd(
			(runtimeType, method.Module, method.MetadataToken),
			static (key, declared) =>
			{
				var implementation = ResolveImplementation(key.RuntimeType, declared);
				return implementation == null ? For(declared) : Merge(For(declared), For(implementation));
			},
			method);
	}

	/// <summary>
	/// 在运行时类型的继承链上解析实际执行的方法：自最派生层逐层向上，直到静态方法的声明层为止。
	/// 虚方法优先返回 <see cref="MethodInfo.GetBaseDefinition"/> 相同的实际 override（<c>new</c> 遮蔽不参与该虚槽的分派）；
	/// 没有 override 时返回最派生的同名同签名方法，覆盖 <c>new</c> 遮蔽与非虚方法的情形。
	/// </summary>
	/// <param name="runtimeType">目标的运行时类型。</param>
	/// <param name="method">按静态类型查找到的方法声明。</param>
	/// <returns>实际实现的方法；继承链上没有同名同签名方法时返回 <see langword="null"/>。</returns>
	private static MethodInfo? ResolveImplementation(Type runtimeType, MethodInfo method)
	{
		var baseDefinition = method.IsVirtual ? method.GetBaseDefinition() : null;
		MethodInfo? shadowed = null;

		for (var type = runtimeType; type != null && type != typeof(object) && type != method.DeclaringType; type = type.BaseType)
		{
			foreach (var candidate in type.GetMethods(ImplementationFlags))
			{
				if (!SignatureMatches(candidate, method))
				{
					continue;
				}

				// MethodInfo 不保证跨反射调用返回同一实例，必须用 Equals（按模块 + 元数据标记）而不是引用相等
				if (baseDefinition != null && candidate.GetBaseDefinition().Equals(baseDefinition))
				{
					return candidate;
				}

				shadowed ??= candidate;
			}
		}

		return shadowed;
	}

	/// <summary>
	/// 判断两个方法是否同名且参数签名一致（参数个数与逐个参数类型相同）。
	/// </summary>
	/// <param name="candidate">候选方法。</param>
	/// <param name="method">静态查找到的方法。</param>
	/// <returns>签名一致则返回 <see langword="true"/>。</returns>
	private static bool SignatureMatches(MethodInfo candidate, MethodInfo method)
	{
		if (!string.Equals(candidate.Name, method.Name, StringComparison.Ordinal))
		{
			return false;
		}

		var candidateParameters = candidate.GetParameters();
		var parameters = method.GetParameters();

		if (candidateParameters.Length != parameters.Length)
		{
			return false;
		}

		for (var index = 0; index < parameters.Length; index++)
		{
			if (candidateParameters[index].ParameterType != parameters[index].ParameterType)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 合并多组权限要求并按「码 + 角色」去重，保持首次出现顺序。
	/// </summary>
	/// <param name="sets">要合并的要求集合；<see langword="null"/> 项按空集合处理。</param>
	/// <returns>只读的合并结果；全部为空时返回空集合。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="sets"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 供多模块 / 多来源合并使用：同一个码可能被类型级声明、方法级声明与来源同时给出，
	/// 只保留首次出现的那一条，避免同一判定看到两条只差大小写的重复要求。
	/// 合并时同样裁剪权限码，来源给出的 <c>" order:read"</c> 也能命中授予。
	/// </remarks>
	public static IReadOnlyList<PermissionAttribute> Merge(params IEnumerable<PermissionAttribute>[] sets)
	{
		ArgumentNullException.ThrowIfNull(sets);

		var merged = new List<PermissionAttribute>();
		var seen = new HashSet<PermissionAttribute>(RequirementComparer.Instance);

		foreach (var set in sets)
		{
			if (set == null)
			{
				continue;
			}

			foreach (var declaration in set)
			{
				if (declaration == null)
				{
					continue;
				}

				var requirement = Trim(declaration);

				if (seen.Add(requirement))
				{
					merged.Add(requirement);
				}
			}
		}

		return merged.AsReadOnly();
	}

	/// <summary>
	/// 把按 <c>(type, operation)</c> 回答的来源包装成 <see cref="IPermissionRequirementSource"/>。
	/// </summary>
	/// <param name="source">按 <c>(type, operation)</c> 回答的来源。</param>
	/// <returns>适配器；对同一来源实例返回同一个适配器（按弱引用缓存，不阻止来源被回收）。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 来源没有方法上下文：适配后忽略 <c>method</c>，仍按 <c>(type, operation)</c> 回答。
	/// </remarks>
	public static IPermissionRequirementSource Adapt(IPermissionCodeSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		return Adapters.GetValue(source, static candidate => new AdaptedRequirementSource(candidate));
	}

	/// <summary>
	/// 读取一组声明并构造只读快照：逐条裁剪权限码、复制角色数组，并按「码 + 角色」去重。
	/// </summary>
	/// <param name="declarations">反射读到的声明。</param>
	/// <returns>只读快照。</returns>
	private static IReadOnlyList<PermissionAttribute> Snapshot(IEnumerable<PermissionAttribute> declarations)
	{
		var requirements = new List<PermissionAttribute>();
		var seen = new HashSet<PermissionAttribute>(RequirementComparer.Instance);

		foreach (var declaration in declarations)
		{
			if (declaration == null)
			{
				continue;
			}

			var requirement = Copy(declaration);

			if (seen.Add(requirement))
			{
				requirements.Add(requirement);
			}
		}

		return requirements.AsReadOnly();
	}

	/// <summary>
	/// 复制一条声明：缓存里必须是自有实例，否则调用方改 <see cref="PermissionAttribute.Roles"/> 的数组元素
	/// 就能污染整份缓存。
	/// </summary>
	/// <param name="declaration">原声明。</param>
	/// <returns>裁剪并复制后的声明。</returns>
	private static PermissionAttribute Copy(PermissionAttribute declaration)
	{
		return new PermissionAttribute(TrimCode(declaration.Permission), [.. declaration.Roles])
		{
			Message = declaration.Message
		};
	}

	/// <summary>
	/// 裁剪权限码；裁剪后与原文一致时返回原实例，避免判定路径上的多余分配。
	/// </summary>
	/// <param name="declaration">原声明。</param>
	/// <returns>已裁剪的声明。</returns>
	private static PermissionAttribute Trim(PermissionAttribute declaration)
	{
		var permission = declaration.Permission;

		return string.Equals(TrimCode(permission), permission, StringComparison.Ordinal)
			? declaration
			: Copy(declaration);
	}

	/// <summary>
	/// 裁剪权限码；<see langword="null"/> 归一为空字符串（两者在判定上等价，都表示「不校验权限码」）。
	/// </summary>
	/// <param name="permission">原始权限码。</param>
	/// <returns>裁剪后的权限码。</returns>
	private static string TrimCode(string? permission)
	{
		return permission?.Trim() ?? string.Empty;
	}

	/// <summary>
	/// 要求去重比较器：权限码忽略大小写，角色按序数比较。
	/// </summary>
	private sealed class RequirementComparer : IEqualityComparer<PermissionAttribute>
	{
		/// <summary>
		/// 共享实例。
		/// </summary>
		internal static RequirementComparer Instance { get; } = new();

		/// <inheritdoc />
		public bool Equals(PermissionAttribute? x, PermissionAttribute? y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			if (!string.Equals(x.Permission, y.Permission, StringComparison.OrdinalIgnoreCase) || x.Roles.Length != y.Roles.Length)
			{
				return false;
			}

			for (var index = 0; index < x.Roles.Length; index++)
			{
				if (!string.Equals(x.Roles[index], y.Roles[index], StringComparison.Ordinal))
				{
					return false;
				}
			}

			return true;
		}

		/// <inheritdoc />
		public int GetHashCode(PermissionAttribute requirement)
		{
			var hash = new HashCode();

			// 码与角色的哈希必须与 Equals 的比较规则一致；null 码按空字符串参与（TrimCode 已归一）
			hash.Add(requirement.Permission == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(requirement.Permission));

			foreach (var role in requirement.Roles)
			{
				hash.Add(role == null ? 0 : StringComparer.Ordinal.GetHashCode(role));
			}

			return hash.ToHashCode();
		}
	}

	/// <summary>
	/// 按 <c>(type, operation)</c> 回答的来源的适配器：转交给 <see cref="IPermissionRequirementSource"/>，忽略 <c>method</c>。
	/// </summary>
	private sealed class AdaptedRequirementSource(IPermissionCodeSource source) : IPermissionRequirementSource
	{
		/// <inheritdoc />
		public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, MethodInfo? method, string operation)
		{
			return source.RequirementsFor(type, operation);
		}
	}
}

#pragma warning restore CS0618
