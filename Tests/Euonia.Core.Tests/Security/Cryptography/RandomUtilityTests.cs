using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class RandomUtilityTests
{
	[Fact]
	public void GenerateRandomString_ReturnsExactHexLength()
	{
		var value = RandomUtility.GenerateRandomString(31);

		Assert.Equal(31, value.Length);
		Assert.All(value, character => Assert.True(char.IsAsciiHexDigit(character)));
	}

	[Fact]
	public void GenerateRandomDigit_ReturnsDigits()
	{
		var value = RandomUtility.GenerateRandomDigit(6);

		Assert.Equal(6, value.Length);
		Assert.All(value, character => Assert.InRange(character, '0', '9'));
	}

	[Fact]
	public void GenerateRandomBytes_ReturnsRequestedLength()
	{
		Assert.Equal(32, RandomUtility.GenerateRandomBytes(32).Length);
	}

	[Fact]
	public void Next_Range_IsRespected()
	{
		var random = new RandomUtility();

		for (var index = 0; index < 100; index++)
			Assert.InRange(random.Next(5, 10), 5, 9);
	}

	[Fact]
	public void NextDouble_IsInUnitInterval()
	{
		var random = new RandomUtility();

		for (var index = 0; index < 100; index++)
			Assert.InRange(random.NextDouble(), 0.0, 1.0);
	}

	[Fact]
	public void GenerateRandomString_NonPositive_Throws()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => RandomUtility.GenerateRandomString(0));
	}
}
