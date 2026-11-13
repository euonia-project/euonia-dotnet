using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 提供 RSA 非对称加解密能力的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.RSA</c>。
/// </summary>
public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 使用 PEM/base64 编码密钥执行非对称加密和解密的 RSA 辅助类。
	/// </summary>
	public class RSA
	{
		private readonly System.Security.Cryptography.RSA? _privateKeyRsaProvider;
		private readonly System.Security.Cryptography.RSA? _publicKeyRsaProvider;

		/// <summary>
		/// 初始化 <see cref="RSA"/> 类的新实例。
		/// </summary>
		/// <param name="privateKey">采用 PKCS#1 或类似 DER 格式的 Base64 编码私钥。如果为 null 或空，则无法使用私钥功能。</param>
		/// <param name="publicKey">Base64 编码的公钥（X.509 SubjectPublicKeyInfo）。如果为 null 或空，则无法使用公钥功能。</param>
		public RSA(string privateKey, string? publicKey = null)
		{
			if (!string.IsNullOrEmpty(privateKey))
			{
				_privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey);
			}

			if (!string.IsNullOrEmpty(publicKey))
			{
				_publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey);
			}
		}

		/// <summary>
		/// 使用配置的公钥加密提供的明文。
		/// </summary>
		/// <param name="source">要加密的明文。</param>
		/// <returns>Base64 编码的密文。</returns>
		/// <exception cref="Exception">未配置公钥提供程序时抛出。</exception>
		public string Encrypt(string source)
		{
			if (_publicKeyRsaProvider == null)
			{
				throw new Exception("_publicKeyRsaProvider is null");
			}

			return Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(source), RSAEncryptionPadding.Pkcs1));
		}

		/// <summary>
		/// 使用配置的私钥解密提供的 Base64 编码密文。
		/// </summary>
		/// <param name="source">Base64 编码的密文。</param>
		/// <returns>解密后的明文。</returns>
		/// <exception cref="Exception">未配置私钥提供程序时抛出。</exception>
		public string Decrypt(string source)
		{
			if (_privateKeyRsaProvider == null)
			{
				throw new Exception("_privateKeyRsaProvider is null");
			}

			return Encoding.UTF8.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(source), RSAEncryptionPadding.Pkcs1));
		}

		/// <summary>
		/// 从 Base64 编码的私钥（DER）创建 <see cref="RSA"/> 实例。
		/// </summary>
		/// <param name="key">Base64 编码的私钥字节（DER/PKCS#1）。</param>
		/// <returns>已初始化的 <see cref="System.Security.Cryptography.RSA"/> 实例。</returns>
		private static System.Security.Cryptography.RSA CreateRsaProviderFromPrivateKey(string key)
		{
			var privateKeyBits = Convert.FromBase64String(key);

			var rsa = System.Security.Cryptography.RSA.Create();
			var parameters = new RSAParameters();

			using (var reader = new BinaryReader(new MemoryStream(privateKeyBits)))
			{
				var twobytes = reader.ReadUInt16();
				switch (twobytes)
				{
					case 0x8130:
						reader.ReadByte();
						break;
					case 0x8230:
						reader.ReadInt16();
						break;
					default:
						throw new Exception("Unexpected value read reader.ReadUInt16()");
				}

				twobytes = reader.ReadUInt16();
				if (twobytes != 0x0102)
				{
					throw new Exception("Unexpected version");
				}

				var bt = reader.ReadByte();
				if (bt != 0x00)
				{
					throw new Exception("Unexpected value read reader.ReadByte()");
				}

				parameters.Modulus = reader.ReadBytes(GetIntegerSize(reader));
				parameters.Exponent = reader.ReadBytes(GetIntegerSize(reader));
				parameters.D = reader.ReadBytes(GetIntegerSize(reader));
				parameters.P = reader.ReadBytes(GetIntegerSize(reader));
				parameters.Q = reader.ReadBytes(GetIntegerSize(reader));
				parameters.DP = reader.ReadBytes(GetIntegerSize(reader));
				parameters.DQ = reader.ReadBytes(GetIntegerSize(reader));
				parameters.InverseQ = reader.ReadBytes(GetIntegerSize(reader));
			}

			rsa.ImportParameters(parameters);
			return rsa;
		}

		/// <summary>
		/// 从 Base64 编码的公钥（X.509/SPKI）创建 <see cref="RSA"/> 实例。
		/// </summary>
		/// <param name="key">Base64 编码的公钥字节（X.509 SubjectPublicKeyInfo）。</param>
		/// <returns>已初始化的 <see cref="System.Security.Cryptography.RSA"/> 实例；如果解析失败则返回 null。</returns>
		private static System.Security.Cryptography.RSA? CreateRsaProviderFromPublicKey(string key)
		{
			byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

			var x509Key = Convert.FromBase64String(key);

			using var mem = new MemoryStream(x509Key);
			using var reader = new BinaryReader(mem);
			var twobytes = reader.ReadUInt16();
			switch (twobytes)
			{
				case 0x8130:
					reader.ReadByte();
					break;
				case 0x8230:
					reader.ReadInt16();
					break;
				default:
					return null;
			}

			var seq = reader.ReadBytes(15);
			if (!CompareBytes(seq, seqOid)) //确保 OID 的 Sequence 正确
			{
				return null;
			}

			twobytes = reader.ReadUInt16();
			switch (twobytes)
			{
				//数据按小端顺序读取（Bit String 的实际数据顺序为 03 81）
				case 0x8103:
					reader.ReadByte(); //前进 1 个字节
					break;
				case 0x8203:
					reader.ReadInt16(); //前进 2 个字节
					break;
				default:
					return null;
			}

			var bt = reader.ReadByte();
			if (bt != 0x00) //期望下一个字节为 null
				return null;

			twobytes = reader.ReadUInt16();
			switch (twobytes)
			{
				//数据按小端顺序读取（Sequence 的实际数据顺序为 30 81）
				case 0x8130:
					reader.ReadByte(); //前进 1 个字节
					break;
				case 0x8230:
					reader.ReadInt16(); //前进 2 个字节
					break;
				default:
					return null;
			}

			twobytes = reader.ReadUInt16();
			byte lowByte;
			byte highByte = 0x00;

			switch (twobytes)
			{
				//数据按小端顺序读取（Integer 的实际数据顺序为 02 81）
				case 0x8102:
					lowByte = reader.ReadByte(); // 读取接下来的字节，即模数中的字节数
					break;
				case 0x8202:
					highByte = reader.ReadByte(); //前进 2 个字节
					lowByte = reader.ReadByte();
					break;
				default:
					return null;
			}

			byte[] value = { lowByte, highByte, 0x00, 0x00 }; //反转字节顺序，因为 ASN.1 密钥使用大端顺序
			var modulusSize = BitConverter.ToInt32(value, 0);

			int firstbyte = reader.PeekChar();
			if (firstbyte == 0x00)
			{
				//如果模数首字节（最高位）为零，则不包含它
				reader.ReadByte(); //跳过这个 null 字节
				modulusSize -= 1; //将模数缓冲区大小减 1
			}

			var modulus = reader.ReadBytes(modulusSize);

			if (reader.ReadByte() != 0x02)
			{
				return null;
			}

			int exponentCount = reader.ReadByte();
			var exponent = reader.ReadBytes(exponentCount);

			// ------- 创建 RSACryptoServiceProvider 实例并用公钥初始化 -----
			var rsa = System.Security.Cryptography.RSA.Create();
			var rsaKeyInfo = new RSAParameters
			{
				Modulus = modulus,
				Exponent = exponent
			};
			rsa.ImportParameters(rsaKeyInfo);

			return rsa;
		}

		/// <summary>
		/// 从当前 BinaryReader 位置读取 ASN.1 INTEGER 并返回该整数占用的字节数。
		/// </summary>
		/// <param name="reader">定位在 ASN.1 INTEGER 标记处的 BinaryReader。</param>
		/// <returns>组成该整数值的字节数。</returns>
		private static int GetIntegerSize(BinaryReader reader)
		{
			int count;
			var @byte = reader.ReadByte();
			if (@byte != 0x02)
			{
				return 0;
			}

			@byte = reader.ReadByte();

			switch (@byte)
			{
				case 0x81:
					count = reader.ReadByte();
					break;
				case 0x82:
					{
						var highByte = reader.ReadByte();
						var lowByte = reader.ReadByte();
						byte[] value = { lowByte, highByte, 0x00, 0x00 };
						count = BitConverter.ToInt32(value, 0);
						break;
					}
				default:
					count = @byte;
					break;
			}

			while (reader.ReadByte() == 0x00)
			{
				count -= 1;
			}

			reader.BaseStream.Seek(-1, SeekOrigin.Current);
			return count;
		}

		/// <summary>
		/// 比较两个字节数组是否相等。
		/// </summary>
		/// <param name="source">第一个字节数组。</param>
		/// <param name="target">第二个字节数组。</param>
		/// <returns>如果两个数组长度和内容都相同则返回 true，否则返回 false。</returns>
		private static bool CompareBytes(byte[] source, byte[] target)
		{
			if (source.Length != target.Length)
			{
				return false;
			}

			var i = 0;
			foreach (var @byte in source)
			{
				if (@byte != target[i])
				{
					return false;
				}

				i++;
			}

			return true;
		}
	}
}