namespace System;

/// <summary>
/// 文本修剪类型。
/// </summary>
public enum TextTrimType
{
	/// <summary>
	/// 不修剪。
	/// </summary>
	None,

	/// <summary>
	/// 修剪文本开头。
	/// </summary>
	Head,

	/// <summary>
	/// 修剪文本末尾。
	/// </summary>
	Tail,

	/// <summary>
	/// 修剪文本开头和末尾。
	/// </summary>
	Both,

	/// <summary>
	/// 移除所有空白字符。
	/// </summary>
	All,
}