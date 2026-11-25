using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;


/// <summary>
/// 提供 AES 对称加解密能力的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.AES</c>。
/// </summary>
public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 使用调用方提供的密钥提供加密与解密工具的 AES 辅助类。
	/// </summary>
	public class AES
	{
		/// <summary>
		/// 使用由 UTF8 派生的密钥字符串加密指定的明文。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">密钥字符串；其 UTF8 字节用作 AES 密钥。</param>
		/// <returns>Base64 编码的密文；如果 source 为 null 或空则返回 null。</returns>
		public static string? Encrypt(string source, string key)
		{
			return Encrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用提供的密钥字节加密指定的明文。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">用于 AES 的密钥字节（长度须符合 AES 算法的要求）。</param>
		/// <returns>Base64 编码的密文；如果 source 为 null 或空则返回 null。</returns>
		public static string? Encrypt(string source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			var bytes = Encoding.UTF8.GetBytes(source);

			using var rm = Aes.Create();
			rm.Key = key;
			rm.Mode = CipherMode.ECB;
			rm.Padding = PaddingMode.PKCS7;
			var encryptor = rm.CreateEncryptor();
			var result = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
			return Convert.ToBase64String(result);
		}

		/// <summary>
		/// 使用由 UTF8 派生的密钥字符串解密指定的 Base64 编码密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <param name="key">密钥字符串；其 UTF8 字节用作 AES 密钥。</param>
		/// <returns>解密后的明文；如果 source 为 null 或空则返回 null。</returns>
		public static string? Decrypt(string source, string key)
		{
			return Decrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// 使用提供的密钥字节解密指定的 Base64 编码密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <param name="key">用于 AES 的密钥字节（长度须符合 AES 算法的要求）。</param>
		/// <returns>解密后的明文；如果 source 为 null 或空则返回 null。</returns>
		public static string? Decrypt(string source, byte[] key)
		{
			if (string.IsNullOrEmpty(source))
			{
				return null;
			}

			var bytes = Convert.FromBase64String(source);

			using var rm = Aes.Create();
			rm.Key = key;
			rm.Mode = CipherMode.ECB;
			rm.Padding = PaddingMode.PKCS7;

			var decryptor = rm.CreateDecryptor();
			var result = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);

			return Encoding.UTF8.GetString(result);
		}
	}
}
