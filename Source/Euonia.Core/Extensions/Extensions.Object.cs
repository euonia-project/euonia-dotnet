using System.ComponentModel;
using System.Globalization;
using System.Reflection;

public static partial class Extensions
{
	/// <summary>
	/// 简化和美化将对象转换为类型。
	/// </summary>
	/// <typeparam name="T">要转换的目标类型</typeparam>
	/// <param name="obj">要转换的对象</param>
	/// <returns>转换后的对象</returns>
	public static T As<T>(this object obj)
		where T : class
	{
		return (T)obj;
	}

	/// <summary>
	/// 使用 <see cref="Convert.ChangeType(object,System.Type)"/> 方法将给定对象转换为值类型。
	/// </summary>
	/// <param name="obj">要转换的对象</param>
	/// <typeparam name="T">目标对象的类型</typeparam>
	/// <returns>转换后的对象</returns>
	public static T To<T>(this object obj)
		where T : struct
	{
		if (obj == null)
		{
			throw new NullReferenceException();
		}

		if (typeof(T) == obj.GetType())
		{
			return (T)obj;
		}

		if (typeof(T) == typeof(Guid))
		{
			// ReSharper disable once PossibleNullReferenceException
			return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.ToString());
		}

		return (T)System.Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// 检查元素是否在列表中。
	/// </summary>
	/// <param name="item">要检查的元素</param>
	/// <param name="list">元素列表</param>
	/// <typeparam name="T">元素类型</typeparam>
	public static bool IsIn<T>(this T item, params T[] list)
	{
		return list.Contains(item);
	}

	/// <summary>
	/// 检查元素是否在给定的集合中。
	/// </summary>
	/// <param name="item">要检查的元素</param>
	/// <param name="items">元素集合</param>
	/// <typeparam name="T">元素类型</typeparam>
	/// <returns>如果元素在集合中，则返回 true；否则返回 false。</returns>
	public static bool IsIn<T>(this T item, IEnumerable<T> items)
	{
		return items.Contains(item);
	}

	/// <summary>
	/// 使用指定的比较器检查元素是否在集合中。
	/// </summary>
	/// <param name="item">要检查的元素</param>
	/// <param name="items">元素集合</param>
	/// <param name="comparer">用于比较元素的比较器</param>
	/// <typeparam name="T">元素类型</typeparam>
	/// <returns>如果元素在集合中，则返回 true；否则返回 false。</returns>
	public static bool IsIn<T>(this T item, IEnumerable<T> items, IEqualityComparer<T> comparer)
	{
		return items.Contains(item, comparer);
	}

	/// <summary>
	/// 根据条件对对象执行函数并返回修改后或原始的对象。适用于链式调用。
	/// </summary>
	/// <typeparam name="T">对象类型</typeparam>
	/// <param name="obj">要操作的对象</param>
	/// <param name="condition">条件</param>
	/// <param name="func">要执行的函数</param>
	/// <returns>如果条件为 true，则返回函数执行后的对象；否则返回原始对象。</returns>
	public static T If<T>(this T obj, bool condition, Func<T, T> func)
	{
		return condition ? func(obj) : obj;
	}

	/// <summary>
	/// 根据条件对对象执行操作并返回原始对象。适用于链式调用。
	/// </summary>
	/// <typeparam name="T">对象类型</typeparam>
	/// <param name="obj">要操作的对象</param>
	/// <param name="condition">条件</param>
	/// <param name="action">要执行的操作</param>
	/// <returns>返回原始对象。</returns>
	public static T If<T>(this T obj, bool condition, Action<T> action)
	{
		if (condition)
		{
			action(obj);
		}

		return obj;
	}

	/// <summary>
	/// 检查类型是否定义了指定特性。
	/// </summary>
	/// <typeparam name="TAttribute">要检查的特性类型</typeparam>
	/// <param name="type">要检查的类型</param>
	/// <param name="inherit">是否搜索继承链以查找特性</param>
	/// <returns>如果类型定义了指定特性，则返回 true；否则返回 false。</returns>
	public static bool HasAttribute<TAttribute>(this Type type, bool inherit = true)
		where TAttribute : Attribute
	{
		var attribute = type.GetCustomAttributes<TAttribute>(inherit);
		return attribute.Any();
	}

	/// <summary>
	/// 检查方法是否定义了指定特性。
	/// </summary>
	/// <typeparam name="TAttribute">要检查的特性类型</typeparam>
	/// <param name="method">要检查的方法</param>
	/// <param name="inherit">是否搜索继承链以查找特性</param>
	/// <returns>如果方法定义了指定特性，则返回 true；否则返回 false。</returns>
	public static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = true)
		where TAttribute : Attribute
	{
		var attribute = method.GetCustomAttributes<TAttribute>(inherit);
		return attribute.Any();
	}

	/// <summary>
	/// 检查对象类型是否定义了指定特性，并输出该特性。
	/// </summary>
	/// <typeparam name="TAttribute">要检查的特性类型</typeparam>
	/// <param name="source">要检查的对象</param>
	/// <param name="attribute">输出的特性</param>
	/// <param name="inherit">是否搜索继承链以查找特性</param>
	/// <returns>如果对象类型定义了指定特性，则返回 true；否则返回 false。</returns>
	public static bool HasAttribute<TAttribute>(this object source, out TAttribute attribute, bool inherit = true)
		where TAttribute : Attribute
	{
		var type = source.GetType();
		attribute = type.GetCustomAttribute<TAttribute>(inherit);
		return attribute != null;
	}

	/// <summary>
	/// 检查对象类型是否定义了指定特性，并输出所有匹配的特性。
	/// </summary>
	/// <typeparam name="TAttribute">要检查的特性类型</typeparam>
	/// <param name="source">要检查的对象</param>
	/// <param name="attributes">输出的特性集合</param>
	/// <param name="inherit">是否搜索继承链以查找特性</param>
	/// <returns>如果对象类型定义了指定特性，则返回 true；否则返回 false。</returns>
	public static bool HasAttribute<TAttribute>(this object source, out IEnumerable<TAttribute> attributes, bool inherit = true)
		where TAttribute : Attribute
	{
		var type = source.GetType();
		attributes = type.GetCustomAttributes<TAttribute>(inherit);
		return attributes.Any();
	}
}