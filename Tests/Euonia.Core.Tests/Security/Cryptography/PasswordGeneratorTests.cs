using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests.Security;

public class PasswordGeneratorTests
{
	[Fact]
	public void GeneratePassword_All_ContainsEveryCharacterType()
	{
		for (var index = 0; index < 20; index++)
		{
			var password = PasswordGenerator.GeneratePassword(PasswordComplexity.All, 16, 16);

			Assert.Equal(16, password.Length);
			Assert.Contains(password, character => char.IsLower(character));
			Assert.Contains(password, character => char.IsUpper(character));
			Assert.Contains(password, character => char.IsDigit(character));
			Assert.Contains(password, character => !char.IsLetterOrDigit(character));
		}
	}

	[Fact]
	public void GeneratePassword_None_ContainsOnlyLettersAndDigits()
	{
		var password = PasswordGenerator.GeneratePassword(PasswordComplexity.None, 8, 16);

		Assert.InRange(password.Length, 8, 16);
		Assert.All(password, character => Assert.True(char.IsLetterOrDigit(character)));
	}

	[Fact]
	public void GeneratePassword_LengthBelowRequiredSets_Throws()
	{
		Assert.Throws<ArgumentException>(() => PasswordGenerator.GeneratePassword(PasswordComplexity.All, 2, 2));
	}

	[Fact]
	public void GeneratePassword_ZeroLength_Throws()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassword(PasswordComplexity.None, 0, 8));
	}

	[Fact]
	public void GeneratePassword_MaxBelowMin_UsesMin()
	{
		var password = PasswordGenerator.GeneratePassword(PasswordComplexity.ContainsDigit, 12, 4);

		Assert.Equal(12, password.Length);
		Assert.Contains(password, character => char.IsDigit(character));
	}
}
