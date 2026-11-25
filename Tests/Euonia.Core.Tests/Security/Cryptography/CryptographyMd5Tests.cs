using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyMd5Tests
{
	[Fact]
	public void Encrypt_KnownVector_ReturnsLowercaseHex()
	{
		Assert.Equal("900150983cd24fb0d6963f7d28e17f72", Cryptography.MD5.Encrypt("abc"));
	}

	[Fact]
	public void Encrypt_EmptyString_ReturnsKnownDigest()
	{
		Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", Cryptography.MD5.Encrypt(string.Empty));
	}

	[Fact]
	public void Encrypt_Unicode_Returns32HexCharacters()
	{
		var digest = Cryptography.MD5.Encrypt("Euonia 中文");

		Assert.Equal(32, digest.Length);
		Assert.All(digest, character => Assert.True(char.IsAsciiHexDigit(character)));
	}

	[Fact]
	public void Encrypt_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => Cryptography.MD5.Encrypt(null!));
	}

	[Fact]
	public void Encrypt_Unicode_MatchesUtf8Digest()
	{
		// 契约：摘要必须按 UTF-8 字节计算，而不是系统默认编码。
		Assert.Equal("640a432eb04d9ceded1bc624548b2371", Cryptography.MD5.Encrypt("Euonia 中文"));
	}
}
