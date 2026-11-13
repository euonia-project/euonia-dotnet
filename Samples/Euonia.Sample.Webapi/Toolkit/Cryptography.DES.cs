using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 提供 DES/AES 加解密兼容实现的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.DES</c>。
/// </summary>
public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 提供加密与解密方法的 DES 辅助类。注意：部分方法在处理 32 字节密钥重载时内部使用 AES/Rijndael。
	/// </summary>
	public class DES
	{
		/// <summary>
		/// 无密钥参数的重载所使用的默认 32 字节盐/密钥。
		/// </summary>
		private static readonly byte[] _defaultSalt = { 0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x4B, 0x9B, 0x93, 0x8B, 0x83, 0x7B, 0x73, 0x6B, 0x63, 0x5B, 0x53, 0xF3, 0xFB, 0xA3, 0xAB, 0xB3, 0xBB, 0xC3, 0xEB, 0xE3, 0xDB, 0xD3, 0xCB };

		#region Encrypt

		/// <summary>
		/// 使用默认 32 字节密钥加密指定的字符串（内部使用 ECB 模式的 AES 算法）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <returns>Base64 编码的密文。</returns>
		public static string Encrypt(string source)
		{
			return Encrypt(source, _defaultSalt);
		}

		/// <summary>
		/// 使用提供的 32 字节密钥加密指定的字符串（内部使用 ECB 模式的 AES 算法）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">用作 AES 密钥的 32 字节密钥。</param>
		/// <returns>Base64 编码的密文。</returns>
		public static string Encrypt(string source, byte[] key)
		{
			using SymmetricAlgorithm sa = Aes.Create(); //Rijndael.Create();
			sa.Key = key;
			sa.Mode = CipherMode.ECB;
			sa.Padding = PaddingMode.PKCS7;
			using var ms = new MemoryStream();
			using var cs = new CryptoStream(ms, sa.CreateEncryptor(), CryptoStreamMode.Write);
			var byt = Encoding.Unicode.GetBytes(source);
			cs.Write(byt, 0, byt.Length);
			cs.FlushFinalBlock();
			cs.Close();
			return Convert.ToBase64String(ms.ToArray());
		}

		/// <summary>
		/// 使用提供密钥字符串的 DES 加密指定的字符串（密钥字节由密钥的 UTF8 编码派生）。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <param name="key">用于派生 DES 密钥字节的密钥字符串。</param>
		/// <returns>Base64 编码的密文。</returns>
		public static string Encrypt(string source, string key)
		{
			using var provider = System.Security.Cryptography.DES.Create();
			{
				provider.Mode = CipherMode.ECB;
				provider.Padding = PaddingMode.PKCS7;
			}

			var keyBytes = Encoding.UTF8.GetBytes(key);
			var sourceBytes = Encoding.UTF8.GetBytes(source);
			using var memory = new MemoryStream();
			using var crypto = new CryptoStream(memory, provider.CreateEncryptor(keyBytes, keyBytes), CryptoStreamMode.Write);
			crypto.Write(sourceBytes, 0, sourceBytes.Length);
			crypto.FlushFinalBlock();
			return Convert.ToBase64String(memory.ToArray());
		}

		#endregion

		#region Decrypt

		/// <summary>
		/// 使用默认 32 字节密钥解密指定的 Base64 编码密文（内部使用 ECB 模式的 AES 算法）。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <returns>解密后的明文。</returns>
		public static string Decrypt(string source)
		{
			return Decrypt(source, _defaultSalt);
		}

		/// <summary>
		/// 使用提供的 32 字节密钥解密指定的 Base64 编码密文（内部使用 ECB 模式的 AES 算法）。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <param name="key">用作 AES 密钥的 32 字节密钥。</param>
		/// <returns>解密后的明文。</returns>
		public static string Decrypt(string source, byte[] key)
		{
			using SymmetricAlgorithm sa = Aes.Create(); //Rijndael.Create();
			sa.Key = key;
			sa.Mode = CipherMode.ECB;
			sa.Padding = PaddingMode.PKCS7;
			var ct = sa.CreateDecryptor();
			var byt = Convert.FromBase64String(source);
			using var ms = new MemoryStream(byt);
			using var cs = new CryptoStream(ms, ct, CryptoStreamMode.Read);
			using var sr = new StreamReader(cs, Encoding.Unicode);
			return sr.ReadToEnd();
		}

		#endregion
	}
}