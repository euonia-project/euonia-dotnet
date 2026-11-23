using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// AES 加密与解密辅助类。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 使用 ECB 模式与 PKCS7 填充；ECB 对相同明文块产生相同密文块，
	/// 不适用于需要语义安全的新数据。新代码应使用随机 IV 的 CBC/GCM。
	/// </para>
	/// <para>
	/// 密钥长度必须为 16、24 或 32 字节。字符串密钥按其 UTF-8 字节使用。
	/// </para>
	/// </remarks>
	public static class AES
	{
		// ReSharper disable once InconsistentNaming
		private static readonly byte[] DefaultSalt =
		[
			0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x4B, 0x9B, 0x93, 0x8B, 0x83, 0x7B, 0x73,
			0x6B, 0x63, 0x5B, 0x53, 0xF3, 0xFB, 0xA3, 0xAB, 0xB3, 0xBB, 0xC3, 0xEB, 0xE3, 0xDB, 0xD3, 0xCB
		];

		/// <summary>
		/// 使用内置的默认密钥加密指定的明文。
		/// </summary>
		/// <param name="source">要加密的明文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <returns>Base64 编码的密文；输入为空时返回 <see langword="null"/>。</returns>
		public static string? Encrypt(string? source)
		{
			return Encrypt(source, DefaultSalt);
		}

		/// <summary>
		/// 使用 UTF-8 编码的密钥字符串加密指定的明文。
		/// </summary>
		/// <param name="source">要加密的明文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">密钥字符串，其 UTF-8 字节长度必须为 16、24 或 32。</param>
		/// <returns>Base64 编码的密文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentException"><paramref name="key"/> 为空或长度不合法。</exception>
		public static string? Encrypt(string? source, string key)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			return Encrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用指定的密钥字节加密明文。
		/// </summary>
		/// <param name="source">要加密的明文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">AES 密钥字节，长度必须为 16、24 或 32。</param>
		/// <returns>Base64 编码的密文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		public static string? Encrypt(string? source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			ArgumentNullException.ThrowIfNull(key);
			ValidateKey(key);

			var bytes = Encoding.UTF8.GetBytes(source);

			using var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.PKCS7;

			using var encryptor = aes.CreateEncryptor();
			var result = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

			return Convert.ToBase64String(result);
		}

		/// <summary>
		/// 使用内置的默认密钥解密指定的 Base64 密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <returns>解密后的明文；输入为空时返回 <see langword="null"/>。</returns>
		public static string? Decrypt(string? source)
		{
			return Decrypt(source, DefaultSalt);
		}

		/// <summary>
		/// 使用 UTF-8 编码的密钥字符串解密 Base64 密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">密钥字符串，其 UTF-8 字节长度必须为 16、24 或 32。</param>
		/// <returns>解密后的明文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentException"><paramref name="key"/> 为空或长度不合法。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		public static string? Decrypt(string? source, string key)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			return Decrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用指定的密钥字节解密 Base64 密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">AES 密钥字节，长度必须为 16、24 或 32。</param>
		/// <returns>解密后的明文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		public static string? Decrypt(string? source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			ArgumentNullException.ThrowIfNull(key);
			ValidateKey(key);

			var bytes = Convert.FromBase64String(source);

			using var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.PKCS7;

			using var decryptor = aes.CreateDecryptor();
			var result = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

			return Encoding.UTF8.GetString(result);
		}

		private static void ValidateKey(byte[] key)
		{
			if (key.Length is not (16 or 24 or 32))
			{
				throw new ArgumentException("AES 密钥长度必须为 16、24 或 32 字节。", nameof(key));
			}
		}
	}
}
