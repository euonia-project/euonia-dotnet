using System.Security.Cryptography;
using RsaHelper = Nerosoft.Euonia.Security.Cryptography.RSA;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyRsaTests
{
	[Fact]
	public void EncryptAndDecrypt_RoundTripsWithPkcs1Keys()
	{
		using var rsa = RSA.Create(2048);

		var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
		var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

		using var helper = new RsaHelper(privateKey, publicKey);

		var ciphertext = helper.Encrypt("Euonia 世界");

		Assert.Equal("Euonia 世界", helper.Decrypt(ciphertext));
	}

	[Fact]
	public void Constructor_PemKeys_Work()
	{
		using var rsa = RSA.Create(2048);

		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());

		var ciphertext = helper.Encrypt("pem");

		Assert.Equal("pem", helper.Decrypt(ciphertext));
	}

	[Fact]
	public void Constructor_NoKeys_Throws()
	{
		Assert.Throws<ArgumentException>(() => new RsaHelper(null));
	}

	[Fact]
	public void Constructor_InvalidKey_ThrowsCryptographicException()
	{
		Assert.Throws<CryptographicException>(() => new RsaHelper("not-a-key"));
	}

	[Fact]
	public void EncryptAndDecrypt_WithOnlyPrivateKey_UsesPrivateKeyAsPublicKey()
	{
		using var rsa = RSA.Create(2048);
		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem());

		var ciphertext = helper.Encrypt("value");

		Assert.Equal("value", helper.Decrypt(ciphertext));
	}

	[Fact]
	public void Encrypt_DefaultPadding_UsesOaepSha256()
	{
		// 回归：修复前默认使用 PKCS#1 v1.5，OAEP-SHA256 无法解密默认密文。
		using var rsa = RSA.Create(2048);
		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());

		var ciphertext = helper.Encrypt("oaep");
		var bytes = Convert.FromBase64String(ciphertext);

		Assert.Equal("oaep", Encoding.UTF8.GetString(rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA256)));
		Assert.ThrowsAny<CryptographicException>(() => rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1));
	}

	[Fact]
	public void Decrypt_DefaultPadding_ReadsOaepSha256Ciphertext()
	{
		using var rsa = RSA.Create(2048);
		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem());

		var ciphertext = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes("external"), RSAEncryptionPadding.OaepSHA256));

		Assert.Equal("external", helper.Decrypt(ciphertext));
	}

	[Fact]
	public void EncryptAndDecrypt_ExplicitPkcs1_RoundTrips()
	{
		using var rsa = RSA.Create(2048);
		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem());

		var ciphertext = helper.Encrypt("legacy", RSAEncryptionPadding.Pkcs1);

		Assert.Equal("legacy", helper.Decrypt(ciphertext, RSAEncryptionPadding.Pkcs1));
		// 默认 OAEP-SHA256 解密不能读取 PKCS#1 v1.5 密文。
		Assert.ThrowsAny<CryptographicException>(() => helper.Decrypt(ciphertext));
	}

	[Fact]
	public void Encrypt_NullPadding_Throws()
	{
		using var rsa = RSA.Create(2048);
		using var helper = new RsaHelper(rsa.ExportRSAPrivateKeyPem());

		Assert.Throws<ArgumentNullException>(() => helper.Encrypt("value", null!));
		Assert.Throws<ArgumentNullException>(() => helper.Decrypt("AAAA", null!));
	}

	[Fact]
	public void Constructor_ValidPrivateKey_InvalidPublicKey_ThrowsCryptographicException()
	{
		using var rsa = RSA.Create(2048);
		var privateKey = rsa.ExportRSAPrivateKeyPem();

		// 公钥导入失败时必须抛出 CryptographicException（构造内部同时释放已导入的私钥）。
		var exception = Assert.Throws<CryptographicException>(() => new RsaHelper(privateKey, "not-a-key"));

		Assert.Contains("公钥", exception.Message);
	}
}
