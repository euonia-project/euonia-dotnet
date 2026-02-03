using System.Linq.Expressions;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Abstract class that serves as the base for all business objects.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BusinessObject<T> : BusinessObject
	where T : BusinessObject<T>
{
	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <typeparam name="TValue">The property value.</typeparam>
	/// <param name="info"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(PropertyInfo<TValue> info)
	{
		return PropertyInfoManager.RegisterProperty(typeof(T), info);
	}

	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <typeparam name="TValue">The property value.</typeparam>
	/// <param name="propertyName"></param>
	/// <param name="friendlyName"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName = null)
	{
		return RegisterProperty(new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T)));
	}
	
	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <typeparam name="TValue">The property value.</typeparam>
	/// <param name="propertyName"></param>
	/// <param name="friendlyName"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName, TValue defaultValue)
	{
		var property = new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T), defaultValue);
		return RegisterProperty(property);
	}

	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <param name="friendlyName"></param>
	/// <param name="defaultValue"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(string propertyName, string friendlyName, Func<TValue> defaultValue)
	{
		var property = new PropertyInfo<TValue>(propertyName, friendlyName, typeof(T), defaultValue());
		return RegisterProperty(property);
	}

	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <typeparam name="TValue">The property value.</typeparam>
	/// <param name="expression"></param>
	/// <param name="friendlyName"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName = null)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty<TValue>(property.Name, friendlyName);
	}
	
	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <typeparam name="TValue">The property value.</typeparam>
	/// <param name="expression"></param>
	/// <param name="friendlyName"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName, TValue defaultValue)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty(property.Name, friendlyName, defaultValue);
	}

	/// <summary>
	/// Registers a property.
	/// </summary>
	/// <param name="expression"></param>
	/// <param name="friendlyName"></param>
	/// <param name="defaultValue"></param>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	protected static PropertyInfo<TValue> RegisterProperty<TValue>(Expression<Func<T, object>> expression, string friendlyName, Func<TValue> defaultValue)
	{
		var property = Reflect<T>.GetProperty(expression);
		return RegisterProperty(property.Name, friendlyName, defaultValue());
	}
}