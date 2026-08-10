using System.Linq.Expressions;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 作为所有业务对象基类的抽象类。
/// </summary>
/// <typeparam name="T">业务对象的具体类型。</typeparam>
public abstract class BusinessObject<T> : BusinessObject
	where T : BusinessObject<T>
{
	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="info">属性信息。</param>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(PropertyInfo<TValue> info)
	{
		return PropertyInfoManager.RegisterProperty(typeof(T), info);
	}

	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName = null)
	{
		return RegisterProperty(new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T)));
	}
	
	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="defaultValue">属性的默认值。</param>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName, TValue defaultValue)
	{
		var property = new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T), defaultValue);
		return RegisterProperty(property);
	}

	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="defaultValue">返回属性默认值的函数。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName, Func<TValue> defaultValue)
	{
		var property = new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T), defaultValue());
		return RegisterProperty(property);
	}

	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="expression">标识属性的表达式。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName = null)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty<TValue>(property.Name, friendlyName);
	}
	
	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <param name="expression">标识属性的表达式。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="defaultValue">属性的默认值。</param>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName, TValue defaultValue)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty(property.Name, friendlyName, defaultValue);
	}

	/// <summary>
	/// 注册一个属性。
	/// </summary>
	/// <param name="expression">标识属性的表达式。</param>
	/// <param name="friendlyName">属性的友好名称。</param>
	/// <param name="defaultValue">返回属性默认值的函数。</param>
	/// <typeparam name="TValue">属性值的类型。</typeparam>
	/// <returns>注册的属性信息。</returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName, Func<TValue> defaultValue)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty(property.Name, friendlyName, defaultValue());
	}
}