using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 用于生成随机数或随机字符串的实用工具类。
/// </summary>
public class RandomUtility : Random
{
	private static readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
	private readonly byte[] _uint32Buffer = new byte[4];

	/// <summary>
	/// 生成指定长度的随机字符串。
	/// </summary>
	/// <param name="length">要生成的字符串长度，默认 32；为奇数时实际长度向下取偶。</param>
	/// <returns>由小写十六进制字符组成的随机字符串。</returns>
	public static string GenerateRandomString(int length = 32)
	{
		var bytes = new byte[Convert.ToInt32(length / 2)];
		_generator.GetBytes(bytes);
		var hex = new StringBuilder(bytes.Length * 2);
		foreach (var b in bytes)
		{
			hex.Append($"{b:x2}");
		}

		return hex.ToString();
	}

	/// <summary>
	/// 返回一个非负随机数。
	/// </summary>
	/// <returns>
	/// 一个大于等于零且小于 <see cref="F:System.Int32.MaxValue"/> 的 32 位有符号整数。
	/// </returns>
	public override int Next()
	{
		_generator.GetBytes(_uint32Buffer);
		return BitConverter.ToInt32(_uint32Buffer, 0) & 0x7FFFFFFF;
	}

	/// <summary>
	/// 返回一个小于指定最大值的非负随机数。
	/// </summary>
	/// <param name="maxValue">要生成的随机数的排他上限。<paramref name="maxValue"/> 必须大于或等于零。</param>
	/// <returns>
	/// 一个大于等于零且小于 <paramref name="maxValue"/> 的 32 位有符号整数；也就是说，返回值范围通常包含零但不包含 <paramref name="maxValue"/>。但是，如果 <paramref name="maxValue"/> 等于零，则返回 <paramref name="maxValue"/>。
	/// </returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">
	/// 	<paramref name="maxValue"/> 小于零。
	/// </exception>
	public override int Next(int maxValue)
	{
		if (maxValue < 0)
			throw new ArgumentOutOfRangeException(nameof(maxValue));
		return Next(0, maxValue);
	}

	/// <summary>
	/// 返回指定范围内的随机数。
	/// </summary>
	/// <param name="minValue">所返回随机数的包含下限。</param>
	/// <param name="maxValue">所返回随机数的排他上限。<paramref name="maxValue"/> 必须大于或等于 <paramref name="minValue"/>。</param>
	/// <returns>
	/// 一个大于等于 <paramref name="minValue"/> 且小于 <paramref name="maxValue"/> 的 32 位有符号整数；也就是说，返回值范围包含 <paramref name="minValue"/> 但不包含 <paramref name="maxValue"/>。如果 <paramref name="minValue"/> 等于 <paramref name="maxValue"/>，则返回 <paramref name="minValue"/>。
	/// </returns>
	/// <exception cref="T:System.ArgumentOutOfRangeException">
	/// 	<paramref name="minValue"/> 大于 <paramref name="maxValue"/>。
	/// </exception>
	public override int Next(int minValue, int maxValue)
	{
		if (minValue > maxValue)
			throw new ArgumentOutOfRangeException(nameof(minValue));
		if (minValue == maxValue)
			return minValue;
		long diff = maxValue - minValue;

		while (true)
		{
			_generator.GetBytes(_uint32Buffer);
			var rand = BitConverter.ToUInt32(_uint32Buffer, 0);

			var max = (1 + (long)uint.MaxValue);
			var remainder = max % diff;
			if (rand < max - remainder)
			{
				return (int)(minValue + (rand % diff));
			}
		}
	}

	/// <summary>
	/// 返回一个介于 0.0 和 1.0 之间的随机数。
	/// </summary>
	/// <returns>
	/// 一个大于或等于 0.0 且小于 1.0 的双精度浮点数。
	/// </returns>
	public override double NextDouble()
	{
		_generator.GetBytes(_uint32Buffer);
		var rand = BitConverter.ToUInt32(_uint32Buffer, 0);
		return rand / (1.0 + uint.MaxValue);
	}

	/// <summary>
	/// 用随机数填充指定字节数组的元素。
	/// </summary>
	/// <param name="buffer">用于存放随机数的字节数组。</param>
	/// <exception cref="T:System.ArgumentNullException">
	/// 	<paramref name="buffer"/> 为 null。
	/// </exception>
	public override void NextBytes(byte[] buffer)
	{
		if (buffer == null)
			throw new ArgumentNullException(nameof(buffer));
		_generator.GetBytes(buffer);
	}
}