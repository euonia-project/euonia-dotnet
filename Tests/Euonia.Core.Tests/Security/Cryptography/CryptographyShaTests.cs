using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyShaTests
{
	[Fact]
	public void Encrypt_KnownVector_ReturnsExpectedBase64()
	{
		// SHA-256("abc") 的 Base64 表示。
		Assert.Equal("ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", Cryptography.SHA.Encrypt("abc"));
	}

	[Fact]
	public void Encrypt_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => Cryptography.SHA.Encrypt(null!));
	}
}
