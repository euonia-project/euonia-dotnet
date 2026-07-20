using System.ComponentModel;

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供类型转换和强制转换的辅助方法。
/// </summary>
public class TypeHelper
{
	/// <summary>
	/// 将值强制转换为所需的目标类型。
	/// </summary>
	/// <param name="desiredType">目标类型。</param>
	/// <param name="valueType">值的原始类型。</param>
	/// <param name="value">要转换的值。</param>
	/// <returns>转换后的对象。</returns>
	/// <remarks>
	/// 此方法处理多种转换场景，包括：直接赋值兼容、可空类型、枚举解析、基本类型转换以及通过 <see cref="TypeDescriptor"/> 进行的类型转换。
	/// </remarks>
	public static object CoerceValue(Type desiredType, Type valueType, object value)
	{
		if (desiredType.IsAssignableFrom(valueType))
		{
			// 类型匹配，直接返回值
			return value;
		}

		if (desiredType.IsGenericType)
		{
			if (desiredType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				if (value == null)
				{
					return null;
				}

				if (valueType == typeof(string) && Convert.ToString(value) == string.Empty)
				{
					return null;
				}
			}
		}

		desiredType = desiredType.GetPropertyType();

		if (desiredType.IsEnum)
		{
			if ((value as byte?).HasValue)
			{
				return Enum.Parse(desiredType, ((byte?)value).Value.ToString());
			}

			if ((value as short?).HasValue)
			{
				return Enum.Parse(desiredType, ((short?)value).Value.ToString());
			}

			if ((value as int?).HasValue)
			{
				return Enum.Parse(desiredType, ((int?)value).Value.ToString());
			}

			if ((value as long?).HasValue)
			{
				return Enum.Parse(desiredType, ((long?)value).Value.ToString());
			}
		}

		if (desiredType.IsEnum && (valueType == typeof(string) || Enum.GetUnderlyingType(desiredType) == valueType))
		{
			return Enum.Parse(desiredType, value.ToString() ?? string.Empty);
		}

		if ((desiredType.IsPrimitive || desiredType == typeof(decimal)) && valueType == typeof(string) && string.IsNullOrEmpty((string)value))
		{
			value = 0;
		}

		try
		{
			if (desiredType == typeof(string) && value != null)
			{
				return value.ToString();
			}

			return Convert.ChangeType(value, desiredType);
		}
		catch
		{
			var converter = TypeDescriptor.GetConverter(desiredType);
			if (valueType != null)
			{
				var cnv1 = TypeDescriptor.GetConverter(valueType);
				if (converter.CanConvertFrom(valueType))
				{
					return converter.ConvertFrom(value);
				}

				if (cnv1.CanConvertTo(desiredType))
				{
					return cnv1.ConvertTo(value!, desiredType);
				}
			}

			throw;
		}
	}

	/// <summary>
	/// 将值强制转换为指定的目标类型 <typeparamref name="T"/>。
	/// </summary>
	/// <typeparam name="T">目标类型。</typeparam>
	/// <param name="valueType">值的原始类型。</param>
	/// <param name="value">要转换的值。</param>
	/// <returns>转换后的 <typeparamref name="T"/> 类型值。</returns>
	public static T CoerceValue<T>(Type valueType, object value)
	{
		return (T)CoerceValue(typeof(T), valueType, value);
	}

	/// <summary>
	/// 将输入类型的值强制转换为输出类型。
	/// </summary>
	/// <typeparam name="TOutput">输出目标类型。</typeparam>
	/// <typeparam name="TInput">输入值类型。</typeparam>
	/// <param name="value">要转换的值。</param>
	/// <returns>转换后的 <typeparamref name="TOutput"/> 类型值。</returns>
	public static TOutput CoerceValue<TOutput, TInput>(TInput value)
	{
		return (TOutput)CoerceValue(typeof(TOutput), typeof(TInput), value);
	}
}