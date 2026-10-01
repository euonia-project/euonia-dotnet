using System.Collections.Concurrent;
using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Osba 的默认权限要求来源：按<b>工厂方法约定</b>收集类型级与方法级的 <see cref="PermissionAttribute"/>。
/// </summary>
/// <remarks>
/// <para>
/// 规则表就是「哪个工厂方法特性对应哪个操作」，与对象工厂查找工厂方法用的是<b>同一套候选口径</b>
/// （<see cref="ObjectReflector.GetFactoryMethods"/>）：特性命中，或方法名落在该特性推导出的约定名里。
/// 两个口径同源是刻意的——「工厂会调用的方法」与「权限扫描会看的方法」一旦分叉，
/// 就会出现在某个方法上声明了权限却从不生效（或反之）的静默缺口。
/// </para>
/// <para>
/// 本实现的实例在 Osba 内以静态单例存在（<see cref="Instance"/>），用于「宿主没有注册任何
/// <see cref="IPermissionCodeSource"/>」时的兜底：声明了要求但无人判定必须报错，
/// 而不是因为没装权限实现就静默放行。
/// </para>
/// <para>结果按（类型，操作）缓存，反复调用不会重复反射。</para>
/// </remarks>
public sealed class ObjectPermissionRequirementProvider : IPermissionCodeSource
{
	/// <summary>操作 → 工厂方法特性。一个操作可以有多个同义特性（如创建与插入）。</summary>
	private static readonly (string Operation, Type[] AttributeTypes)[] Rules =
	[
		(BusinessOperation.Read, [typeof(FactoryFetchAttribute)]),
		(BusinessOperation.Create, [typeof(FactoryCreateAttribute), typeof(FactoryInsertAttribute)]),
		(BusinessOperation.Update, [typeof(FactoryUpdateAttribute)]),
		(BusinessOperation.Delete, [typeof(FactoryDeleteAttribute)]),
		(BusinessOperation.Execute, [typeof(FactoryExecuteAttribute)])
	];

	/// <summary>
	/// 默认实例：宿主未注册 <see cref="IPermissionCodeSource"/> 时由工厂边界兜底使用；
	/// 引擎适配包也用它作为注册期的约定来源。
	/// </summary>
	public static ObjectPermissionRequirementProvider Instance { get; } = new();

	private readonly ConcurrentDictionary<(Type Type, string Operation), PermissionAttribute[]> _cache = new();

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => BusinessOperation.All;

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return
		[
			.. RequirementsFor(type, operation)
			   .Select(requirement => requirement.Permission)
			   .Where(permission => !string.IsNullOrEmpty(permission))
			   .Distinct(StringComparer.OrdinalIgnoreCase)
		];
	}

	/// <inheritdoc />
	/// <remarks>
	/// 覆写默认实现：本来源能表达<b>角色</b>要求（声明里带的角色），因此不折算成「有码、无角色」。
	/// </remarks>
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		var attributeTypes = AttributeTypesOf(operation);

		// 本来源只认识规则表里的操作：其余操作不参与判定（与「只有声明过规则的操作才属于本来源」一致）
		if (type == null || attributeTypes.Length == 0)
		{
			return [];
		}

		return _cache.GetOrAdd((type, operation), key => Collect(key.Type, attributeTypes));
	}

	private static Type[] AttributeTypesOf(string operation)
	{
		foreach (var (name, attributeTypes) in Rules)
		{
			if (string.Equals(name, operation, StringComparison.OrdinalIgnoreCase))
			{
				return attributeTypes;
			}
		}

		return [];
	}

	private static PermissionAttribute[] Collect(Type type, Type[] attributeTypes)
	{
		var requirements = new List<PermissionAttribute>();

		// 类型级要求适用于该类型支持的全部操作（inherit: true，派生类型同样适用）
		requirements.AddRange(type.GetCustomAttributes<PermissionAttribute>(true));

		foreach (var attributeType in attributeTypes)
		{
			foreach (var method in ObjectReflector.GetFactoryMethods(type, attributeType))
			{
				requirements.AddRange(method.GetCustomAttributes<PermissionAttribute>(true));
			}
		}

		return [.. requirements];
	}
}
