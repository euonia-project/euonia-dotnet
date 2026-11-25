using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class CryptographyBase64Tests
{
	[Fact]
	public void EncodeAndDecode_RoundTripsUtf8Text()
	{
		const string value = "Euonia 安全 42";

		var encoded = Cryptography.Base64.Encode(value);

		Assert.Equal(value, Cryptography.Base64.Decode(encoded));
	}

	[Fact]
	public void Encode_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => Cryptography.Base64.Encode(null!));
	}

	[Fact]
	public void Decode_Invalid_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() => Cryptography.Base64.Decode("***"));
	}
}
