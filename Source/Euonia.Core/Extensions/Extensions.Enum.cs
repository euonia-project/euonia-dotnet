using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Nerosoft.Euonia.Reflection;

public static partial class Extensions
{
	/// <summary>
	/// 确定指定的枚举值是否已在枚举类型中定义。
	/// </summary>
	/// <typeparam name="TEnum">枚举类型。</typeparam>
	/// <param name="enum">要检查的枚举值。</param>
	/// <returns>如果枚举值已定义，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public static bool IsValid<TEnum>(this TEnum @enum)
		where TEnum : Enum
	{
		return Enum.IsDefined(typeof(TEnum), @enum);
	}

	/// <summary>
	/// 获取枚举字段上指定类型的特性。
	/// </summary>
	/// <typeparam name="T">特性类型。</typeparam>
	/// <param name="enum">要获取特性的枚举值。</param>
	/// <returns>返回枚举字段上的指定特性，如果不存在则返回 <c>null</c>。</returns>
	public static T GetAttribute<T>(this Enum @enum) where T : Attribute
	{
		return EnumHelper.GetCustomerAttribute<T>(@enum);
	}

	/// <summary>
	/// 获取枚举字段的描述文本。
	/// </summary>
	/// <param name="enum">要获取描述的枚举值。</param>
	/// <returns>返回枚举字段的描述文本，如果不存在则返回枚举值的字符串表示。</returns>
	/// <remarks>字段应添加 <see cref="DescriptionAttribute"/>。</remarks>
	public static string GetDescription(this Enum @enum)
	{
		var attribute = @enum.GetAttribute<DescriptionAttribute>();
		return attribute?.Description ?? @enum.ToString();
	}

	/// <summary>
	/// 获取指定枚举值的本地化描述。
	/// </summary>
	/// <param name="enum">要获取描述的枚举值。</param>
	/// <param name="resourceManager">用于查找本地化字符串的 <see cref="ResourceManager"/>。</param>
	/// <param name="resourceCulture">查找资源字符串时使用的 <see cref="CultureInfo"/>。传入 <c>null</c> 使用资源管理器的默认区域行为。</param>
	/// <returns>本地化描述字符串。</returns>
	/// <remarks>字段应添加 <see cref="DescriptionAttribute"/>，其 Description 属性应为资源键。</remarks>
	/// <exception cref="NullReferenceException">如果枚举字段未定义，则抛出异常。</exception>
	public static string GetDescription(this Enum @enum, ResourceManager resourceManager, CultureInfo resourceCulture = null)
	{
		resourceCulture ??= CultureInfo.CurrentCulture;

		var field = @enum.GetType().GetField(@enum.ToString());
		if (field == null)
		{
			throw new NullReferenceException($"Field '{@enum}' not defined.");
		}

		var attribute = field.GetCustomAttribute<DescriptionAttribute>();
		var key = attribute?.Description ?? @enum.ToString();
		var value = resourceManager.GetString(key, resourceCulture);
		return value;
	}

	/// <summary>
	/// 获取枚举值的所有标志位。
	/// </summary>
	/// <typeparam name="TEnum">枚举类型。</typeparam>
	/// <param name="enum">要获取标志位的枚举值。</param>
	/// <returns>返回枚举值的所有标志位。</returns>
	public static IEnumerable<TEnum> GetFlags<TEnum>(this Enum @enum)
		where TEnum : Enum
	{
		foreach (Enum item in Enum.GetValues(@enum.GetType()))
		{
			if (@enum.HasFlag(item))
			{
				yield return (TEnum)item;
			}
		}
	}

	/// <summary>
	/// 获取枚举值的所有标志位。
	/// </summary>
	/// <param name="enum">要获取标志位的枚举值。</param>
	/// <returns>返回枚举值的所有标志位。</returns>
	public static IEnumerable<Enum> GetFlags(this Enum @enum)
	{
		return GetFlags(@enum, Enum.GetValues(@enum.GetType()).Cast<Enum>().ToArray());
	}

	/// <summary>
	/// 获取枚举值的所有独立标志位。
	/// </summary>
	/// <param name="enum">要获取独立标志位的枚举值。</param>
	/// <returns>返回枚举值的所有独立标志位。</returns>
	public static IEnumerable<Enum> GetIndividualFlags(this Enum @enum)
	{
		return GetFlags(@enum, GetFlagValues(@enum.GetType()).ToArray());
	}

	/// <summary>
	/// 获取枚举值的所有标志位。
	/// </summary>
	/// <param name="enum">要获取标志位的枚举值。</param>
	/// <param name="values">用于计算标志位的枚举值数组。</param>
	/// <returns>返回枚举值的所有标志位。</returns>
	private static IEnumerable<Enum> GetFlags(Enum @enum, Enum[] values)
	{
		var bits = System.Convert.ToUInt64(@enum);
		var results = new List<Enum>();
		for (var i = values.Length - 1; i >= 0; i--)
		{
			var mask = System.Convert.ToUInt64(values[i]);
			if (i == 0 && mask == 0L)
			{
				break;
			}

			if ((bits & mask) != mask)
			{
				continue;
			}

			results.Add(values[i]);
			bits -= mask;
		}

		if (bits != 0L)
		{
			return Enumerable.Empty<Enum>();
		}

		if (System.Convert.ToUInt64(@enum) != 0L)
		{
			return results.Reverse<Enum>();
		}

		if (bits == System.Convert.ToUInt64(@enum) && values.Length > 0 && System.Convert.ToUInt64(values[0]) == 0L)
		{
			return values.Take(1);
		}

		return Enumerable.Empty<Enum>();
	}

	/// <summary>
	/// 获取枚举类型的所有独立标志位。
	/// </summary>
	/// <param name="enumType">枚举类型。</param>
	/// <returns>返回枚举类型的所有独立标志位。</returns>
	private static IEnumerable<Enum> GetFlagValues(Type enumType)
	{
		ulong flag = 0x1;
		foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
		{
			var bits = System.Convert.ToUInt64(value);
			if (bits == 0L)
			{
				continue; // 跳过零值
			}

			while (flag < bits)
			{
				flag <<= 1;
			}

			if (flag == bits)
			{
				yield return value;
			}
		}
	}

	/// <summary>
	/// 获取当前区域中 <see cref="DayOfWeek"/> 的缩写名称。
	/// </summary>
	/// <param name="dayOfWeek">要获取缩写名称的 <see cref="DayOfWeek"/>。</param>
	/// <returns>返回指定 <see cref="DayOfWeek"/> 的缩写名称。</returns>
	public static string AbbreviatedDayName(this DayOfWeek dayOfWeek)
	{
		return dayOfWeek.AbbreviatedDayName(CultureInfo.CurrentCulture);
	}
	
	/// <summary>
	/// 获取指定区域中 <see cref="DayOfWeek"/> 的缩写名称。
	/// </summary>
	/// <param name="dayOfWeek">要获取缩写名称的 <see cref="DayOfWeek"/>。</param>
	/// <param name="cultureInfo">指定的区域信息。</param>
	/// <returns>返回指定 <see cref="DayOfWeek"/> 的缩写名称。</returns>
	public static string AbbreviatedDayName(this DayOfWeek dayOfWeek, CultureInfo cultureInfo)
	{
		return cultureInfo.DateTimeFormat.AbbreviatedDayNames[(int)dayOfWeek];
	}
}