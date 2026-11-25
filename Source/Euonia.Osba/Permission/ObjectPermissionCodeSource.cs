using System.Collections.Concurrent;
using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 兼容入口：按<b>工厂方法约定</b>收集类型级与方法级 <see cref="PermissionAttribute"/>，按 <c>(类型, 操作)</c> 回答。
/// </summary>
/// <remarks>
/// <para>
/// 规则表就是「哪个工厂方法特性对应哪个操作」，与对象工厂查找工厂方法用的是<b>同一套候选口径</b>
/// （<see cref="ObjectReflector.GetFactoryMethods"/>）：特性命中，或方法名落在该特性推导出的约定名里。
/// 按「类型 + 被调用的方法」收集要求由 <see cref="PermissionRequirements.For(Type, MethodInfo)"/> 完成。
/// </para>
/// <para>
/// 宿主显式登记 <c>Source(ObjectPermissionCodeSource.Instance)</c> 时，本来源作为一份权限码来源参与要求并集。
/// </para>
/// <para>结果按（类型，操作）缓存，反复调用不会重复反射。</para>
/// </remarks>
[Obsolete("按 (type, operation) 查询的兼容类型。请使用 PermissionRequirements / IPermissionRequirementSource；本类型将在下个主版本移除。")]
public sealed class ObjectPermissionCodeSource : IPermissionCodeSource
{
	/// <summary>
	/// 操作 → 工厂方法特性。一个操作可以有多个同义特性（如创建与插入）。
	/// </summary>
	private static readonly (string Operation, Type[] AttributeTypes)[] Rules =
	[
		(BusinessOperation.Read, [typeof(FactoryFetchAttribute)]),
		(BusinessOperation.Create, [typeof(FactoryCreateAttribute), typeof(FactoryInsertAttribute)]),
		(BusinessOperation.Update, [typeof(FactoryUpdateAttribute)]),
		(BusinessOperation.Delete, [typeof(FactoryDeleteAttribute)]),
		(BusinessOperation.Execute, [typeof(FactoryExecuteAttribute)])
	];

	/// <summary>
	/// 共享实例：宿主显式登记 <c>Source(ObjectPermissionCodeSource.Instance)</c> 时使用。
	/// </summary>
	public static ObjectPermissionCodeSource Instance { get; } = new();

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
