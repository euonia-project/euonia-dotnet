using System.Security.Cryptography;
using System.Text;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyDesTests
{
	private const string AesKey = "0123456789abcdef0123456789abcdef";
	private const string DesKey = "12345678";

	[Fact]
	public void EncryptAndDecrypt_WithByteKey_RoundTrips()
	{
		var key = Encoding.UTF8.GetBytes(AesKey);
		var ciphertext = Cryptography.DES.Encrypt("DES 测试 123", key);

		Assert.Equal("DES 测试 123", Cryptography.DES.Decrypt(ciphertext, key));
	}

	[Fact]
	public void Encrypt_WithInvalidByteKeyLength_ThrowsArgumentException()
	{
		// 回归：修复前 aes.Key 赋值抛 CryptographicException，与文档承诺的 ArgumentException 不符。
		Assert.Throws<ArgumentException>(() => Cryptography.DES.Encrypt("value", new byte[5]));
		Assert.Throws<ArgumentException>(() => Cryptography.DES.Encrypt("value", new byte[17]));
	}

	[Fact]
	public void Decrypt_WithInvalidByteKeyLength_ThrowsArgumentException()
	{
		Assert.Throws<ArgumentException>(() => Cryptography.DES.Decrypt("AAAA", new byte[5]));
		Assert.Throws<ArgumentException>(() => Cryptography.DES.Decrypt("AAAA", new byte[17]));
	}

	[Fact]
	public void Encrypt_WithStringKey_MatchesLegacyEcbDes()
	{
		// 兼容契约：字符串密钥重载必须与遗留 DES（ECB + PKCS7，不使用 IV）的密文保持一致。
		const string plaintext = "DES 测试 123";
		var ciphertext = Cryptography.DES.Encrypt(plaintext, DesKey);

#pragma warning disable SYSLIB0021 // 测试中显式构造遗留 DES 作为参照。
		using var provider = System.Security.Cryptography.DES.Create();
#pragma warning restore SYSLIB0021
		provider.Mode = CipherMode.ECB;
		provider.Padding = PaddingMode.PKCS7;

		var sourceBytes = Encoding.UTF8.GetBytes(plaintext);
		using var encryptor = provider.CreateEncryptor(Encoding.UTF8.GetBytes(DesKey), null);
		var expected = Convert.ToBase64String(encryptor.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length));

		Assert.Equal(expected, ciphertext);
	}

	[Fact]
	public void Encrypt_WithStringKey_ProducesBase64()
	{
		var ciphertext = Cryptography.DES.Encrypt("value", DesKey);

		Assert.NotEmpty(ciphertext);
		Assert.NotEmpty(Convert.FromBase64String(ciphertext));
	}

	[Fact]
	public void Encrypt_WithInvalidStringKeyLength_Throws()
	{
		Assert.ThrowsAny<ArgumentException>(() => Cryptography.DES.Encrypt("value", "short"));
	}

	[Fact]
	public void Encrypt_NullSource_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => Cryptography.DES.Encrypt(null!, DesKey));
	}

	[Fact]
	public void Decrypt_InvalidBase64_Throws()
	{
		Assert.Throws<FormatException>(() => Cryptography.DES.Decrypt("not-base64", Encoding.UTF8.GetBytes(AesKey)));
	}
}
