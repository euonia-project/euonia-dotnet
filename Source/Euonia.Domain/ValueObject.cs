namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 值对象契约。
/// </summary>
/// <typeparam name="TValueObject">值对象的类型。</typeparam>
public class ValueObject<TValueObject> : IEquatable<TValueObject>
	where TValueObject : ValueObject<TValueObject>
{
	#region IEquatable and Override Equals operators

	/// <summary>
	/// 判断此值对象是否与其他值对象相等。
	/// </summary>
	/// <param name="other">要比较的目标对象。</param>
	/// <returns>如果相等则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Equals(TValueObject other)
	{
		if (other == null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		// 比较所有公共属性
		var publicProperties = GetType().GetProperties();

		if (publicProperties.Length > 0)
		{
			return publicProperties.All(property =>
			{
				var left = property.GetValue(this, null);
				var right = property.GetValue(other, null);

				if (left == null || right == null)
				{
					return false;
				}

				return left is TValueObject ? ReferenceEquals(left, right) : left.Equals(right);
			});
		}

		return true;
	}

	/// <inheritdoc/>
	/// <param name="obj">要比较的对象。</param>
	/// <returns>如果相等则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		return obj is ValueObject<TValueObject> item && Equals((TValueObject)item);
	}

	/// <summary>
	/// 获取此值对象的哈希码。
	/// </summary>
	/// <returns>此值对象的哈希码。</returns>
	public override int GetHashCode()
	{
		var hashCode = 31;
		var changeMultiplier = false;
		const int index = 1;

		// 比较所有公共属性
		var publicProperties = GetType().GetProperties();

		if (publicProperties.Length == 0)
		{
			return hashCode;
		}

		foreach (var item in publicProperties)
		{
			var value = item.GetValue(this, null);

			if (value != null)
			{
				hashCode = hashCode * (changeMultiplier ? 59 : 114) + value.GetHashCode();

				changeMultiplier = !changeMultiplier;
			}
			else
			{
				hashCode ^= index * 13; // 仅用于支持 {"a",null,null,"a"} 与 {null,"a","a",null} 的区分
			}
		}

		return hashCode;
	}

	/// <summary>
	/// 实现 == 运算符。
	/// </summary>
	/// <param name="left">左侧操作数。</param>
	/// <param name="right">右侧操作数。</param>
	/// <returns>运算符的结果。</returns>
	public static bool operator ==(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
	{
		return left?.Equals(right) ?? Equals(right, null);
	}

	/// <summary>
	/// 实现 != 运算符。
	/// </summary>
	/// <param name="left">左侧操作数。</param>
	/// <param name="right">右侧操作数。</param>
	/// <returns>运算符的结果。</returns>
	public static bool operator !=(ValueObject<TValueObject> left, ValueObject<TValueObject> right)
	{
		return !(left == right);
	}

	#endregion
}
