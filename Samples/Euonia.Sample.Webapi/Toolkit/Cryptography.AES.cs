using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;


public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// AES helper providing encryption and decryption utilities using provided keys or a default key.
	/// </summary>
	public class AES
	{
		private static readonly byte[] _defaultSalt = { 0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x4B, 0x9B, 0x93, 0x8B, 0x83, 0x7B, 0x73, 0x6B, 0x63, 0x5B, 0x53, 0xF3, 0xFB, 0xA3, 0xAB, 0xB3, 0xBB, 0xC3, 0xEB, 0xE3, 0xDB, 0xD3, 0xCB };

		/// <summary>
		/// Encrypts the specified plain text using the default key.
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <returns>Base64-encoded cipher text, or null if source is null or empty.</returns>
		public static string Encrypt(string source)
		{
			return Encrypt(source, _defaultSalt);
		}

		/// <summary>
		/// Encrypts the specified plain text using a UTF8-derived key string.
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <param name="key">Key string; its UTF8 bytes are used as AES key.</param>
		/// <returns>Base64-encoded cipher text, or null if source is null or empty.</returns>
		public static string Encrypt(string source, string key)
		{
			return Encrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// Encrypts the specified plain text using the provided key bytes.
		/// </summary>
		/// <param name="source">Plain text to encrypt.</param>
		/// <param name="key">Key bytes to use with AES (length must be appropriate for AES algorithm).</param>
		/// <returns>Base64-encoded cipher text, or null if source is null or empty.</returns>
		public static string Encrypt(string source, byte[] key)
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
		/// Decrypts the specified Base64-encoded cipher text using the default key.
		/// </summary>
		/// <param name="source">Base64-encoded cipher text.</param>
		/// <returns>Decrypted plain text, or null if source is null or empty.</returns>
		public static string Decrypt(string source)
		{
			return Decrypt(source, _defaultSalt);
		}

		/// <summary>
		/// Decrypts the specified Base64-encoded cipher text using a UTF8-derived key string.
		/// </summary>
		/// <param name="source">Base64-encoded cipher text.</param>
		/// <param name="key">Key string; its UTF8 bytes are used as AES key.</param>
		/// <returns>Decrypted plain text, or null if source is null or empty.</returns>
		public static string Decrypt(string source, string key)
		{
			return Decrypt(source, Encoding.UTF8.GetBytes(key));
		}

		/// <summary>
		/// Decrypts the specified Base64-encoded cipher text using the provided key bytes.
		/// </summary>
		/// <param name="source">Base64-encoded cipher text.</param>
		/// <param name="key">Key bytes to use with AES (length must be appropriate for AES algorithm).</param>
		/// <returns>Decrypted plain text, or null if source is null or empty.</returns>
		public static string Decrypt(string source, byte[] key)
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
