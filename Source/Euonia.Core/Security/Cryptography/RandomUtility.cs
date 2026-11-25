using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 基于密码学安全随机数生成随机字符串、数字或字节，并提供 <see cref="Random"/> 的安全实现。
/// </summary>
/// <remarks>
/// 静态方法使用进程级共享的 <see cref="RandomNumberGenerator"/>，可安全并发调用；
/// 实例方法继承自 <see cref="Random"/>，实例本身不保证线程安全。
/// </remarks>
public class RandomUtility : Random
{
	private static readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();

	private readonly byte[] _buffer = new byte[8];

	/// <summary>
	/// 生成指定长度的随机十六进制字符串。
	/// </summary>
	/// <param name="length">字符串长度，默认 32。</param>
	/// <returns>由小写十六进制字符组成的随机字符串。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 小于 1。</exception>
	public static string GenerateRandomString(int length = 32)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

		var bytes = new byte[(length + 1) / 2];
		Generator.GetBytes(bytes);

		var hex = Convert.ToHexString(bytes).ToLowerInvariant();

		return hex[..length];
	}

	/// <summary>
	/// 生成指定长度的随机数字字符串。
	/// </summary>
	/// <param name="length">数字字符串长度，默认 6。</param>
	/// <returns>由 0-9 组成的随机字符串。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 小于 1。</exception>
	public static string GenerateRandomDigit(int length = 6)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

		var characters = new char[length];

		for (var index = 0; index < length; index++)
		{
			characters[index] = (char)('0' + RandomNumberGenerator.GetInt32(10));
		}

		return new string(characters);
	}

	/// <summary>
	/// 生成指定长度的密码学安全随机字节。
	/// </summary>
	/// <param name="length">字节数，必须大于 0。</param>
	/// <returns>随机字节数组。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 小于 1。</exception>
	public static byte[] GenerateRandomBytes(int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

		var bytes = new byte[length];
		Generator.GetBytes(bytes);

		return bytes;
	}

	/// <summary>
	/// 返回一个非负随机数。
	/// </summary>
	/// <returns>大于等于零且小于 <see cref="int.MaxValue"/> 的 32 位有符号整数。</returns>
	public override int Next()
	{
		Generator.GetBytes(_buffer);

		return BitConverter.ToInt32(_buffer, 0) & 0x7FFFFFFF;
	}

	/// <summary>
	/// 返回一个小于指定最大值的非负随机数。
	/// </summary>
	/// <param name="maxValue">随机数的独占上限，必须大于等于零。</param>
	/// <returns>大于等于零且小于 <paramref name="maxValue"/> 的整数。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> 小于零。</exception>
	public override int Next(int maxValue)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

		return Next(0, maxValue);
	}

	/// <summary>
	/// 返回指定范围内的随机数。
	/// </summary>
	/// <param name="minValue">包含下限。</param>
	/// <param name="maxValue">独占上限，必须大于等于 <paramref name="minValue"/>。</param>
	/// <returns>大于等于 <paramref name="minValue"/> 且小于 <paramref name="maxValue"/> 的整数。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> 大于 <paramref name="maxValue"/>。</exception>
	public override int Next(int minValue, int maxValue)
	{
		if (minValue > maxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(minValue));
		}

		if (minValue == maxValue)
		{
			return minValue;
		}

		var range = (ulong)((long)maxValue - minValue);
		var limit = ulong.MaxValue - ulong.MaxValue % range;
		ulong sample;

		do
		{
			Generator.GetBytes(_buffer);
			sample = BitConverter.ToUInt64(_buffer, 0);
		}
		while (sample >= limit);

		return (int)(minValue + (long)(sample % range));
	}

	/// <summary>
	/// 返回介于 0.0 与 1.0 之间的随机数。
	/// </summary>
	/// <returns>大于等于 0.0 且小于 1.0 的双精度浮点数。</returns>
	public override double NextDouble()
	{
		Generator.GetBytes(_buffer);
		var sample = BitConverter.ToUInt64(_buffer, 0) >> 11;

		return sample * (1.0 / (1UL << 53));
	}

	/// <summary>
	/// 用随机字节填充指定数组。
	/// </summary>
	/// <param name="buffer">要填充的数组。</param>
	/// <exception cref="ArgumentNullException"><paramref name="buffer"/> 为 <see langword="null"/>。</exception>
	public override void NextBytes(byte[] buffer)
	{
		ArgumentNullException.ThrowIfNull(buffer);

		Generator.GetBytes(buffer);
	}

	/// <summary>
	/// 用随机字节填充指定跨度。
	/// </summary>
	/// <param name="buffer">要填充的跨度。</param>
	public override void NextBytes(Span<byte> buffer)
	{
		Generator.GetBytes(buffer);
	}
}
