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
	public void EncryptAndDecrypt_WithDefaultKey_RoundTrips()
	{
		var ciphertext = Cryptography.DES.Encrypt("default-key");

		Assert.Equal("default-key", Cryptography.DES.Decrypt(ciphertext));
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
