using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 提供 <see cref="EntityEntry"/> 相关的扩展方法，用于安全地设置实体属性值。
/// </summary>
public static class EntityEntryExtensions
{
	/// <summary>
	/// 若实体包含指定名称的属性，则设置其当前值；属性不存在时不做任何操作。
	/// </summary>
	/// <typeparam name="TProperty">属性值的类型。</typeparam>
	/// <param name="entry">实体项。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <param name="value">要设置的属性值。</param>
	public static void TrySetProperty<TProperty>(this EntityEntry entry, string propertyName, TProperty value)
	{
		if (entry.Properties.Any(p => p.Metadata.Name == propertyName))
		{
			entry.Property(propertyName).CurrentValue = value;
		}
	}
}
