using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象反射器。
/// </summary>
public class ObjectReflector
{
	private const BindingFlags BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

	/// <summary>
	/// 工厂方法特性名称的前缀：约定方法名需在去除该前缀后展开，使 <c>Update</c> 与 <c>FactoryUpdate</c> 两种写法都成立。
	/// </summary>
	private const string FactoryPrefix = "Factory";

	private static readonly string[] _collectionTypesName =
	[
		typeof(IList<>).FullName,
		typeof(ICollection<>).FullName,
		typeof(IEnumerable<>).FullName
	];

	private static readonly ConcurrentDictionary<Type, List<Tuple<PropertyInfo, Type, bool, object>>> _propertyCache = new();
	private static readonly ConcurrentDictionary<string, MethodInfo> _factoryMethods = new();
	private static readonly ConcurrentDictionary<Type, string[]> _conventionalMethodNames = new();

	internal static List<Tuple<PropertyInfo, Type, bool, object>> GetAutoInjectProperties(Type objectType)
	{
		return _propertyCache.GetOrAdd(objectType, type =>
		{
			var autoInjectProperties = new List<Tuple<PropertyInfo, Type, bool, object>>();
			var propertiesOfType = type.GetRuntimeProperties().ToList();

			foreach (var property in propertiesOfType)
			{
				var attribute = property.GetCustomAttribute<InjectAttribute>();

				if (attribute == null)
				{
					continue;
				}

				var (propertyType, multiple) = FindServiceType(property.Name, property.PropertyType);

				autoInjectProperties.Add(Tuple.Create(property, propertyType, multiple, attribute.ServiceKey));
			}

			return autoInjectProperties;
		});
	}

	internal static MethodInfo FindFactoryMethod<TTarget, TAttribute>(object[] criteria)
		where TAttribute : FactoryMethodAttribute
	{
		return FindFactoryMethod<TTarget>(typeof(TAttribute), criteria);
	}

	internal static MethodInfo FindFactoryMethod<TTarget>(Type attributeType, object[] criteria)
	{
		var name = GetMethodName<TTarget>(attributeType, criteria);
		return _factoryMethods.GetOrAdd(name, () => FindMatchedMethod<TTarget>(attributeType, criteria));
	}

	private static MethodInfo FindMatchedMethod<TTarget>(Type attributeType, object[] criteria)
	{
		var candidates = GetCandidateMethods(typeof(TTarget), attributeType);
		if (candidates == null || candidates.Count == 0)
		{
			throw new MissingMethodException(typeof(TTarget).FullName, GetConventionalMethodNames(attributeType).JoinAsString("/"));
		}

		var matches = new List<Tuple<MethodInfo, int>>();

		int parameterCount;
		if (criteria != null)
		{
			parameterCount = criteria.GetType() == typeof(object[]) ? criteria.GetLength(0) : 1;
		}
		else
		{
			parameterCount = 1;
		}

		if (parameterCount > 0)
		{
			foreach (var candidate in candidates)
			{
				var methodParameters = candidate.Item1.GetParameters();

				// 目标方法的参数数量可以多于传入条件，多出的参数必须带默认值（可选参数）
				var optionalCount = methodParameters.Length - parameterCount;
				if (optionalCount < 0 || (optionalCount > 0 && methodParameters.Skip(parameterCount).Any(t => !t.HasDefaultValue)))
				{
					continue;
				}

				// 可选参数匹配降低优先级，使长度精确匹配的重载优先
				var score = -Math.Max(optionalCount, 0);
				var index = 0;

				if (criteria!.GetType() == typeof(object[]))
				{
					foreach (var c in criteria)
					{
						var currentScore = CalculateParameterMatchScore(methodParameters[index], c);
						if (currentScore == 0)
						{
							break;
						}

						score += currentScore;
						index++;
					}
				}
				else
				{
					var currentScore = CalculateParameterMatchScore(methodParameters[index], criteria);
					if (currentScore != 0)
					{
						score += currentScore;
						index++;
					}
				}

				if (index == parameterCount)
				{
					matches.Add(Tuple.Create(candidate.Item1, score + candidate.Item2));
				}
			}
		}
		else
		{
			foreach (var (method, score) in candidates)
			{
				var methodParameters = method.GetParameters();
				if (methodParameters.Length == 0 || methodParameters.All(t => t.HasDefaultValue))
				{
					// 无参数方法优先于全为可选参数的方法
					matches.Add(Tuple.Create(method, score - methodParameters.Length));
				}
			}
		}

		if (matches.Count == 0)
		{
			// 查找 params 数组
			foreach (var (method, score) in candidates)
			{
				var lastParam = method.GetParameters().LastOrDefault();
				if (lastParam != null && lastParam.ParameterType == typeof(object[]) &&
				    lastParam.GetCustomAttributes<ParamArrayAttribute>().Any())
				{
					matches.Add(Tuple.Create(method, 1 + score));
				}
			}
		}

		if (matches.Count == 0)
		{
			var methodNames = GetConventionalMethodNames(attributeType).JoinAsString("/");
			var paramsNames = GetParameterTypeNames(criteria);
			throw new MissingMethodException(typeof(TTarget).FullName, $"{methodNames}({paramsNames})");
		}

		var matchedMethod = matches[0];
		if (matches.Count > 1)
		{
			var maxScore = int.MinValue;
			var maxCount = 0;
			foreach (var item in matches)
			{
				if (item.Item2 > maxScore)
				{
					maxScore = item.Item2;
					maxCount = 1;
					matchedMethod = item;
				}
				else if (item.Item2 == maxScore)
				{
					maxCount++;
				}
			}

			if (maxCount > 1)
			{
				throw new AmbiguousMatchException(Resources.IDS_MULTIPLE_METHOD_MATCHED);
			}
		}

		return matchedMethod.Item1;
	}

	/// <summary>
	/// 查找匹配的方法。
	/// </summary>
	/// <param name="attributeType">方法应具有的特性类型。</param>
	/// <param name="parameterTypes">方法的参数类型列表。</param>
	/// <typeparam name="TTarget">目标类型。</typeparam>
	/// <returns>匹配的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="MissingMethodException">当未找到匹配的方法时抛出。</exception>
	/// <exception cref="AmbiguousMatchException">当找到多个匹配的方法时抛出。</exception>
	public static MethodInfo FindMatchedMethod<TTarget>(Type attributeType, IReadOnlyList<Type> parameterTypes)
	{
		var methods = typeof(TTarget).GetRuntimeMethods()
		                             .Where(t => t.GetCustomAttribute(attributeType) != null)
		                             .ToList();
		if (methods is not { Count: > 0 })
		{
			throw new MissingMethodException($"Missing method with attribute '{attributeType.Name}' on {typeof(TTarget).FullName}");
		}

		var matches = new List<MethodInfo>();

		foreach (var method in methods)
		{
			var parameters = method.GetParameters();

			if (parameters.Length == 0 && parameterTypes.Count == 0)
			{
				matches.Add(method);
				continue;
			}

			if (parameters.Length != parameterTypes.Count)
			{
				continue;
			}

			var isAllParameterMatched = Enumerable.Range(0, parameters.Length).Select(index => parameters[index].ParameterType == parameterTypes[index]).All(t => t);
			if (isAllParameterMatched)
			{
				matches.Add(method);
			}
		}

		return matches.Count switch
		{
			0 => throw new MissingMethodException("Missing method matched the specified arguments."),
			1 => matches[0],
			_ => throw new AmbiguousMatchException("Multiple methods matched.")
		};
	}

	private static List<Tuple<MethodInfo, int>> GetCandidateMethods(Type targetType, Type attributeType, int level = 0)
	{
		var validNames = GetConventionalMethodNames(attributeType);

		var result = new List<Tuple<MethodInfo, int>>();
		var methods = targetType.GetMethods(BINDING_FLAGS)
		                        .Where(t => t.GetCustomAttribute(attributeType) != null || validNames.Contains(t.Name));

		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var method in methods)
		{
			result.Add(Tuple.Create(method, level));
		}

		if (result.Count == 0 && targetType.BaseType != null && targetType.BaseType != typeof(object) && !targetType.BaseType.IsInterface)
		{
			level--;
			result.AddRange(GetCandidateMethods(targetType.BaseType, attributeType, level));
		}


		return result;
	}

	/// <summary>
	/// 为属性类型查找注入的服务类型。
	/// </summary>
	/// <param name="name">属性名称。</param>
	/// <param name="type">属性类型。</param>
	/// <param name="multiple">服务是否有多个实现。</param>
	/// <returns>包含服务类型和是否为多实现的元组。</returns>
	/// <exception cref="NotSupportedException">当属性类型不受支持时抛出。</exception>
	/// <exception cref="InvalidOperationException">当属性类型无效时抛出。</exception>
	private static Tuple<Type, bool> FindServiceType(string name, Type type, bool? multiple = null)
	{
		while (true)
		{
			if (type.IsPrimitive)
			{
				throw new NotSupportedException("Can not inject primitive type property.");
			}

			if (!type.IsClass && !type.IsInterface)
			{
				throw new NotSupportedException($"Can not inject property '{name}', the property type {type.FullName} is not supported.");
			}

			if (type == typeof(object))
			{
				throw new NotSupportedException($"Can not inject property '{name}', the property type {type.FullName} is not supported.");
			}

			var @interface = type.GetInterface(nameof(IEnumerable));
			if (@interface == null)
			{
				return Tuple.Create(type, multiple ?? false);
			}

			if (multiple == true)
			{
				throw new NotSupportedException();
			}

			if (type.IsArray)
			{
				var interfaces = type.FindInterfaces(HandlerInterfaceFilter, null);
				if (interfaces == null || interfaces.Length == 0)
				{
					throw new InvalidOperationException();
				}

				type = interfaces[0].GenericTypeArguments[0];
				multiple = true;
				continue;
			}

			if (type.IsGenericType)
			{
				var propertyTypeFullname = $"{type.Namespace}.{type.Name}";
				if (propertyTypeFullname == typeof(IEnumerable<>).FullName)
				{
					if (type.GenericTypeArguments.Length != 1)
					{
						throw new InvalidOperationException("");
					}

					var genericArgumentType = type.GenericTypeArguments[0];

					type = genericArgumentType;
					multiple = true;
					continue;
				}
			}


			throw new NotSupportedException($"Can not inject property '{name}', the property type {type.FullName} is not supported.");
		}
	}

	private static bool HandlerInterfaceFilter(Type type, object criteria)
	{
		var typeName = $"{type.Namespace}.{type.Name}";
		return _collectionTypesName.Contains(typeName);
	}

	private static string GetMethodName<TTarget>(Type attributeType, object[] criteria)
	{
		var name = $"{typeof(TTarget).FullName}.{attributeType.Name.Replace(nameof(Attribute), string.Empty)}({GetParameterTypeNames(criteria)})";
		return name;
	}

	/// <summary>
	/// 获取条件数组的参数类型名称列表。
	/// </summary>
	/// <param name="criteria">条件数组。</param>
	/// <returns>以逗号连接的类型名称字符串。</returns>
	private static string GetParameterTypeNames(object[] criteria)
	{
		if (criteria == null)
		{
			return string.Empty;
		}

		var parameterTypeNames = new List<string>();
		if (criteria.GetType() == typeof(object[]))
		{
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var item in criteria)
			{
				parameterTypeNames.Add(item == null ? "null" : GetTypeName(item.GetType()));
			}
		}
		else
		{
			parameterTypeNames.Add(GetTypeName(criteria.GetType()));
		}

		return parameterTypeNames.Join(", ");
	}

	private static string GetTypeName(Type type)
	{
		if (type.IsArray)
		{
			return $"{GetTypeName(type.GetElementType())}[]";
		}

		if (!type.IsGenericType)
		{
			return type.FullName ?? type.Name;
		}

		var result = new StringBuilder();
		var genericArguments = type.GetGenericArguments();
		result.Append(type.GetGenericTypeDefinition().FullName);
		result.Append('<');

		for (var index = 0; index < genericArguments.Length; index++)
		{
			if (index > 0)
			{
				result.Append(',');
			}

			result.Append(GetTypeName(genericArguments[index]));
		}

		result.Append('>');

		return result.ToString();
	}

	/// <summary>
	/// 计算参数匹配分数。
	/// </summary>
	private static int CalculateParameterMatchScore(ParameterInfo parameter, object criteria)
	{
		if (criteria == null)
		{
			if (parameter.ParameterType.IsPrimitive)
			{
				return 0;
			}

			if (parameter.ParameterType == typeof(object))
			{
				return 2;
			}

			if (parameter.ParameterType == typeof(object[]))
			{
				return 2;
			}

			if (parameter.ParameterType.IsClass)
			{
				return 1;
			}

			if (parameter.ParameterType.IsArray)
			{
				return 1;
			}

			if (parameter.ParameterType.IsInterface)
			{
				return 1;
			}

			if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
			{
				return 2;
			}
		}
		else
		{
			if (criteria.GetType() == parameter.ParameterType)
			{
				return 3;
			}

			if (parameter.ParameterType == typeof(object))
			{
				return 1;
			}

			if (parameter.ParameterType.IsInstanceOfType(criteria))
			{
				return 2;
			}
		}

		return 0;
	}

	/// <summary>
	/// 获取约定的方法名称。
	/// </summary>
	/// <param name="attributeType">特性类型。</param>
	/// <returns>约定的方法名称数组。</returns>
	/// <remarks>
	/// 特性名本身带有 <c>Factory</c> 前缀（如 <see cref="FactoryUpdateAttribute"/>），
	/// 需先去前缀再展开，否则会得到 <c>FactoryFactoryUpdate</c> 这类永不匹配的名称，
	/// 使 <c>Update</c> / <c>UpdateAsync</c> 这一半约定形同虚设。
	/// 对 <see cref="FactoryUpdateAttribute"/> 返回
	/// <c>Update</c>、<c>UpdateAsync</c>、<c>FactoryUpdate</c>、<c>FactoryUpdateAsync</c>。
	/// </remarks>
	internal static string[] GetConventionalMethodNames(Type attributeType)
	{
		return _conventionalMethodNames.GetOrAdd(attributeType, static type =>
		{
			// FactoryUpdateAttribute -> FactoryUpdate
			var name = type.Name.Replace(nameof(Attribute), string.Empty);

			// FactoryUpdate -> Update（前缀后为空时保持不变，避免生成空前缀名称）
			var operation = name.StartsWith(FactoryPrefix, StringComparison.Ordinal) && name.Length > FactoryPrefix.Length
				? name[FactoryPrefix.Length..]
				: name;

			return (string[])[operation, $"{operation}Async", name, $"{name}Async"];
		});
	}

	/// <summary>
	/// 判断方法是否为指定操作对应的工厂方法：标记了对应的工厂方法特性，或符合约定的方法名。
	/// </summary>
	/// <param name="method">待判断的方法。</param>
	/// <param name="attributeType">工厂方法特性类型。</param>
	/// <returns>是工厂方法则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// <para>
	/// 判定规则：标记了工厂方法特性的方法不限定名称；未标记的方法必须严格匹配约定名称
	/// （<see cref="GetConventionalMethodNames"/>，大小写敏感），拼写不符即不予识别。
	/// </para>
	/// <para>
	/// 该方法与工厂方法查找（<see cref="FindFactoryMethod{TTarget}(Type, object[])"/>）使用同一套判定规则，
	/// 确保"能被工厂调用的方法"与"参与权限要求收集的方法"始终一致。
	/// </para>
	/// </remarks>
	internal static bool IsFactoryMethod(MethodInfo method, Type attributeType)
	{
		return method.IsDefined(attributeType, true)
		       || GetConventionalMethodNames(attributeType).Contains(method.Name);
	}
}