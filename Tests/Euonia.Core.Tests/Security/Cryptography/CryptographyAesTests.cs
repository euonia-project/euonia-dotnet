using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyAesTests
{
	private const string Key = "0123456789abcdef0123456789abcdef";

	[Fact]
	public void EncryptAndDecrypt_RoundTripsUtf8Text()
	{
		const string plaintext = "密码 Password 123";

		var ciphertext = Cryptography.AES.Encrypt(plaintext, Key);

		Assert.NotNull(ciphertext);
		Assert.NotEqual(plaintext, ciphertext);
		Assert.Equal(plaintext, Cryptography.AES.Decrypt(ciphertext, Key));
	}

	[Fact]
	public void Encrypt_EmptyInput_ReturnsNull()
	{
		Assert.Null(Cryptography.AES.Encrypt(string.Empty, Key));
		Assert.Null(Cryptography.AES.Encrypt(null, Key));
	}

	[Fact]
	public void Encrypt_InvalidKeyLength_Throws()
	{
		Assert.Throws<ArgumentException>(() => Cryptography.AES.Encrypt("value", "short-key"));
	}

	[Fact]
	public void Decrypt_InvalidBase64_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => Cryptography.AES.Decrypt("not-base64", Key));
	}
}
