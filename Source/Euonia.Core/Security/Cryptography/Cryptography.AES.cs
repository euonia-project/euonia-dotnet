using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// AES 加密与解密辅助类。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 新代码请使用 <see cref="EncryptGcm(string?,string)"/> / <see cref="DecryptGcm(string?,string)"/> 等 GCM 方法：
	/// AES-GCM 使用随机 nonce 与认证标签，同时提供机密性与完整性保护。
	/// </para>
	/// <para>
	/// 显式密钥的 <see cref="Encrypt(string?,string)"/> / <see cref="Decrypt(string?,string)"/> 重载使用 ECB + PKCS7，
	/// 仅用于兼容遗留数据；ECB 对相同明文块产生相同密文块，不适用于需要语义安全的新数据。
	/// </para>
	/// <para>
	/// 密钥长度必须为 16、24 或 32 字节。字符串密钥按其 UTF-8 字节使用。
	/// </para>
	/// </remarks>
	public static class AES
	{
		private const int NonceSize = 12;
		private const int TagSize = 16;

		/// <summary>
		/// 使用 UTF-8 编码的密钥字符串加密指定的明文（ECB + PKCS7）。
		/// </summary>
		/// <remarks>
		/// 仅用于兼容遗留数据；新代码请使用 <see cref="EncryptGcm(string?,string)"/>。
		/// </remarks>
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
		/// 使用指定的密钥字节加密明文（ECB + PKCS7）。
		/// </summary>
		/// <remarks>
		/// 仅用于兼容遗留数据；新代码请使用 <see cref="EncryptGcm(string?,byte[])"/>。
		/// </remarks>
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
		/// 使用 UTF-8 编码的密钥字符串解密 Base64 密文（ECB + PKCS7）。
		/// </summary>
		/// <remarks>
		/// 仅用于兼容遗留数据；新代码请使用 <see cref="DecryptGcm(string?,string)"/>。
		/// </remarks>
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
		/// 使用指定的密钥字节解密 Base64 密文（ECB + PKCS7）。
		/// </summary>
		/// <remarks>
		/// 仅用于兼容遗留数据；新代码请使用 <see cref="DecryptGcm(string?,byte[])"/>。
		/// </remarks>
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

		/// <summary>
		/// 使用 AES-GCM 与 UTF-8 编码的密钥字符串加密指定的明文，是新代码的推荐加密方式。
		/// </summary>
		/// <param name="source">要加密的明文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">密钥字符串，其 UTF-8 字节长度必须为 16、24 或 32。</param>
		/// <returns>Base64 编码的密文载荷；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentException"><paramref name="key"/> 为空或长度不合法。</exception>
		/// <remarks>
		/// 输出格式为 Base64(nonce(12 字节) + 认证标签(16 字节) + 密文)。nonce 每次随机生成，
		/// 相同明文两次加密产生不同密文；解密请使用 <see cref="DecryptGcm(string?,string)"/>。
		/// </remarks>
		public static string? EncryptGcm(string? source, string key)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			return EncryptGcm(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用 AES-GCM 与指定的密钥字节加密明文，是新代码的推荐加密方式。
		/// </summary>
		/// <param name="source">要加密的明文；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">AES 密钥字节，长度必须为 16、24 或 32。</param>
		/// <returns>Base64 编码的密文载荷；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		/// <remarks>
		/// 输出格式为 Base64(nonce(12 字节) + 认证标签(16 字节) + 密文)。nonce 每次随机生成，
		/// 相同明文两次加密产生不同密文；解密请使用 <see cref="DecryptGcm(string?,byte[])"/>。
		/// </remarks>
		public static string? EncryptGcm(string? source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			ArgumentNullException.ThrowIfNull(key);
			ValidateKey(key);

			var plaintext = Encoding.UTF8.GetBytes(source);
			var nonce = RandomNumberGenerator.GetBytes(NonceSize);
			var ciphertext = new byte[plaintext.Length];
			var tag = new byte[TagSize];

			using var gcm = new AesGcm(key, TagSize);
			gcm.Encrypt(nonce, plaintext, ciphertext, tag);

			var payload = new byte[NonceSize + TagSize + ciphertext.Length];
			nonce.CopyTo(payload, 0);
			tag.CopyTo(payload, NonceSize);
			ciphertext.CopyTo(payload, NonceSize + TagSize);

			return Convert.ToBase64String(payload);
		}

		/// <summary>
		/// 使用 AES-GCM 与 UTF-8 编码的密钥字符串解密 Base64 密文载荷。
		/// </summary>
		/// <param name="source">Base64 编码的密文载荷（nonce + 认证标签 + 密文）；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">密钥字符串，其 UTF-8 字节长度必须为 16、24 或 32。</param>
		/// <returns>解密后的明文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentException"><paramref name="key"/> 为空或长度不合法。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		/// <exception cref="CryptographicException"><paramref name="source"/> 长度不足以包含 nonce 与认证标签，或密文/标签被篡改导致认证失败。</exception>
		/// <remarks>
		/// 解密时校验认证标签；密文、nonce 或标签的任何篡改都会抛出 <see cref="CryptographicException"/>，不会返回部分明文。
		/// </remarks>
		public static string? DecryptGcm(string? source, string key)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			return DecryptGcm(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用 AES-GCM 与指定的密钥字节解密 Base64 密文载荷。
		/// </summary>
		/// <param name="source">Base64 编码的密文载荷（nonce + 认证标签 + 密文）；为 <see langword="null"/> 或空时返回 <see langword="null"/>。</param>
		/// <param name="key">AES 密钥字节，长度必须为 16、24 或 32。</param>
		/// <returns>解密后的明文；输入为空时返回 <see langword="null"/>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> 长度不合法。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		/// <exception cref="CryptographicException"><paramref name="source"/> 长度不足以包含 nonce 与认证标签，或密文/标签被篡改导致认证失败。</exception>
		/// <remarks>
		/// 解密时校验认证标签；密文、nonce 或标签的任何篡改都会抛出 <see cref="CryptographicException"/>，不会返回部分明文。
		/// </remarks>
		public static string? DecryptGcm(string? source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			ArgumentNullException.ThrowIfNull(key);
			ValidateKey(key);

			var payload = Convert.FromBase64String(source);

			if (payload.Length < NonceSize + TagSize)
			{
				throw new CryptographicException("AES-GCM 密文载荷不合法：长度不足以包含 nonce 与认证标签。");
			}

			var nonce = payload.AsSpan(0, NonceSize);
			var tag = payload.AsSpan(NonceSize, TagSize);
			var ciphertext = payload.AsSpan(NonceSize + TagSize);
			var plaintext = new byte[ciphertext.Length];

			using var gcm = new AesGcm(key, TagSize);
			gcm.Decrypt(nonce, ciphertext, tag, plaintext);

			return Encoding.UTF8.GetString(plaintext);
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
