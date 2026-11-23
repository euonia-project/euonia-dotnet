namespace Nerosoft.Euonia.Security;

/// <summary>
/// 指定生成密码时要求包含的字符类型。
/// </summary>
[Flags]
public enum PasswordComplexity
{
	/// <summary>
	/// 无特定复杂度要求，使用字母与数字。
	/// </summary>
	None = 0,

	/// <summary>
	/// 至少包含一个小写字母（a-z）。
	/// </summary>
	ContainsLowercase = 1,

	/// <summary>
	/// 至少包含一个大写字母（A-Z）。
	/// </summary>
	ContainsUppercase = 2,

	/// <summary>
	/// 至少包含一个数字（0-9）。
	/// </summary>
	ContainsDigit = 4,

	/// <summary>
	/// 至少包含一个符号（!@#$%^&amp;* 等）。
	/// </summary>
	ContainsSymbol = 8,

	/// <summary>
	/// 同时包含小写、大写、数字与符号。
	/// </summary>
	All = ContainsLowercase | ContainsUppercase | ContainsDigit | ContainsSymbol
}
