using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

public static partial class Extensions
{
	/// <summary>
	/// 定义基本类型的集合。
	/// </summary>
	private static readonly Type[] _primitiveTypes =
	{
		typeof(string),
		typeof(decimal),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(TimeSpan),
		typeof(Guid),
#if NET5_0_OR_GREATER
		typeof(DateOnly),
		typeof(TimeOnly),
#endif
	};

	/// <summary>
	/// 获取类型的包含程序集名称的完整名称。
	/// </summary>
	/// <param name="type">要获取完整名称的类型。</param>
	/// <returns>类型的完整名称，包含程序集名称。</returns>
	public static string GetFullNameWithAssemblyName(this Type type)
	{
		return type.FullName + ", " + type.Assembly.GetName().Name;
	}

	/// <summary>
	/// 确定此类型的实例是否可以赋值给 <typeparamref name="TTarget"/> 的实例。
	/// 内部使用 <see cref="Type.IsAssignableFrom"/>。
	/// </summary>
	/// <typeparam name="TTarget">目标类型</typeparam>
	/// <param name="type">要检查的类型。</param>
	/// <returns>如果此类型的实例可以赋值给 <typeparamref name="TTarget"/> 的实例，则为 true；否则为 false。</returns>
	public static bool IsAssignableTo<TTarget>([NotNull] this Type type)
	{
		Check.EnsureNotNull(type, nameof(type));

		return type.IsAssignableTo(typeof(TTarget));
	}

	/// <summary>
	/// 确定此类型的实例是否可以赋值给 <paramref name="targetType"/> 的实例。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <param name="targetType">目标类型。</param>
	/// <returns>如果此类型的实例可以赋值给 <paramref name="targetType"/> 的实例，则为 true；否则为 false。</returns>
	public static bool IsAssignableTo([NotNull] this Type type, [NotNull] Type targetType)
	{
		Check.EnsureNotNull(type, nameof(type));
		Check.EnsureNotNull(targetType, nameof(targetType));

		return targetType.IsAssignableFrom(type);
	}

	/// <summary>
	/// 确定此类型的实例是否可以赋值给泛型类型 <paramref name="genericType"/> 的实例。
	/// 内部检查所有接口和基类型。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <param name="genericType">目标泛型类型。</param>
	/// <returns>如果此类型的实例可以赋值给泛型类型 <paramref name="genericType"/> 的实例，则为 true；否则为 false。</returns>
	public static bool IsAssignableToGeneric([NotNull] this Type type, [NotNull] Type genericType)
	{
		Check.EnsureNotNull(type, nameof(type));
		Check.EnsureNotNull(genericType, nameof(genericType));

		var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
		if (isTheRawGenericType)
		{
			return true;
		}

		while (type != null && type != typeof(object))
		{
			isTheRawGenericType = IsTheRawGenericType(type);
			if (isTheRawGenericType)
				return true;
			type = type.BaseType;
		}

		return false;

		bool IsTheRawGenericType(Type test) => genericType == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
	}

	/// <summary>
	/// 获取此类型的所有基类。
	/// </summary>
	/// <param name="type">要获取其基类的类型。</param>
	/// <param name="includeObject">是否在返回数组中包含 <see cref="object"/> 类型。</param>
	/// <returns>此类型的所有基类。</returns>
	public static Type[] GetBaseClasses([NotNull] this Type type, bool includeObject = true)
	{
		Check.EnsureNotNull(type, nameof(type));

		var types = new List<Type>();
		AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject);
		return types.ToArray();
	}

	/// <summary>
	/// 获取此类型的所有基类，可以指定停止类型。
	/// </summary>
	/// <param name="type">要获取其基类的类型。</param>
	/// <param name="stoppingType">停止向更深基类遍历的类型，此类型将包含在返回数组中。</param>
	/// <param name="includeObject">是否在返回数组中包含 <see cref="object"/> 类型。</param>
	/// <returns>此类型的所有基类，直到指定的停止类型。</returns>
	public static Type[] GetBaseClasses([NotNull] this Type type, Type stoppingType, bool includeObject = true)
	{
		Check.EnsureNotNull(type, nameof(type));

		var types = new List<Type>();
		AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
		return types.ToArray();
	}

	/// <summary>
	/// 递归地将类型及其所有基类添加到集合中，直到达到指定的停止类型。
	/// </summary>
	/// <param name="types">要添加类型的集合。</param>
	/// <param name="type">当前类型。</param>
	/// <param name="includeObject">是否在集合中包含 <see cref="object"/> 类型。</param>
	/// <param name="stoppingType">达到此类型时停止递归。</param>
	private static void AddTypeAndBaseTypesRecursively([NotNull] ICollection<Type> types, Type type, bool includeObject, Type stoppingType = null)
	{
		if (type == null || type == stoppingType)
		{
			return;
		}

		if (!includeObject && type == typeof(object))
		{
			return;
		}

		AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
		types.Add(type);
	}

	/// <summary>
	/// 检测方法是否为异步方法。
	/// </summary>
	/// <param name="method">要检查的 <see cref="MethodInfo"/> 实例。</param>
	/// <returns>如果方法是异步的，则为 true；否则为 false。</returns>
	public static bool IsAsync([NotNull] this MethodInfo method)
	{
		if (method == null)
		{
			throw new NullReferenceException("The method instance is null.");
		}

		var returnType = method.ReturnType;
		return returnType == typeof(Task) || (returnType.IsGenericType && returnType.GetInterfaces().Any(type => type == typeof(IAsyncResult)));
	}

	/// <summary>
	/// 获取属性类型，如果是 <see cref="Nullable{T}"/> 则返回其基础类型。
	/// </summary>
	/// <param name="propertyType">要检查的属性类型。</param>
	/// <returns>属性的基础类型，如果不是 <see cref="Nullable{T}"/> 则返回原类型。</returns>
	public static Type GetPropertyType(this Type propertyType)
	{
		if (propertyType.IsGenericType && (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
		{
			return Nullable.GetUnderlyingType(propertyType);
		}

		return propertyType;
	}

	/// <summary>
	/// 检测指定类型是否继承自目标类型。
	/// </summary>
	/// <typeparam name="T">目标类型。</typeparam>
	/// <param name="type">要检查的类型。</param>
	/// <returns>如果指定类型继承自目标类型，则为 true；否则为 false。</returns>
	public static bool IsExtends<T>(this Type type)
	{
		return type.IsExtends(typeof(T));
	}

	/// <summary>
	/// 检测指定类型是否继承自目标类型。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <param name="targetType">目标类型。</param>
	/// <returns>如果指定类型继承自目标类型，则为 true；否则为 false。</returns>
	public static bool IsExtends(this Type type, Type targetType)
	{
		var baseType = type.BaseType;

		while (baseType != null && baseType != typeof(object))
		{
			if (baseType == targetType)
			{
				return true;
			}

			baseType = baseType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// 确定指定类型是否实现了目标类型。
	/// </summary>
	/// <typeparam name="T">目标类型。</typeparam>
	/// <param name="type">要检查的类型。</param>
	/// <returns>如果指定类型实现了目标类型，则为 true；否则为 false。</returns>
	public static bool IsImplements<T>(this Type type)
	{
		return type.IsAssignableTo(typeof(T));
	}

	/// <summary>
	/// 确定指定类型是否实现了目标类型。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <param name="targetType">目标类型。</param>
	/// <returns>如果指定类型实现了目标类型，则为 true；否则为 false。</returns>
	public static bool IsImplements(this Type type, Type targetType)
	{
		return type.IsAssignableTo(targetType);
	}

	/// <summary>
	/// 确定指定类型是否实现了泛型接口。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <param name="targetType">目标泛型接口类型。</param>
	/// <returns>如果指定类型实现了泛型接口，则为 true；否则为 false。</returns>
	public static bool IsImplementsGeneric(this Type type, Type targetType)
	{
		return type.GetInterfaces().Any(f => f.IsGenericType && f.GetGenericTypeDefinition() == targetType);
	}

	/// <summary>
	/// 确定指定类型是否为基本类型。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <returns>如果指定类型为基本类型，则为 true；否则为 false。</returns>
	public static bool IsPrimitiveType(this Type type)
	{
		Check.EnsureNotNull(type, nameof(type));
		return type.IsPrimitive || type.IsEnum || type.IsIn(_primitiveTypes);
	}

	/// <summary>
	/// 确定指定类型是否为匿名类型。
	/// </summary>
	/// <param name="type">要检查的类型。</param>
	/// <returns>如果指定类型为匿名类型，则为 true；否则为 false。</returns>
	public static bool IsAnonymousType(this Type type)
	{
		return type.FullName != null && type.HasAttribute<CompilerGeneratedAttribute>() && type.FullName.Contains("AnonymousType");
	}
}