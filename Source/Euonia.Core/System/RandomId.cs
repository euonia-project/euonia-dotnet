using System.Security.Cryptography;

namespace System;

/// <summary>
/// 生成随机 ID 的工具类。
/// </summary>
internal class RandomId
{
	private const string Charset = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private static readonly RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();

	private static string GenerateKey()
	{
		var chars = Charset.ToCharArray();
		var random = CreateRandom();

		// Fisher-Yates 洗牌，保证均匀且不依赖共享可变状态。
		for (var i = chars.Length - 1; i > 0; i--)
		{
			var index = random.Next(i + 1);
			(chars[i], chars[index]) = (chars[index], chars[i]);
		}

		return new string(chars);
	}

	private static Random CreateRandom()
	{
		var seekBytes = new byte[4];
		_randomNumberGenerator.GetBytes(seekBytes);
		var seek = BitConverter.ToInt32(seekBytes, 0) & int.MaxValue;
		return new Random(seek);
	}

	/// <summary>
	/// 根据提供的种子生成随机 ID。
	/// </summary>
	/// <param name="seed">随机数种子。</param>
	/// <returns>生成的随机 ID 字符串。</returns>
	public static string Generate(long seed)
	{
		var key = GenerateKey();

		return Mixup(key, seed);
	}

	private static string Convert(string key, long value)
	{
		if (value < 62)
		{
			return key[(int)value].ToString();
		}

		var y = (int)(value % 62);
		var x = value / 62;
		return Convert(key, x) + key[y];
	}

	private static string Mixup(string key, long value)
	{
		var sequence = Convert(key, value);
		var salt = sequence.Aggregate(0, (current, seq) => current + seq);

		var x = salt % sequence.Length;

		var original = sequence.ToCharArray();
		var source = new char[original.Length];
		Array.Copy(original, x, source, 0, sequence.Length - x);
		Array.Copy(original, 0, source, sequence.Length - x, x);
		return source.Aggregate(string.Empty, ((current, @char) => current + @char));
	}
}