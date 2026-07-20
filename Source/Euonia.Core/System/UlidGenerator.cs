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

	private static byte[] GetTimestamp()
	{
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Get UTC time in milliseconds
		var timestampBytes = BitConverter.GetBytes(timestamp); // Convert to bytes

		if (BitConverter.IsLittleEndian)
			Array.Reverse(timestampBytes); // Ensure big-endian order

		var result = new byte[6]; // We only need 6 bytes for ULID's 48-bit timestamp
		Array.Copy(timestampBytes, 2, result, 0, 6); // Extract the last 6 bytes

		return result;
	}

	private static byte[] GetRandomBytes()
	{
		var randomBytes = new byte[10];
		_randomNumberGenerator.GetBytes(randomBytes);
		return randomBytes;
	}

	private static string Encode(byte[] timestamp, byte[] randomBytes)
	{
		var ulid = new StringBuilder(26);

		// Convert 48-bit timestamp (6 bytes) into Base32
		var ulidBytes = new byte[16]; // ULID is 16 bytes total
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