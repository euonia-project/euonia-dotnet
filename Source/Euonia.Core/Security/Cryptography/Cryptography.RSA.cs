using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// RSA 非对称加密与解密辅助类。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 私钥支持 PEM、Base64 编码的 PKCS#1（RSAPrivateKey）或 PKCS#8（PrivateKeyInfo）DER；
	/// 公钥支持 PEM、Base64 编码的 SubjectPublicKeyInfo（SPKI）或 PKCS#1（RSAPublicKey）DER。
	/// </para>
	/// <para>
	/// 加密使用 PKCS#1 v1.5 填充；新协议建议改用 OAEP。
	/// 本类型持有非托管密钥材料，使用完毕后应调用 <see cref="Dispose"/>。
	/// </para>
	/// </remarks>
	public sealed class RSA : IDisposable
	{
		private readonly System.Security.Cryptography.RSA? _privateKey;
		private readonly System.Security.Cryptography.RSA? _publicKey;

		/// <summary>
		/// 使用指定的私钥和/或公钥初始化 <see cref="RSA"/>。
		/// </summary>
		/// <param name="privateKey">私钥材料；为 <see langword="null"/> 或空时不提供解密能力。</param>
		/// <param name="publicKey">公钥材料；为 <see langword="null"/> 或空时回退到私钥中的公钥部分（若私钥可用）。</param>
		/// <exception cref="ArgumentException">两个参数都为空。</exception>
		/// <exception cref="CryptographicException">密钥无法解析。</exception>
		public RSA(string? privateKey, string? publicKey = null)
		{
			if (string.IsNullOrWhiteSpace(privateKey) && string.IsNullOrWhiteSpace(publicKey))
			{
				throw new ArgumentException("必须至少提供私钥或公钥。", nameof(privateKey));
			}

			if (!string.IsNullOrWhiteSpace(privateKey))
			{
				_privateKey = ImportPrivateKey(privateKey);
				_publicKey = _privateKey;
			}

			if (!string.IsNullOrWhiteSpace(publicKey))
			{
				_publicKey = ImportPublicKey(publicKey);
			}
		}

		/// <summary>
		/// 使用公钥加密指定的明文。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <returns>Base64 编码的密文。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="InvalidOperationException">未配置公钥。</exception>
		public string Encrypt(string source)
		{
			ArgumentNullException.ThrowIfNull(source);

			if (_publicKey is null)
			{
				throw new InvalidOperationException("未配置公钥，无法加密。");
			}

			var bytes = _publicKey.Encrypt(Encoding.UTF8.GetBytes(source), RSAEncryptionPadding.Pkcs1);

			return Convert.ToBase64String(bytes);
		}

		/// <summary>
		/// 使用私钥解密指定的 Base64 密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <returns>解密后的明文。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="InvalidOperationException">未配置私钥。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		public string Decrypt(string source)
		{
			ArgumentNullException.ThrowIfNull(source);

			if (_privateKey is null)
			{
				throw new InvalidOperationException("未配置私钥，无法解密。");
			}

			var bytes = _privateKey.Decrypt(Convert.FromBase64String(source), RSAEncryptionPadding.Pkcs1);

			return Encoding.UTF8.GetString(bytes);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (!ReferenceEquals(_privateKey, _publicKey))
			{
				_publicKey?.Dispose();
			}

			_privateKey?.Dispose();
		}

		private static System.Security.Cryptography.RSA ImportPrivateKey(string key)
		{
			var rsa = System.Security.Cryptography.RSA.Create();

			try
			{
				if (key.Contains("-----BEGIN", StringComparison.Ordinal))
				{
					rsa.ImportFromPem(key);
					return rsa;
				}

				var bytes = DecodeBase64(key);

				try
				{
					rsa.ImportRSAPrivateKey(bytes, out _);
				}
				catch (CryptographicException)
				{
					rsa.ImportPkcs8PrivateKey(bytes, out _);
				}

				return rsa;
			}
			catch (Exception exception) when (exception is CryptographicException or ArgumentException or FormatException)
			{
				rsa.Dispose();

				throw new CryptographicException("无法解析 RSA 私钥：支持 PEM、Base64 编码的 PKCS#1 或 PKCS#8 DER。", exception);
			}
		}

		private static System.Security.Cryptography.RSA ImportPublicKey(string key)
		{
			var rsa = System.Security.Cryptography.RSA.Create();

			try
			{
				if (key.Contains("-----BEGIN", StringComparison.Ordinal))
				{
					rsa.ImportFromPem(key);
					return rsa;
				}

				var bytes = DecodeBase64(key);

				try
				{
					rsa.ImportSubjectPublicKeyInfo(bytes, out _);
				}
				catch (CryptographicException)
				{
					rsa.ImportRSAPublicKey(bytes, out _);
				}

				return rsa;
			}
			catch (Exception exception) when (exception is CryptographicException or ArgumentException or FormatException)
			{
				rsa.Dispose();

				throw new CryptographicException("无法解析 RSA 公钥：支持 PEM、Base64 编码的 SPKI 或 PKCS#1 DER。", exception);
			}
		}

		private static byte[] DecodeBase64(string value)
		{
			var normalized = new string(value.Where(character => !char.IsWhiteSpace(character)).ToArray());

			return Convert.FromBase64String(normalized);
		}
	}
}
