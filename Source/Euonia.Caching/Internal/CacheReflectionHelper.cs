using System.Globalization;
using System.Reflection;

namespace Nerosoft.Euonia.Caching.Internal;

internal static class CacheReflectionHelper
{
	/// <summary>
	/// 根据配置创建缓存背板实例。如果配置中定义了背板类型，则通过反射实例化背板；
	/// 否则返回 <c>null</c>。
	/// </summary>
	/// <param name="configuration">缓存管理器配置。</param>
	/// <returns>创建的 <see cref="CacheBackplane"/> 实例；未配置背板时返回 <c>null</c>。</returns>
	/// <exception cref="InvalidOperationException">当配置了背板但没有任何缓存句柄标记为背板源，或背板类型未继承 <see cref="CacheBackplane"/> 时抛出。</exception>
	internal static CacheBackplane CreateBackplane(CacheManagerConfiguration configuration)
	{
		Check.EnsureNotNull(configuration, nameof(configuration));

		if (configuration.BackplaneType != null)
		{
			if (!configuration.CacheHandleConfigurations.Any(p => p.IsBackplaneSource))
			{
				throw new InvalidOperationException(
					"At least one cache handle must be marked as the backplane source if a backplane is defined via configuration.");
			}

			CheckExtends<CacheBackplane>(configuration.BackplaneType);

			var args = new object[] { configuration };
			if (configuration.BackplaneTypeArguments != null)
			{
				args = configuration.BackplaneTypeArguments.Concat(args).ToArray();
			}

			return (CacheBackplane)CreateInstance(configuration.BackplaneType, args);
		}

		return null;
	}

	/// <summary>
	/// 根据缓存管理器配置创建所有缓存句柄实例。
	/// </summary>
	/// <typeparam name="TCacheValue">缓存值的类型。</typeparam>
	/// <param name="manager">缓存管理器实例。</param>
	/// <returns>创建的缓存句柄集合。</returns>
	/// <exception cref="InvalidOperationException">当未定义任何缓存句柄、句柄类型无效或初始化失败时抛出。</exception>
	internal static ICollection<BaseCacheHandle<TCacheValue>> CreateCacheHandles<TCacheValue>(BaseCacheManager<TCacheValue> manager)
	{
		Check.EnsureNotNull(manager, nameof(manager));
		var managerConfiguration = manager.Configuration;
		var handles = new List<BaseCacheHandle<TCacheValue>>();

		foreach (var handleConfiguration in managerConfiguration.CacheHandleConfigurations)
		{
			var handleType = handleConfiguration.HandleType;

			Type instanceType;

			ValidateCacheHandleGenericTypeArguments(handleType);

			// 如果配置的类型没有泛型类型定义（未定义 <T>）
			if (handleType.GetTypeInfo().IsGenericTypeDefinition)
			{
				instanceType = handleType.MakeGenericType(typeof(TCacheValue));
			}
			else
			{
				instanceType = handleType;
			}

			var types = new List<object>([managerConfiguration, manager, handleConfiguration]);
			if (handleConfiguration.ConfigurationTypes.Length > 0)
			{
				types.AddRange(handleConfiguration.ConfigurationTypes);
			}


			if (CreateInstance(instanceType, types.ToArray()) is not BaseCacheHandle<TCacheValue> instance)
			{
				throw new InvalidOperationException("Couldn't initialize handle of type " + instanceType.FullName);
			}

			handles.Add(instance);
		}

		if (handles.Count == 0)
		{
			throw new InvalidOperationException("No cache handles defined.");
		}

		// 验证背板是缓存管理器中的最后一个句柄（仅在配置了背板时检查）
		if (handles.Any(p => p.Configuration.IsBackplaneSource) && manager.Configuration.BackplaneType != null)
		{
			if (!handles.Last().Configuration.IsBackplaneSource)
			{
				throw new InvalidOperationException("The last cache handle should be the backplane source.");
			}
		}

		{
		}

		return handles;
	}

	/// <summary>
	/// 通过反射创建指定类型的实例，使用已知实例列表匹配构造函数参数。
	/// </summary>
	/// <param name="instanceType">要创建的实例类型。</param>
	/// <param name="knownInstances">用于匹配构造函数参数的已知实例列表。</param>
	/// <returns>创建的实例。</returns>
	/// <exception cref="InvalidOperationException">当未找到匹配的构造函数或实例初始化失败时抛出。</exception>
	internal static object CreateInstance(Type instanceType, object[] knownInstances)
	{
		var constructors = instanceType.GetTypeInfo().DeclaredConstructors;

		constructors = constructors.Where(p => !p.IsStatic && p.IsPublic)
		                           .OrderByDescending(p => p.GetParameters().Length)
		                           .ToArray();

		if (!constructors.Any())
		{
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No matching public non static constructor found for type {0}.", instanceType.FullName));
		}

		var args = MatchArguments(constructors, knownInstances);

		try
		{
			return Activator.CreateInstance(instanceType, args);
		}
		catch (Exception ex)
		{
			var exception = ex.InnerException ?? ex;

			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Failed to initialize instance of type {0}.", instanceType), exception);
		}
	}

	/// <summary>
	/// 在构造函数集合中匹配参数，从已知实例列表中选择与参数类型兼容的实例。
	/// </summary>
	/// <param name="constructors">构造函数集合（按参数数量降序排列）。</param>
	/// <param name="instances">可用于匹配参数的已知实例列表。</param>
	/// <returns>匹配到的构造函数参数数组。</returns>
	/// <exception cref="InvalidOperationException">当找不到匹配的构造函数时抛出。</exception>
	private static object[] MatchArguments(IEnumerable<ConstructorInfo> constructors, object[] instances)
	{
		ParameterInfo lastParamMiss = null;
		ConstructorInfo lastCtor = null;

		foreach (var constructor in constructors)
		{
			lastCtor = constructor;
			var args = new List<object>();
			var parameters = constructor.GetParameters();
			var instancesCopy = new List<object>(instances);

			foreach (var param in parameters)
			{
				var paramValue = instancesCopy
				                 .Where(p => p != null)
				                 .FirstOrDefault(p => param.ParameterType.GetTypeInfo().IsAssignableFrom(p.GetType().GetTypeInfo()));

				if (paramValue == null)
				{
					lastParamMiss = param;
					break;
				}

				// 修复 #112：不重复使用已添加到参数列表中的同一实例
				instancesCopy.Remove(paramValue);
				args.Add(paramValue);
			}

			if (parameters.Length == args.Count)
			{
				return args.ToArray();
			}
		}

		if (constructors.Any(p => p.GetParameters().Length == 0))
		{
			// 未找到匹配，将尝试无参构造函数
			return [];
		}

		// 给出更详细的失败信息
		if (lastCtor != null && lastParamMiss != null)
		{
			var ctorTypes = string.Join(", ", lastCtor.GetParameters().Select(p => p.ParameterType.Name).ToArray());

			throw new InvalidOperationException(
				$"Could not find a matching constructor for type '{lastCtor.DeclaringType?.Name}'. Trying to match [{ctorTypes}] but missing {lastParamMiss.ParameterType.Name}");
		}

		{
		}

		throw new InvalidOperationException(
			$"Could not find a matching or empty constructor for type '{lastCtor?.DeclaringType?.Name}'.");
	}

	/// <summary>
	/// 递归获取指定类型的泛型基类类型定义列表。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <returns>泛型基类类型定义序列。</returns>
	private static IEnumerable<Type> GetGenericBaseTypes(this Type type)
	{
		var baseType = type.GetTypeInfo().BaseType;
		if (baseType == null || !baseType.GetTypeInfo().IsGenericType)
		{
			return Enumerable.Empty<Type>();
		}

		var genericBaseType = baseType.GetTypeInfo().IsGenericTypeDefinition ? baseType : baseType.GetGenericTypeDefinition();
		return Enumerable.Repeat(genericBaseType, 1)
		                 .Concat(baseType.GetGenericBaseTypes());
	}

	/// <summary>
	/// 验证缓存句柄类型：必须继承 <see cref="BaseCacheHandle{T}"/>，且不得带有已定义的泛型参数。
	/// </summary>
	/// <param name="handle">要验证的句柄类型。</param>
	/// <exception cref="InvalidOperationException">当句柄类型不满足要求时抛出。</exception>
	private static void ValidateCacheHandleGenericTypeArguments(Type handle)
	{
		// 由于调用方的泛型类型已被约束，此处并非严格必需
		if (handle.GetGenericBaseTypes().All(p => p != typeof(BaseCacheHandle<>)))
		{
			throw new InvalidOperationException(
				string.Format(
					CultureInfo.InvariantCulture,
					"Configured cache handle does not implement base cache handle [{0}].",
					handle.ToString()));
		}

		if (handle.IsGenericType && !handle.IsGenericTypeDefinition)
		{
			throw new InvalidOperationException(
				string.Format(
					CultureInfo.InvariantCulture,
					"Cache handle type [{0}] should not have any generic arguments defined.",
					handle.ToString()));
		}
	}

	/// <summary>
	/// 检查指定类型是否继承自 <typeparamref name="TValid"/>。
	/// </summary>
	/// <typeparam name="TValid">有效基类型。</typeparam>
	/// <param name="type">要检查的类型。</param>
	/// <exception cref="InvalidOperationException">当类型未继承自 <typeparamref name="TValid"/> 时抛出。</exception>
	private static void CheckExtends<TValid>(Type type)
	{
		var isExtendsType = type.IsExtends<TValid>(); //typeof(TValid).IsAssignableFrom(type);
		if (isExtendsType)
		{
			return;
		}

		throw new InvalidOperationException($"Type {type.FullName} does not extend from {typeof(TValid).Name}.");
	}
}