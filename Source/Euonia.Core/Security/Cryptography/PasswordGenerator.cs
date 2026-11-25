using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 使用密码学安全随机数按指定复杂度生成密码。
/// </summary>
/// <remarks>
/// 生成的密码长度在 <c>minLength</c> 与 <c>maxLength</c> 之间（含两端）随机，
/// 每个要求的字符类型至少出现一次；字符顺序经过随机打乱。
/// 参数校验先于随机采样完成，同一组参数的行为是确定的：要么始终成功，要么始终抛出参数异常。
/// </remarks>
public static class PasswordGenerator
{
	private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
	private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	private const string DigitChars = "0123456789";
	private const string SymbolChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

	/// <summary>
	/// 按指定的复杂度与长度约束生成随机密码。
	/// </summary>
	/// <param name="complexity">要求包含的字符类型；为 <see cref="PasswordComplexity.None"/> 时使用字母与数字。</param>
	/// <param name="minLength">密码最小长度，必须大于 0；指定复杂度时不得小于要求的字符类型数量。</param>
	/// <param name="maxLength">密码最大长度；小于 <paramref name="minLength"/> 时按 <paramref name="minLength"/> 处理。</param>
	/// <returns>生成的随机密码。</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> 小于 1。</exception>
	/// <exception cref="ArgumentException">复杂度未指定任何字符类型，或 <paramref name="minLength"/> 小于要求的字符类型数量。</exception>
	public static string GeneratePassword(PasswordComplexity complexity, int minLength = 8, int maxLength = 16)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minLength);

		if (maxLength < minLength)
		{
			maxLength = minLength;
		}

		if (complexity == PasswordComplexity.None)
		{
			return GenerateSimplePassword(NextLength(minLength, maxLength));
		}

		var sets = new List<string>(4);

		if (complexity.HasFlag(PasswordComplexity.ContainsLowercase))
		{
			sets.Add(LowercaseChars);
		}

		if (complexity.HasFlag(PasswordComplexity.ContainsUppercase))
		{
			sets.Add(UppercaseChars);
		}

		if (complexity.HasFlag(PasswordComplexity.ContainsDigit))
		{
			sets.Add(DigitChars);
		}

		if (complexity.HasFlag(PasswordComplexity.ContainsSymbol))
		{
			sets.Add(SymbolChars);
		}

		if (sets.Count == 0)
		{
			throw new ArgumentException("复杂度未指定任何字符类型。", nameof(complexity));
		}

		// 校验必须先于随机取长度：否则 minLength 小于字符类型数量时会随采样结果时而成功、时而抛异常。
		if (minLength < sets.Count)
		{
			throw new ArgumentException($"密码最小长度不能小于要求的字符类型数量（{sets.Count}）。", nameof(minLength));
		}

		var length = NextLength(minLength, maxLength);
		var pool = sets.SelectMany(set => set).Distinct().ToArray();
		var characters = new char[length];
		var index = 0;

		foreach (var set in sets)
		{
			characters[index++] = set[RandomNumberGenerator.GetInt32(set.Length)];
		}

		while (index < length)
		{
			characters[index++] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
		}

		Shuffle(characters);

		return new string(characters);
	}

	private static int NextLength(int minLength, int maxLength)
	{
		// GetInt32 的上界是独占的；maxLength == int.MaxValue 时 +1 会溢出，
		// 改在 [minLength - 1, int.MaxValue) 采样后 +1，得到 [minLength, int.MaxValue]。
		return maxLength == int.MaxValue
			? RandomNumberGenerator.GetInt32(minLength - 1, int.MaxValue) + 1
			: RandomNumberGenerator.GetInt32(minLength, maxLength + 1);
	}

	private static string GenerateSimplePassword(int length)
	{
		const string chars = LowercaseChars + UppercaseChars + DigitChars;
		var characters = new char[length];

		for (var index = 0; index < length; index++)
		{
			characters[index] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
		}

		Shuffle(characters);

		return new string(characters);
	}

	private static void Shuffle(char[] characters)
	{
		for (var index = characters.Length - 1; index > 0; index--)
		{
			var target = RandomNumberGenerator.GetInt32(index + 1);
			(characters[index], characters[target]) = (characters[target], characters[index]);
		}
	}
}
