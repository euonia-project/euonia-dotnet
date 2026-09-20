using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象上 <see cref="PermissionAttribute"/> 要求的收集入口。
/// </summary>
/// <remarks>
/// <para>
/// 方法级要求的收集范围与工厂方法的查找范围一致（<see cref="ObjectReflector.IsFactoryMethod"/>）：
/// 既包含标记了工厂方法特性的方法，也包含符合命名约定的方法。若只按特性收集，
/// 以命名约定声明的工厂方法上的 <see cref="PermissionAttribute"/> 会被静默忽略，导致权限形同虚设。
/// </para>
/// <para>
/// 本类同时服务于运行期判定（<see cref="BusinessObject"/>）与启动期校验
/// （<see cref="ScopeModelRegistry"/>），确保两处对「某个操作声明了哪些权限码」不会得出不同答案。
/// </para>
/// </remarks>
internal static class PermissionRequirements
{
	private static readonly ConcurrentDictionary<(Type Type, BusinessOperation Operation), PermissionAttribute[]> _cache = new();

	/// <summary>
	/// 收集类型级与执行指定操作的工厂方法上的权限要求。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限要求数组；结果按（类型，操作）缓存。</returns>
	internal static IReadOnlyList<PermissionAttribute> For(Type type, BusinessOperation operation)
	{
		return _cache.GetOrAdd((type, operation), static key =>
		{
			var (type, operation) = key;
			var factoryAttributeTypes = GetFactoryAttributeTypes(operation);
			var requirements = new List<PermissionAttribute>();

			requirements.AddRange(type.GetCustomAttributes<PermissionAttribute>(true));

			if (factoryAttributeTypes.Length > 0)
			{
				foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (factoryAttributeTypes.Any(factoryAttributeType => ObjectReflector.IsFactoryMethod(method, factoryAttributeType)))
					{
						requirements.AddRange(method.GetCustomAttributes<PermissionAttribute>(true));
					}
				}
			}

			return requirements.ToArray();
		});
	}

	/// <summary>
	/// 收集指定操作上声明的权限码（忽略空的权限名）。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限码集合。</returns>
	internal static IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation)
	{
		return For(type, operation)
		       .Select(requirement => requirement.Permission)
		       .Where(permission => !string.IsNullOrEmpty(permission))
		       .ToArray();
	}

	/// <summary>
	/// 获取指定操作对应的工厂方法特性类型（一个操作可能对应多个特性）。
	/// </summary>
	/// <param name="operation">业务操作。</param>
	/// <returns>工厂方法特性类型数组。</returns>
	internal static Type[] GetFactoryAttributeTypes(BusinessOperation operation)
	{
		return operation switch
		{
			BusinessOperation.Read => [typeof(FactoryFetchAttribute)],
			BusinessOperation.Create => [typeof(FactoryCreateAttribute), typeof(FactoryInsertAttribute)],
			BusinessOperation.Update => [typeof(FactoryUpdateAttribute)],
			BusinessOperation.Delete => [typeof(FactoryDeleteAttribute)],
			BusinessOperation.Execute => [typeof(FactoryExecuteAttribute)],
			_ => []
		};
	}

	/// <summary>
	/// 获取框架已知的全部业务操作。
	/// </summary>
	internal static IReadOnlyList<BusinessOperation> AllOperations { get; } =
	[
		BusinessOperation.Read,
		BusinessOperation.Create,
		BusinessOperation.Update,
		BusinessOperation.Delete,
		BusinessOperation.Execute
	];
}
