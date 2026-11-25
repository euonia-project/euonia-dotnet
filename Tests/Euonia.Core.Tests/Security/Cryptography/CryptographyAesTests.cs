using System.Security.Cryptography;
using System.Text;
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

	[Fact]
	public void EncryptGcmAndDecryptGcm_RoundTripsUtf8Text()
	{
		const string plaintext = "密码 Password 123";

		var ciphertext = Cryptography.AES.EncryptGcm(plaintext, Key);

		Assert.NotNull(ciphertext);
		Assert.NotEqual(plaintext, ciphertext);
		Assert.Equal(plaintext, Cryptography.AES.DecryptGcm(ciphertext, Key));
	}

	[Fact]
	public void EncryptGcmAndDecryptGcm_WithByteKey_RoundTrips()
	{
		var key = Encoding.UTF8.GetBytes(Key);

		var ciphertext = Cryptography.AES.EncryptGcm("byte-key", key);

		Assert.Equal("byte-key", Cryptography.AES.DecryptGcm(ciphertext, key));
	}

	[Fact]
	public void EncryptGcm_SamePlaintext_ProducesDifferentCiphertext()
	{
		var first = Cryptography.AES.EncryptGcm("same", Key);
		var second = Cryptography.AES.EncryptGcm("same", Key);

		Assert.NotEqual(first, second);
	}

	[Fact]
	public void EncryptGcm_Output_ContainsNonceTagAndCiphertext()
	{
		const string plaintext = "format";

		var payload = Convert.FromBase64String(Cryptography.AES.EncryptGcm(plaintext, Key)!);

		// 输出格式：nonce(12 字节) + 认证标签(16 字节) + 密文（与 UTF-8 明文等长）。
		Assert.Equal(12 + 16 + Encoding.UTF8.GetByteCount(plaintext), payload.Length);
	}

	[Fact]
	public void DecryptGcm_TamperedCiphertext_ThrowsCryptographicException()
	{
		var payload = Convert.FromBase64String(Cryptography.AES.EncryptGcm("tamper", Key)!);
		payload[^1] ^= 0xFF;

		Assert.ThrowsAny<CryptographicException>(() => Cryptography.AES.DecryptGcm(Convert.ToBase64String(payload), Key));
	}

	[Fact]
	public void DecryptGcm_TamperedTag_ThrowsCryptographicException()
	{
		var payload = Convert.FromBase64String(Cryptography.AES.EncryptGcm("tamper", Key)!);
		payload[12] ^= 0xFF;

		Assert.ThrowsAny<CryptographicException>(() => Cryptography.AES.DecryptGcm(Convert.ToBase64String(payload), Key));
	}

	[Fact]
	public void DecryptGcm_TamperedNonce_ThrowsCryptographicException()
	{
		var payload = Convert.FromBase64String(Cryptography.AES.EncryptGcm("tamper", Key)!);
		payload[0] ^= 0xFF;

		Assert.ThrowsAny<CryptographicException>(() => Cryptography.AES.DecryptGcm(Convert.ToBase64String(payload), Key));
	}

	[Fact]
	public void DecryptGcm_WrongKey_ThrowsCryptographicException()
	{
		var ciphertext = Cryptography.AES.EncryptGcm("secret", Key);

		Assert.ThrowsAny<CryptographicException>(() => Cryptography.AES.DecryptGcm(ciphertext, "ffffffffffffffffffffffffffffffff"));
	}

	[Fact]
	public void DecryptGcm_TruncatedPayload_ThrowsCryptographicException()
	{
		var truncated = Convert.ToBase64String(new byte[12 + 16 - 1]);

		Assert.Throws<CryptographicException>(() => Cryptography.AES.DecryptGcm(truncated, Key));
	}

	[Fact]
	public void EncryptGcm_EmptyInput_ReturnsNull()
	{
		Assert.Null(Cryptography.AES.EncryptGcm(string.Empty, Key));
		Assert.Null(Cryptography.AES.EncryptGcm(null, Key));
		Assert.Null(Cryptography.AES.DecryptGcm(string.Empty, Key));
		Assert.Null(Cryptography.AES.DecryptGcm(null, Key));
	}

	[Fact]
	public void EncryptGcm_InvalidKeyLength_Throws()
	{
		Assert.Throws<ArgumentException>(() => Cryptography.AES.EncryptGcm("value", "short-key"));
		Assert.Throws<ArgumentException>(() => Cryptography.AES.DecryptGcm("value", "short-key"));
	}

	[Fact]
	public void DecryptGcm_InvalidBase64_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => Cryptography.AES.DecryptGcm("not-base64", Key));
	}
}
