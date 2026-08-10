using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象反射器。
/// </summary>
public class ObjectReflector
{
	private const BindingFlags BINDING_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

	private static readonly string[] _collectionTypesName =
	[
		typeof(IList<>).FullName,
		typeof(ICollection<>).FullName,
		typeof(IEnumerable<>).FullName
	];

	private static readonly ConcurrentDictionary<Type, List<Tuple<PropertyInfo, Type, bool, object>>> _propertyCache = new();
	private static readonly ConcurrentDictionary<string, MethodInfo> _factoryMethods = new();

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
				var score = 0;
				var methodParameters = candidate.Item1.GetParameters();
				if (methodParameters.Length != parameterCount)
				{
					continue;
				}

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
				if (method.GetParameters().Length == 0)
				{
					matches.Add(Tuple.Create(method, score));
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

		{
			var method = matches[0];
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
						method = item;
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

			return method.Item1;
		}
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

		{
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

			{
				// 防止代码检查警告。
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
			return type.Name;
		}

		var result = new StringBuilder();
		var genericArguments = type.GetGenericArguments();
		result.Append(type.Name);
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
	private static string[] GetConventionalMethodNames(Type attributeType)
	{
		var validNames = new[]
		{
			$"Factory{attributeType.Name.Replace(nameof(Attribute), string.Empty)}",
			$"Factory{attributeType.Name.Replace(nameof(Attribute), string.Empty)}Async",
			$"{attributeType.Name.Replace(nameof(Attribute), string.Empty)}",
			$"{attributeType.Name.Replace(nameof(Attribute), string.Empty)}Async"
		};
		return validNames;
	}
}