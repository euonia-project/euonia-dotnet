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

	[Fact]
	public void GeneratePassword_All_MinLengthAtLeastSets_AlwaysSucceeds()
	{
		for (var index = 0; index < 500; index++)
		{
			var password = PasswordGenerator.GeneratePassword(PasswordComplexity.All, 4, 16);

			Assert.InRange(password.Length, 4, 16);
			Assert.Contains(password, character => char.IsLower(character));
			Assert.Contains(password, character => char.IsUpper(character));
			Assert.Contains(password, character => char.IsDigit(character));
			Assert.Contains(password, character => !char.IsLetterOrDigit(character));
		}
	}

	[Fact]
	public void GeneratePassword_All_MinBelowSets_ThrowsDeterministically()
	{
		// 回归：修复前先随机取长度再校验，(All, 2, 16) 约 86% 概率成功、14% 概率抛异常；
		// 修复后校验前置，同一组参数必须每次都抛出 ArgumentException。
		for (var index = 0; index < 500; index++)
		{
			Assert.Throws<ArgumentException>(() => PasswordGenerator.GeneratePassword(PasswordComplexity.All, 2, 16));
		}
	}

	[Fact]
	public void GeneratePassword_MaxIntMaxValue_ValidatesBeforeSampling()
	{
		// 回归：修复前 maxLength + 1 溢出导致 ArgumentOutOfRangeException；
		// 修复后校验先于采样，确定性抛出 ArgumentException，不会触发溢出采样。
		Assert.Throws<ArgumentException>(() => PasswordGenerator.GeneratePassword(PasswordComplexity.All, 2, int.MaxValue));
	}
}
