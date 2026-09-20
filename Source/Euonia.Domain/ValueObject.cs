namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 值对象契约。
/// </summary>
/// <typeparam name="TValueObject">值对象的类型。</typeparam>
public class ValueObject<TValueObject> : IValueObject, IEquatable<TValueObject>
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
				return ValuesEqual(left, right);
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

		return obj is TValueObject item && Equals(item);
	}

	/// <summary>
	/// 判断两个属性值是否相等。支持单值（含可空）与序列（<see cref="System.Collections.IEnumerable"/>，如集合/数组/列表）的语义比较。
	/// </summary>
	private static bool ValuesEqual(object left, object right)
	{
		if (left == null || right == null)
		{
			return left == right;
		}

		if (ReferenceEquals(left, right))
		{
			return true;
		}

		if (left is IEnumerable sequenceLeft && right is IEnumerable sequenceRight)
		{
			return sequenceLeft.Cast<object>().SequenceEqual(sequenceRight.Cast<object>());
		}

		return left is TValueObject ? ReferenceEquals(left, right) : left.Equals(right);
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
				hashCode = hashCode * (changeMultiplier ? 59 : 114) + GetValueHashCode(value);

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
	/// 计算单个属性值的哈希码；序列按元素逐个累加，与 <see cref="ValuesEqual(object, object)"/> 的序列语义保持一致。
	/// </summary>
	private static int GetValueHashCode(object value)
	{
		return value switch
		{
			null => 0,
			IEnumerable sequence => sequence.Cast<object>().Aggregate(17, (hash, element) => hash * 31 + GetValueHashCode(element)),
			_ => value.GetHashCode(),
		};
	}

	/// <summary>
	/// 返回包含全部公共属性取值的结构化字符串。
	/// </summary>
	/// <returns>当前值的结构化表示。</returns>
	public override string ToString()
	{
		var publicProperties = GetType().GetProperties();

		if (publicProperties.Length == 0)
		{
			return GetType().Name;
		}

		var parts = publicProperties
			.Select(property =>
			{
				var value = property.GetValue(this, null);
				return $"{property.Name} = {FormatValue(value)}";
			});

		return string.Join(Environment.NewLine, parts);
	}

	/// <summary>
	/// 将单值或序列格式化为可读文本。
	/// </summary>
	private static string FormatValue(object value)
	{
		return value switch
		{
			null => "null",
			string text => text,
			IEnumerable sequence => $"[{string.Join(", ", sequence.Cast<object>().Select(FormatValue))}]",
			_ => value.ToString(),
		};
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
