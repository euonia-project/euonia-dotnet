#nullable enable
namespace System;

/// <summary>
/// LikeOperator 类用于使用 * 和 ? 通配符比较两个字符串。
/// </summary>
public class LikeOperator
{
	/// <summary>
	/// 使用 * 和 ? 通配符比较两个字符串。
	/// </summary>
	/// <param name="content">要搜索的内容字符串。</param>
	/// <param name="pattern">包含通配符的模式字符串。</param>
	/// <param name="ignoreCase">是否忽略大小写。</param>
	/// <param name="useInvariantCulture">是否使用固定区域性进行比较。</param>
	/// <returns>如果内容匹配模式，则为 true；否则为 false。</returns>
	public static bool LikeString(string? content, string? pattern, bool ignoreCase = true, bool useInvariantCulture = true)
	{
		if (content == null && pattern == null)
			return true;
		if (content == null || pattern == null)
			return false;

		var patternSpan = pattern.AsSpan();
		var contentSpan = content.AsSpan();

		return LikeString(contentSpan, patternSpan, ignoreCase, useInvariantCulture);
	}

	/// <summary>
	/// 使用 * 和 ? 通配符比较两个字符跨度。
	/// </summary>
	public static bool LikeString(ReadOnlySpan<char> contentSpan, ReadOnlySpan<char> patternSpan, bool ignoreCase = true, bool useInvariantCulture = true)
	{
		var zeroOrMoreChars = '*';
		var oneChar = '?';

		EqualsCharDelegate equalsChar;
		if (ignoreCase)
		{
			if (useInvariantCulture)
			{
				equalsChar = EqualsCharInvariantCultureIgnoreCase;
			}
			else
			{
				equalsChar = EqualsCharCurrentCultureIgnoreCase;
			}
		}
		else
		{
			equalsChar = EqualsChar;
		}

		return LikeStringCore(contentSpan, patternSpan, in zeroOrMoreChars, in oneChar, equalsChar);
	}

	/// <summary>
	/// 使用 * 和 ? 通配符比较两个字符跨度的核心实现。
	/// 采用带回溯指针的贪心双指针算法，避免指数级递归。
	/// </summary>
	/// <param name="contentSpan">要匹配的内容跨度。</param>
	/// <param name="patternSpan">模式跨度，包含通配符。</param>
	/// <param name="zeroOrMoreChars">表示零个或多个字符的通配符。</param>
	/// <param name="oneChar">表示单个字符的通配符。</param>
	/// <param name="equalsChar">用于比较字符的委托。</param>
	/// <returns>如果内容匹配模式，则返回 true；否则返回 false。</returns>
	private static bool LikeStringCore(ReadOnlySpan<char> contentSpan, ReadOnlySpan<char> patternSpan, in char zeroOrMoreChars, in char oneChar, EqualsCharDelegate equalsChar)
	{
		var contentLength = contentSpan.Length;
		var patternLength = patternSpan.Length;

		var contentIndex = 0;
		var patternIndex = 0;
		var starIndex = -1;
		var starMatchContentIndex = 0;

		while (contentIndex < contentLength)
		{
			if (patternIndex < patternLength && (patternSpan[patternIndex] == oneChar || equalsChar(in contentSpan[contentIndex], in patternSpan[patternIndex])))
			{
				contentIndex++;
				patternIndex++;
			}
			else if (patternIndex < patternLength && patternSpan[patternIndex] == zeroOrMoreChars)
			{
				starIndex = patternIndex;
				starMatchContentIndex = contentIndex;
				patternIndex++;
			}
			else if (starIndex != -1)
			{
				patternIndex = starIndex + 1;
				starMatchContentIndex++;
				contentIndex = starMatchContentIndex;
			}
			else
			{
				return false;
			}
		}

		while (patternIndex < patternLength && patternSpan[patternIndex] == zeroOrMoreChars)
		{
			patternIndex++;
		}

		return patternIndex == patternLength;
	}

	/// <summary>
	/// 比较两个字符是否相等。
	/// </summary>
	/// <param name="contentItem">要比较的内容字符。</param>
	/// <param name="patternItem">要比较的模式字符。</param>
	/// <returns>如果字符相等，则返回 true；否则返回 false。</returns>
	private static bool EqualsChar(in char contentItem, in char patternItem)
	{
		return contentItem == patternItem;
	}

	/// <summary>
	/// 比较两个字符是否在当前区域性下忽略大小写相等。
	/// </summary>
	/// <param name="contentItem">要比较的内容字符。</param>
	/// <param name="patternItem">要比较的模式字符。</param>
	/// <returns>如果字符在当前区域性下忽略大小写相等，则返回 true；否则返回 false。</returns>
	private static bool EqualsCharCurrentCultureIgnoreCase(in char contentItem, in char patternItem)
	{
		return char.ToUpper(contentItem) == char.ToUpper(patternItem);
	}

	/// <summary>
	/// 比较两个字符是否在不变区域性下忽略大小写相等。
	/// </summary>
	/// <param name="contentItem">要比较的内容字符。</param>
	/// <param name="patternItem">要比较的模式字符。</param>
	/// <returns>如果字符在不变区域性下忽略大小写相等，则返回 true；否则返回 false。</returns>
	private static bool EqualsCharInvariantCultureIgnoreCase(in char contentItem, in char patternItem)
	{
		return char.ToUpperInvariant(contentItem) == char.ToUpperInvariant(patternItem);
	}

	/// <summary>
	/// 用于比较字符的委托类型。
	/// </summary>
	/// <param name="contentItem">要比较的内容字符。</param>
	/// <param name="patternItem">要比较的模式字符。</param>
	/// <returns>如果字符相等，则返回 true；否则返回 false。</returns>
	private delegate bool EqualsCharDelegate(in char contentItem, in char patternItem);
}