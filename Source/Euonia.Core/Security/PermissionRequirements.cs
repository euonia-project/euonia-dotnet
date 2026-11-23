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
/// 配置一行都不用改。判定路径只命中下面两处缓存，不产生任何新的反射。
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
	/// 按 <c>(type, operation)</c> 回答的来源的适配器缓存：同一来源实例只创建一个适配器。
	/// </summary>
	private static readonly ConditionalWeakTable<IPermissionCodeSource, IPermissionRequirementSource> Adapters = new();

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
	/// <param name="type">资源类型。</param>
	/// <param name="method">将要执行的方法；为 <see langword="null"/> 表示只收集类型级要求。</param>
	/// <returns>按「码 + 角色」去重后的只读权限要求快照，保持类型级在前、方法级在后的首次出现顺序。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="type"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 这正是方法级隔离的实现口径：只读<b>这一个</b>方法上的声明，从不遍历同类型的其它方法，
	/// 因此 <c>UpdateAsync</c> 上的码不可能出现在 <c>ArchiveAsync</c> 的调用路径上。
	/// 类型级与方法级只有一侧存在时直接返回那一侧的缓存快照，不额外分配。
	/// </remarks>
	public static IReadOnlyList<PermissionAttribute> For(Type type, MethodInfo? method)
	{
		var typeRequirements = For(type);

		if (method == null)
		{
			return typeRequirements;
		}

		var methodRequirements = For(method);

		if (methodRequirements.Count == 0)
		{
			return typeRequirements;
		}

		return typeRequirements.Count == 0 ? methodRequirements : Merge(typeRequirements, methodRequirements);
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
