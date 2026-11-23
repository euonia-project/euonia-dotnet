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
}
