using System.Globalization;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 提供 <see cref="EntityEntry"/> 相关的扩展方法，用于安全地设置实体属性值。
/// </summary>
public static class EntityEntryExtensions
{
	/// <summary>
	/// 若实体包含指定名称的属性且值与属性类型兼容，则设置其当前值；属性不存在或类型不兼容时不做任何操作。
	/// </summary>
	/// <typeparam name="TProperty">属性值的类型。</typeparam>
	/// <param name="entry">实体项。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="value">要设置的属性值。</param>
	/// <remarks>
	/// 类型兼容指：值的运行时类型可赋值给属性类型（含 <see cref="Nullable{T}"/> 的基础类型），
	/// 或两者均实现 <see cref="IConvertible"/> 且可经 <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> 转换；
	/// <c>null</c> 仅允许写入引用类型或可空值类型属性。不兼容时静默跳过，以兑现 Try 语义。
	/// </remarks>
	public static void TrySetProperty<TProperty>(this EntityEntry entry, string propertyName, TProperty value)
	{
		var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
		if (property is null)
		{
			return;
		}

		var propertyType = property.Metadata.ClrType;
		var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		object? newValue = value;
		if (value is null)
		{
			// 非可空值类型属性不能接受 null，跳过。
			if (propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) is null)
			{
				return;
			}
		}
		else if (!underlyingType.IsInstanceOfType(value))
		{
			if (value is not IConvertible || !typeof(IConvertible).IsAssignableFrom(underlyingType))
			{
				return;
			}

			try
			{
				newValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
			}
			catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
			{
				// 转换失败视为类型不兼容，跳过。
				return;
			}
		}

		property.CurrentValue = newValue;
	}
}
