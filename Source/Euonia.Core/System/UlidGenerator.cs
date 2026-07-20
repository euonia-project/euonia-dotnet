using System.Security.Cryptography;

namespace System;

/// <summary>
/// ULID（通用唯一字典排序标识符）生成器。
/// </summary>
/// <remarks>
/// ULID 是 128 位通用唯一标识符，可按字典排序且 URL 安全。
/// </remarks>
internal static class UlidGenerator
{
	/// <summary>
	/// ULID 使用称为 Crockford's Base32 的特定 32 字符编码，包含数字和大写字母，
	/// 但排除 "I"、"L"、"O" 等字母以避免与数字混淆。
	/// </summary>
	private const string CROCKFORD_BASE32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

	private static readonly RandomNumberGenerator _randomNumberGenerator = RandomNumberGenerator.Create();

	/// <summary>
	/// 生成新的 ULID 字符串。
	/// </summary>
	/// <returns>26 字符的 ULID 字符串。</returns>
	public static string Generate()
	{
		var timestamp = GetTimestamp(); // 6 字节时间戳（48 位）
		var randomBytes = GetRandomBytes(); // 10 字节随机数据（80 位）
		return Encode(timestamp, randomBytes);
	}

	/// <summary>
	/// 获取当前 UTC 时间的 48 位时间戳，并将其转换为字节数组。
	/// </summary>
	/// <returns>6 字节的时间戳字节数组。</returns>
	private static byte[] GetTimestamp()
	{
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 获取 UTC 时间的总毫秒数
		var timestampBytes = BitConverter.GetBytes(timestamp); // 转换为字节

		if (BitConverter.IsLittleEndian)
			Array.Reverse(timestampBytes); // 确保大端序

		var result = new byte[6]; // ULID 的 48 位时间戳只需要 6 字节
		Array.Copy(timestampBytes, 2, result, 0, 6); // 提取最后 6 个字节

		return result;
	}

	/// <summary>
	/// 生成 10 字节的随机数据，用于 ULID 的随机部分。
	/// </summary>
	/// <returns>10 字节的随机字节数组。</returns>
	private static byte[] GetRandomBytes()
	{
		var randomBytes = new byte[10];
		_randomNumberGenerator.GetBytes(randomBytes);
		return randomBytes;
	}

	/// <summary>
	/// 将时间戳和随机字节编码为 26 字符的 ULID 字符串。
	/// </summary>
	/// <param name="timestamp">6 字节的时间戳字节数组。</param>
	/// <param name="randomBytes">10 字节的随机字节数组。</param>
	/// <returns>26 字符的 ULID 字符串。</returns>
	private static string Encode(byte[] timestamp, byte[] randomBytes)
	{
		var ulid = new StringBuilder(26);

		// 将 48 位时间戳（6 字节）转换为 Base32
		var ulidBytes = new byte[16]; // ULID 总共是 16 字节
		Array.Copy(timestamp, 0, ulidBytes, 0, 6);
		Array.Copy(randomBytes, 0, ulidBytes, 6, 10);
		foreach (int value in ulidBytes)
		{
			ulid.Append(CROCKFORD_BASE32[(value >> 3) & 0x1F]);
			ulid.Append(CROCKFORD_BASE32[value & 0x1F]);
		}

		return ulid.ToString();
	}
}