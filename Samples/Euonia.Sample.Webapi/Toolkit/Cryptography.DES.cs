using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// DES helper providing encryption and decryption methods. Note: some methods internally use AES/Rijndael for a 32-byte key overload.
	/// </summary>
	public class DES
	{
		/// <summary>
		/// Default 32-byte salt/key used by overloads that accept no key parameter.
		/// </summary>
		private static readonly byte[] _defaultSalt = { 0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x4B, 0x9B, 0x93, 0x8B, 0x83, 0x7B, 0x73, 0x6B, 0x63, 0x5B, 0x53, 0xF3, 0xFB, 0xA3, 0xAB, 0xB3, 0xBB, 0xC3, 0xEB, 0xE3, 0xDB, 0xD3, 0xCB };

		#region Encrypt

		/// <summary>
		/// Encrypts the specified string using the default 32-byte key (internally uses AES algorithm in ECB mode).
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <returns>Base64-encoded cipher text.</returns>
		public static string Encrypt(string source)
		{
			return Encrypt(source, _defaultSalt);
		}

		/// <summary>
		/// Encrypts the specified string using the provided 32-byte key (internally uses AES algorithm in ECB mode).
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <param name="key">32-byte key to use as AES key.</param>
		/// <returns>Base64-encoded cipher text.</returns>
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
		/// Encrypts the specified string using DES with the provided key string (key bytes derived from UTF8 of the key).
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <param name="key">Key string used to derive DES key bytes.</param>
		/// <returns>Base64-encoded cipher text.</returns>
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
		/// Decrypts the specified Base64-encoded cipher text using the default 32-byte key (internally uses AES algorithm in ECB mode).
		/// </summary>
		/// <param name="source">Base64-encoded cipher text.</param>
		/// <returns>Decrypted plain text.</returns>
		public static string Decrypt(string source)
		{
			return Decrypt(source, _defaultSalt);
		}

		/// <summary>
		/// Decrypts the specified Base64-encoded cipher text using the provided 32-byte key (internally uses AES algorithm in ECB mode).
		/// </summary>
		/// <param name="source">Base64-encoded cipher text.</param>
		/// <param name="key">32-byte key used as AES key.</param>
		/// <returns>Decrypted plain text.</returns>
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