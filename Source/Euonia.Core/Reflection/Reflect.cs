using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供强类型的反射操作方法。
/// </summary>
public static class Reflect
{
	/// <summary>
	/// 从属性表达式中提取属性信息。
	/// </summary>
	/// <typeparam name="T">包含表达式中所指定属性的对象类型。</typeparam>
	/// <param name="expression">属性表达式（例如 p => p.PropertyName）。</param>
	/// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="expression"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentException">当表达式不是 <see cref="MemberExpression"/>、或不表示属性、或属性为静态时抛出。</exception>
	public static PropertyInfo GetProperty<T>(Expression<Func<T>> expression)
	{
		ArgumentAssert.ThrowIfNull(expression, nameof(expression));

		if (expression.Body is not MemberExpression memberExpression)
		{
			throw new ArgumentException(Resources.IDS_EXPRESSION_IS_NOT_A_MEMBER_ACCESS_EXPRESSION, nameof(expression));
		}

		var property = memberExpression.Member as PropertyInfo;
		if (property == null)
		{
			throw new ArgumentException(Resources.IDS_MEMBER_ACCESS_EXPRESSION_DOES_NOT_ACCESS_A_PROPERTY, nameof(expression));
		}

		return property;
	}

	/// <summary>
	/// 从属性表达式中提取属性信息。
	/// </summary>
	/// <typeparam name="T">包含表达式中所指定属性的对象类型。</typeparam>
	/// <param name="expression">属性表达式（例如 p => p.PropertyName）。</param>
	/// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
	public static PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression)
	{
		return GetProperty<T, object>(expression);
	}

	/// <summary>
	/// 从属性表达式中提取属性信息。
	/// </summary>
	/// <typeparam name="T">包含表达式中所指定属性的对象类型。</typeparam>
	/// <typeparam name="TResult">属性的返回类型。</typeparam>
	/// <param name="expression">属性表达式（例如 p => p.PropertyName）。</param>
	/// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="expression"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentException">当表达式不引用属性时抛出。</exception>
	public static PropertyInfo GetProperty<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		ArgumentAssert.ThrowIfNull(expression, nameof(expression));

		PropertyInfo result;

		if (expression.Body.NodeType == ExpressionType.Convert)
		{
			result = ((MemberExpression)((UnaryExpression)expression.Body).Operand).Member as PropertyInfo;
		}
		else
		{
			result = ((MemberExpression)expression.Body).Member as PropertyInfo;
		}

		if (result != null)
		{
			return result;
		}

		throw new ArgumentException($"Expression '{expression}' does not refer to a property.");
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <typeparam name="T">包含方法的对象类型。</typeparam>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	public static MethodInfo GetMethodInfo<T>(Expression<Func<T, Delegate>> expression)
	{
		return GetMethodInfo((LambdaExpression)expression);
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="expression"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentException">当表达式不是 Lambda 表达式或不表示方法调用时抛出。</exception>
	/// <exception cref="NullReferenceException">当方法调用对象为 null 时抛出。</exception>
	public static MethodInfo GetMethodInfo(Expression expression)
	{
		if (expression == null)
		{
			throw new ArgumentNullException(nameof(expression));
		}

		if (expression is not LambdaExpression lambda)
		{
			throw new ArgumentException(Resources.IDS_NOT_A_LAMBDA_EXPRESSION, nameof(expression));
		}

		// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
		switch (lambda.Body.NodeType)
		{
			case ExpressionType.Convert:
			{
				var unaryExpression = (UnaryExpression)lambda.Body;
				var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
				var methodCallObject = (ConstantExpression)methodCallExpression.Object;
				if (methodCallObject == null)
				{
					throw new NullReferenceException(nameof(methodCallObject));
				}

				var methodInfo = (MethodInfo)methodCallObject.Value;
				return methodInfo;
			}
			case ExpressionType.Call:
				return ((MethodCallExpression)lambda.Body).Method;
			default:
				throw new ArgumentException(Resources.IDS_NOT_A_METHOD_CALL, nameof(expression));
		}
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的成员信息。
	/// </summary>
	/// <param name="expression">成员表达式。</param>
	/// <returns>成员的 <see cref="MemberInfo"/>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="expression"/> 为 null 时抛出。</exception>
	/// <exception cref="ArgumentException">当表达式不是 Lambda 表达式或不表示成员访问时抛出。</exception>
	public static MemberInfo GetMemberInfo(Expression expression)
	{
		if (expression == null)
		{
			throw new ArgumentNullException(nameof(expression));
		}

		if (expression is not LambdaExpression lambda)
		{
			throw new ArgumentException(Resources.IDS_NOT_A_LAMBDA_EXPRESSION, nameof(expression));
		}

		var memberExpr = lambda.Body.NodeType switch
		{
			// Func<TTarget, object> 返回 object 类型，因此第一条语句可能是
			// 强制转换（如果字段/属性不返回 object）或直接的成员访问。
			ExpressionType.Convert => ((UnaryExpression)lambda.Body).Operand as MemberExpression,
			ExpressionType.MemberAccess => lambda.Body as MemberExpression,
			_ => null
		};

		if (memberExpr == null)
		{
			throw new ArgumentException(Resources.IDS_NOT_A_MEMBER_ACCESS, nameof(expression));
		}

		return memberExpr.Member;
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <typeparam name="T">对象类型。</typeparam>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="item">目标对象。</param>
	/// <param name="value">要设置的值。</param>
	/// <param name="property">属性表达式。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 null 时抛出。</exception>
	public static void SetValue<T, TValue>(T item, TValue value, Expression<Func<T, TValue>> property)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		var propertyInfo = GetProperty(property);

		propertyInfo.SetValue(item, value);
	}

	/// <summary>
	/// 获取属性值。
	/// </summary>
	/// <typeparam name="T">对象类型。</typeparam>
	/// <param name="item">目标对象。</param>
	/// <param name="property">属性表达式。</param>
	/// <returns>属性值。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 null 时抛出。</exception>
	public static object GetValue<T>(T item, Expression<Func<T, object>> property)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		var propertyInfo = GetProperty(property);

		return propertyInfo.GetValue(item);
	}

	/// <summary>
	/// 检查 <paramref name="givenType"/> 是否实现/继承 <paramref name="genericType"/>。
	/// </summary>
	/// <param name="givenType">要检查的类型。</param>
	/// <param name="genericType">泛型类型。</param>
	/// <returns>如果 <paramref name="givenType"/> 实现或继承了 <paramref name="genericType"/>，则为 true；否则为 false。</returns>
	public static bool IsAssignableToGenericType(Type givenType, Type genericType)
	{
		var givenTypeInfo = givenType.GetTypeInfo();

		if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
		{
			return true;
		}

		foreach (var interfaceType in givenTypeInfo.GetInterfaces())
		{
			if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
			{
				return true;
			}
		}

		return givenTypeInfo.BaseType != null && IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
	}

	/// <summary>
	/// 获取类型实现的泛型类型列表。
	/// </summary>
	/// <param name="givenType">要检查的类型。</param>
	/// <param name="genericType">目标泛型类型。</param>
	/// <returns>实现的泛型类型列表。</returns>
	public static List<Type> GetImplementedGenericTypes(Type givenType, Type genericType)
	{
		var result = new List<Type>();
		AddImplementedGenericTypes(result, givenType, genericType);
		return result;
	}

	private static void AddImplementedGenericTypes(ICollection<Type> result, Type givenType, Type genericType)
	{
		var givenTypeInfo = givenType.GetTypeInfo();

		if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
		{
			result.AddIfNotContains(givenType);
		}

		foreach (var interfaceType in givenTypeInfo.GetInterfaces())
		{
			if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
			{
				result.AddIfNotContains(interfaceType);
			}
		}

		if (givenTypeInfo.BaseType == null)
		{
			return;
		}

		AddImplementedGenericTypes(result, givenTypeInfo.BaseType, genericType);
	}

	/// <summary>
	/// 尝试获取类成员及其声明类型上定义的特性（包括继承的特性）。如果未声明则返回默认值。
	/// </summary>
	/// <typeparam name="TAttribute">特性类型。</typeparam>
	/// <param name="memberInfo">成员信息。</param>
	/// <param name="defaultValue">默认值（默认为 null）。</param>
	/// <param name="inherit">是否从基类继承特性。</param>
	/// <returns>找到的特性实例，如果未找到则返回 <paramref name="defaultValue"/>。</returns>
	public static TAttribute GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
		where TAttribute : Attribute
	{
		if (memberInfo.IsDefined(typeof(TAttribute), inherit))
		{
			return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
		}

		return defaultValue;
	}

	/// <summary>
	/// 尝试获取类成员及其声明类型上定义的特性（包括继承的特性）。如果未声明则返回默认值。
	/// </summary>
	/// <typeparam name="TAttribute">特性类型，必须是引用类型。</typeparam>
	/// <param name="memberInfo">成员信息。</param>
	/// <param name="defaultValue">默认值（默认为 null）。</param>
	/// <param name="inherit">是否从基类继承特性。</param>
	/// <returns>找到的特性实例，如果未找到则返回 <paramref name="defaultValue"/>。</returns>
	public static TAttribute GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default, bool inherit = true)
		where TAttribute : class
	{
		return memberInfo.GetCustomAttributes(inherit).OfType<TAttribute>().FirstOrDefault()
		       ?? memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
		       ?? defaultValue;
	}

	/// <summary>
	/// 尝试获取类成员及其声明类型上定义的所有特性（包括继承的特性）。
	/// </summary>
	/// <typeparam name="TAttribute">特性类型，必须是引用类型。</typeparam>
	/// <param name="memberInfo">成员信息。</param>
	/// <param name="inherit">是否从基类继承特性。</param>
	/// <returns>找到的特性集合。</returns>
	public static IEnumerable<TAttribute> GetAttributesOfMemberOrDeclaringType<TAttribute>(MemberInfo memberInfo, bool inherit = true)
		where TAttribute : class
	{
		var customAttributes = memberInfo.GetCustomAttributes(inherit).OfType<TAttribute>();
		var declaringTypeCustomAttributes =
			memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>();
		return declaringTypeCustomAttributes != null
			? customAttributes.Concat(declaringTypeCustomAttributes).Distinct()
			: customAttributes;
	}

	/// <summary>
	/// 通过完整属性路径从给定对象获取属性值。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="objectType">对象类型。</param>
	/// <param name="propertyPath">属性路径（以 '.' 分隔）。</param>
	/// <returns>属性值，如果路径中某个属性不存在则返回 null。</returns>
	public static object GetValue(object obj, Type objectType, string propertyPath)
	{
		var value = obj;
		var currentType = objectType;
		var objectPath = currentType.FullName;
		var absolutePropertyPath = propertyPath;
		if (objectPath != null && absolutePropertyPath.StartsWith(objectPath))
		{
			absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
		}

		foreach (var propertyName in absolutePropertyPath.Split('.'))
		{
			var property = currentType.GetProperty(propertyName);
			if (property != null)
			{
				if (value != null)
				{
					value = property.GetValue(value, null);
				}

				currentType = property.PropertyType;
			}
			else
			{
				value = null;
				break;
			}
		}

		return value;
	}

	/// <summary>
	/// 通过完整属性路径在给定对象上设置属性值。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="objectType">对象类型。</param>
	/// <param name="propertyPath">属性路径（以 '.' 分隔）。</param>
	/// <param name="value">要设置的值。</param>
	/// <exception cref="MissingMemberException">当路径中的某个属性不存在时抛出。</exception>
	public static void SetValue(object obj, Type objectType, string propertyPath, object value)
	{
		var currentType = objectType;
		PropertyInfo property;
		var objectPath = currentType.FullName;
		var absolutePropertyPath = propertyPath;
		if (absolutePropertyPath.StartsWith(objectPath!))
		{
			absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
		}

		var properties = absolutePropertyPath.Split('.');

		if (properties.Length == 1)
		{
			property = objectType.GetProperty(properties.First());
			if (property == null)
			{
				throw new MissingMemberException($"Property {properties.First()} not found on type {objectType.FullName}.");
			}

			property.SetValue(obj, value);
			return;
		}

		for (var i = 0; i < properties.Length - 1; i++)
		{
			property = currentType.GetProperty(properties[i]);
			if (property == null)
			{
				throw new MissingMemberException($"Property {properties[i]} not found on type {currentType.FullName}.");
			}

			obj = property.GetValue(obj, null);
			currentType = property.PropertyType;
		}

		property = currentType.GetProperty(properties.Last());
		if (property == null)
		{
			throw new MissingMemberException($"Property {properties.Last()} not found on type {currentType.FullName}.");
		}

		property.SetValue(obj, value);
	}

	/// <summary>
	/// 递归获取指定类型（包括其基类）中所有公共常量的值。
	/// </summary>
	/// <param name="type">要获取常量的类型。</param>
	/// <returns>常量值的字符串集合。</returns>
	public static IEnumerable<string> GetPublicConstantsRecursively(Type type)
	{
		const int maxRecursiveParameterValidationDepth = 8;

		var publicConstants = new List<string>();

		static void Recursively(List<string> constants, Type targetType, int currentDepth)
		{
			if (currentDepth > maxRecursiveParameterValidationDepth)
			{
				return;
			}

			constants.AddRange(targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			                             .Where(x => x.IsLiteral && !x.IsInitOnly)
			                             .Select(x => x.GetValue(null)?.ToString()));

			var nestedTypes = targetType.GetNestedTypes(BindingFlags.Public);

			foreach (var nestedType in nestedTypes)
			{
				Recursively(constants, nestedType, currentDepth + 1);
			}
		}

		Recursively(publicConstants, type, 1);

		return publicConstants.ToArray();
	}

	/// <summary>
	/// 调用泛型方法。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="methodName">方法名称。</param>
	/// <param name="genericTypes">泛型类型参数数组。</param>
	/// <param name="parameters">方法参数。</param>
	/// <returns>方法调用的返回值。</returns>
	/// <exception cref="ArgumentNullException">当找不到指定名称的方法时抛出。</exception>
	public static object InvokeGenericMethod(object obj, string methodName, Type[] genericTypes, params object[] parameters)
	{
		var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
		if (method == null)
		{
			throw new ArgumentNullException($"Method {methodName} not found");
		}

		var genericMethod = method.MakeGenericMethod(genericTypes);
		return genericMethod.Invoke(method.IsStatic ? null : obj, parameters);
	}

	/// <summary>
	/// 尝试获取对象的属性值。
	/// </summary>
	/// <typeparam name="T">属性值类型。</typeparam>
	/// <param name="obj">目标对象。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="value">输出属性值。</param>
	/// <returns>如果属性存在且类型匹配，则为 true；否则为 false。</returns>
	public static bool TryGetPropertyValue<T>(object obj, string propertyName, out T value)
	{
		value = default!;
		var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		if (property == null || property.PropertyType != typeof(T))
		{
			return false;
		}

		value = (T)property.GetValue(obj);
		return true;
	}

	/// <summary>
	/// 尝试获取对象的属性值。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="value">输出属性值。</param>
	/// <returns>如果属性存在，则为 true；否则为 false。</returns>
	public static bool TryGetPropertyValue(object obj, string propertyName, out object value)
	{
		value = null!;
		var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		if (property == null)
		{
			return false;
		}

		value = property.GetValue(obj);
		return true;
	}

	#region Public Static Methods

	/// <summary>
	/// 尝试查找要调用的重载方法。如果未找到则返回 null。此重载根据传入的参数类型与类型上的方法参数进行匹配。
	/// </summary>
	/// <param name="methodNameToRetrieve">要查找的方法名称。</param>
	/// <param name="typeToLookThroughTheMethods">要检索方法的类型，以便遍历并尝试找到正确的方法。</param>
	/// <param name="methodParameterTypes">要匹配的方法参数类型数组。</param>
	/// <returns>找到的 <see cref="MethodInfo"/>，如果未找到则返回 null。</returns>
	public static MethodInfo FindMethod(string methodNameToRetrieve, Type typeToLookThroughTheMethods, params Type[] methodParameterTypes)
	{
		return FindMethod(methodNameToRetrieve, typeToLookThroughTheMethods, x => MethodParameterSelector(x, methodParameterTypes));
	}

	/// <summary>
	/// 尝试查找要调用的重载方法。如果未找到则返回 null。此方法将对每个方法评估 MethodSelector 并检查是否返回 true。
	/// </summary>
	/// <param name="methodNameToRetrieve">要查找的方法名称。</param>
	/// <param name="typeToLookThroughTheMethods">要检索方法的类型，以便遍历并尝试找到正确的方法。</param>
	/// <param name="methodSelector">允许调用方查看参数并选择正确方法的委托。</param>
	/// <returns>找到的 <see cref="MethodInfo"/>，如果未找到则返回 null。</returns>
	public static MethodInfo FindMethod(string methodNameToRetrieve, Type typeToLookThroughTheMethods, Func<MethodInfo, bool> methodSelector)
	{
		return FindMethod(methodNameToRetrieve, typeToLookThroughTheMethods.GetMethods(), methodSelector);
	}

	/// <summary>
	/// 尝试查找要调用的重载方法。如果未找到则返回 null。如果你已有相同名称的方法信息集合，请调用此方法。
	/// </summary>
	/// <param name="methodNameToRetrieve">要查找的方法名称。</param>
	/// <param name="methodsToLookThrough">名称相同的方法集合，用于遍历并根据方法选择器进行检查。</param>
	/// <param name="methodSelector">允许调用方查看参数并选择正确方法的委托。</param>
	/// <returns>找到的 <see cref="MethodInfo"/>，如果未找到则返回 null。</returns>
	public static MethodInfo FindMethod(string methodNameToRetrieve, IEnumerable<MethodInfo> methodsToLookThrough, Func<MethodInfo, bool> methodSelector)
	{
		return methodsToLookThrough.FirstOrDefault(methodToInspect => string.Equals(methodNameToRetrieve, methodToInspect.Name, StringComparison.OrdinalIgnoreCase) && methodSelector(methodToInspect));
	}

	#endregion

	#region Private Static Methods

	/// <summary>
	/// 私有辅助方法，用于检查当前方法并检查其方法参数类型。如果匹配则返回 true，否则返回 false。
	/// </summary>
	/// <param name="methodToEvaluate">要评估并检查参数类型是否匹配的方法。</param>
	/// <param name="methodParameterTypes">要匹配的方法参数类型数组。</param>
	/// <returns>方法参数类型是否完全匹配。</returns>
	private static bool MethodParameterSelector(MethodBase methodToEvaluate, params Type[] methodParameterTypes)
	{
		//we are going to match the GetParameters and the MethodParameterTypes. It needs to match index for index and type for type. So GetParameters[0].Type must match MethodParameterTypes[0].Type...[1].Type must match [1].Type

		//holds the index with the method parameter types we are up too
		int i = 0;

		//let's loop through the parameters
		foreach (ParameterInfo thisParameter in methodToEvaluate.GetParameters())
		{
			//it's a generic parameter...ie...TSource then we are going to ignore it because whatever we pass in would be TSource
			if (!thisParameter.ParameterType.IsGenericParameter)
			{
				//is this a generic type? we need to compare this differently
				if (thisParameter.ParameterType.IsGenericType)
				{
					//is the method parameter a generic type?
					if (!methodParameterTypes[i].IsGenericType)
					{
						//it isn't so return false..cause they aren't the same
						return false;
					}

					//if the generic type's don't match then return false...This might be problematic...it works for the scenario which I'm using it for so we will leave this and modify afterwards
					if (thisParameter.ParameterType.GetGenericTypeDefinition() != methodParameterTypes[i].GetGenericTypeDefinition())
					{
						//doesn't match return false
						return false;
					}
				}
				else if (thisParameter.ParameterType != methodParameterTypes[i].UnderlyingSystemType)
				{
					//this is a regular parameter so we can compare it normally
					//we don't have a match...so return false
					return false;
				}
			}

			//increment the index
			i++;
		}

		//if we get here then everything matches so return true
		return true;
	}

	#endregion
}

/// <summary>
/// 提供对 <typeparamref name="TTarget"/> 类型的强类型反射操作。
/// </summary>
/// <typeparam name="TTarget">要反射的类型。</typeparam>
public static class Reflect<TTarget>
{
	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示方法调用。</exception>
	public static MethodInfo GetMethod(Expression<Action<TTarget>> expression)
	{
		return Reflect.GetMethodInfo(expression);
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <typeparam name="T1">方法的第一个参数类型。</typeparam>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示方法调用。</exception>
	public static MethodInfo GetMethod<T1>(Expression<Action<TTarget, T1>> expression)
	{
		return Reflect.GetMethodInfo(expression);
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <typeparam name="T1">方法的第一个参数类型。</typeparam>
	/// <typeparam name="T2">方法的第二个参数类型。</typeparam>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示方法调用。</exception>
	public static MethodInfo GetMethod<T1, T2>(Expression<Action<TTarget, T1, T2>> expression)
	{
		return Reflect.GetMethodInfo(expression);
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的方法信息。
	/// </summary>
	/// <typeparam name="T1">方法的第一个参数类型。</typeparam>
	/// <typeparam name="T2">方法的第二个参数类型。</typeparam>
	/// <typeparam name="T3">方法的第三个参数类型。</typeparam>
	/// <param name="expression">方法表达式。</param>
	/// <returns>方法的 <see cref="MethodInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示方法调用。</exception>
	public static MethodInfo GetMethod<T1, T2, T3>(Expression<Action<TTarget, T1, T2, T3>> expression)
	{
		return Reflect.GetMethodInfo(expression);
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的属性信息。
	/// </summary>
	/// <param name="expression">属性表达式。</param>
	/// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示属性访问。</exception>
	public static PropertyInfo GetProperty(Expression<Func<TTarget, object>> expression)
	{
		var info = Reflect.GetMemberInfo(expression) as PropertyInfo;
		if (info == null)
			throw new ArgumentException("Member is not a property");

		return info;
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的属性信息。
	/// </summary>
	/// <typeparam name="TValue">属性类型。</typeparam>
	/// <param name="expression">属性表达式。</param>
	/// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示属性访问。</exception>
	public static PropertyInfo GetProperty<TValue>(Expression<Func<TTarget, TValue>> expression)
	{
		var info = Reflect.GetMemberInfo(expression) as PropertyInfo;
		if (info == null)
		{
			throw new ArgumentException("Member is not a property");
		}

		return info;
	}

	/// <summary>
	/// 获取 Lambda 表达式表示的字段信息。
	/// </summary>
	/// <param name="expression">字段表达式。</param>
	/// <returns>字段的 <see cref="FieldInfo"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="expression"/> 为 null。</exception>
	/// <exception cref="ArgumentException"><paramref name="expression"/> 不是 Lambda 表达式或不表示字段访问。</exception>
	public static FieldInfo GetField(Expression<Func<TTarget, object>> expression)
	{
		var info = Reflect.GetMemberInfo(expression) as FieldInfo;
		if (info == null)
		{
			throw new ArgumentException("Member is not a field");
		}

		return info;
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="item">目标对象。</param>
	/// <param name="value">要设置的值。</param>
	/// <param name="property">属性表达式。</param>
	/// <exception cref="ArgumentNullException"><paramref name="property"/> 为 null。</exception>
	public static void SetValue<TValue>(TTarget item, TValue value, Expression<Func<TTarget, TValue>> property)
	{
		ArgumentAssert.ThrowIfNull(property, nameof(property));

		var propertyInfo = GetProperty(property);

		propertyInfo.SetValue(item, value);
	}

	/// <summary>
	/// 获取属性值。
	/// </summary>
	/// <param name="item">目标对象。</param>
	/// <param name="property">属性表达式。</param>
	/// <returns>属性值。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="property"/> 为 null。</exception>
	public static object GetValue(TTarget item, Expression<Func<TTarget, object>> property)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		var propertyInfo = GetProperty(property);

		return propertyInfo.GetValue(item);
	}

	/// <summary>
	/// 检测指定类型是否可赋值给泛型类型。
	/// </summary>
	/// <param name="genericType">目标泛型类型。</param>
	/// <returns>如果 <typeparamref name="TTarget"/> 可赋值给 <paramref name="genericType"/>，则为 true；否则为 false。</returns>
	public static bool IsAssignableToGenericType(Type genericType)
	{
		return Reflect.IsAssignableToGenericType(typeof(TTarget), genericType);
	}

	/// <summary>
	/// 获取 <typeparamref name="TTarget"/> 实现的泛型类型列表。
	/// </summary>
	/// <param name="genericType">目标泛型类型。</param>
	/// <returns>实现的泛型类型列表。</returns>
	public static List<Type> GetImplementedGenericTypes(Type genericType)
	{
		return Reflect.GetImplementedGenericTypes(typeof(TTarget), genericType);
	}

	/// <summary>
	/// 通过属性路径获取对象的属性值。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="propertyPath">属性路径（以 '.' 分隔）。</param>
	/// <returns>属性值。</returns>
	public static object GetValue(TTarget obj, string propertyPath)
	{
		return Reflect.GetValue(obj, typeof(TTarget), propertyPath);
	}

	/// <summary>
	/// 通过属性路径设置对象的属性值。
	/// </summary>
	/// <param name="obj">目标对象。</param>
	/// <param name="propertyPath">属性路径（以 '.' 分隔）。</param>
	/// <param name="value">要设置的值。</param>
	public static void SetValue(TTarget obj, string propertyPath, object value)
	{
		Reflect.SetValue(obj, typeof(TTarget), propertyPath, value);
	}

	/// <summary>
	/// 递归获取 <typeparamref name="TTarget"/> 类型中所有公共常量的值。
	/// </summary>
	/// <returns>常量值的字符串集合。</returns>
	public static IEnumerable<string> GetPublicConstantsRecursively()
	{
		return Reflect.GetPublicConstantsRecursively(typeof(TTarget));
	}
}