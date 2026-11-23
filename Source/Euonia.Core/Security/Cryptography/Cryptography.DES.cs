using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// DES 辅助类，提供加密与解密方法。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 字符串密钥重载使用系统 <see cref="System.Security.Cryptography.DES"/>（ECB + PKCS7）；
	/// 32 字节密钥重载内部使用 ECB 模式的 AES，明文按 UTF-16（Unicode）编码。
	/// 这些行为为兼容既有数据而保留。
	/// </para>
	/// <para>
	/// 使用字符串密钥的加密没有对应的解密重载；解密请使用 32 字节密钥重载。
	/// </para>
	/// </remarks>
	public static class DES
	{
		// ReSharper disable once InconsistentNaming
		private static readonly byte[] DefaultSalt =
		[
			0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x4B, 0x9B, 0x93, 0x8B, 0x83, 0x7B, 0x73,
			0x6B, 0x63, 0x5B, 0x53, 0xF3, 0xFB, 0xA3, 0xAB, 0xB3, 0xBB, 0xC3, 0xEB, 0xE3, 0xDB, 0xD3, 0xCB
		];

		/// <summary>
		/// 使用默认的 32 字节密钥加密指定字符串（内部使用 ECB 模式的 AES）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <returns>Base64 编码的密文。</returns>
		public static string Encrypt(string source)
		{
			return Encrypt(source, DefaultSalt);
		}

		/// <summary>
		/// 使用提供的 32 字节密钥加密指定字符串（内部使用 ECB 模式的 AES）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">用作 AES 密钥的 32 字节密钥。</param>
		/// <returns>Base64 编码的密文。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		public static string Encrypt(string source, byte[] key)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(key);

			using var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.PKCS7;

			var bytes = Encoding.Unicode.GetBytes(source);

			using var output = new MemoryStream();
			using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
			{
				crypto.Write(bytes, 0, bytes.Length);
				crypto.FlushFinalBlock();
			}

			return Convert.ToBase64String(output.ToArray());
		}

		/// <summary>
		/// 使用 DES 与提供的密钥字符串加密指定字符串（密钥字节由密钥的 UTF-8 编码派生）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">用于派生 DES 密钥字节的密钥字符串；UTF-8 编码后必须为 8 字节。</param>
		/// <returns>Base64 编码的密文。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 的 UTF-8 编码长度不是 8 字节。</exception>
		public static string Encrypt(string source, string key)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(key);

#pragma warning disable SYSLIB0021 // DES 仅为兼容既有调用保留。
			using var provider = System.Security.Cryptography.DES.Create();
#pragma warning restore SYSLIB0021

			provider.Mode = CipherMode.ECB;
			provider.Padding = PaddingMode.PKCS7;

			var keyBytes = Encoding.UTF8.GetBytes(key);
			var sourceBytes = Encoding.UTF8.GetBytes(source);

			using var output = new MemoryStream();
			using (var crypto = new CryptoStream(output, provider.CreateEncryptor(keyBytes, keyBytes), CryptoStreamMode.Write))
			{
				crypto.Write(sourceBytes, 0, sourceBytes.Length);
				crypto.FlushFinalBlock();
			}

			return Convert.ToBase64String(output.ToArray());
		}

		/// <summary>
		/// 使用默认的 32 字节密钥解密指定的 Base64 编码密文（内部使用 ECB 模式的 AES）。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <returns>解密后的明文。</returns>
		public static string Decrypt(string source)
		{
			return Decrypt(source, DefaultSalt);
		}

		/// <summary>
		/// 使用提供的 32 字节密钥解密指定的 Base64 编码密文（内部使用 ECB 模式的 AES）。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <param name="key">用作 AES 密钥的 32 字节密钥。</param>
		/// <returns>解密后的明文。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		public static string Decrypt(string source, byte[] key)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(key);

			using var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.PKCS7;

			using var input = new MemoryStream(Convert.FromBase64String(source));
			using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
			using var reader = new StreamReader(crypto, Encoding.Unicode);

			return reader.ReadToEnd();
		}
	}
}
