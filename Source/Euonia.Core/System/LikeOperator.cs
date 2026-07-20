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

		if (patternSpan.Length == 1)
		{
			ref readonly char patternItem = ref patternSpan[0];
			if (patternItem == zeroOrMoreChars)
			{
				return true;
			}
		}

		if (contentSpan.Length == 1)
		{
			ref readonly var patternItem = ref patternSpan[0];
			if (patternItem == oneChar)
			{
				return true;
			}
		}

		var zeroOrMorePatternCount = 0;
		var onePatternCount = 0;
		foreach (var @char in patternSpan)
		{
			ref readonly char patternItem = ref @char;
			if (patternItem == zeroOrMoreChars)
			{
				zeroOrMorePatternCount++;
			}
			else if (patternItem == oneChar)
			{
				onePatternCount++;
			}
		}

		if (zeroOrMorePatternCount + onePatternCount == patternSpan.Length)
		{
			if (zeroOrMorePatternCount > 0)
			{
				return true;
			}

			if (patternSpan.Length == contentSpan.Length)
			{
				return true;
			}
		}

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
	/// </summary>
	/// <param name="contentSpan">要匹配的内容跨度。</param>
	/// <param name="patternSpan">模式跨度，包含通配符。</param>
	/// <param name="zeroOrMoreChars">表示零个或多个字符的通配符。</param>
	/// <param name="oneChar">表示单个字符的通配符。</param>
	/// <param name="equalsChar">用于比较字符的委托。</param>
	/// <returns>如果内容匹配模式，则返回 true；否则返回 false。</returns>
	private static bool LikeStringCore(ReadOnlySpan<char> contentSpan, ReadOnlySpan<char> patternSpan, in char zeroOrMoreChars, in char oneChar, EqualsCharDelegate equalsChar)
	{
		var contentIndex = 0;
		var patternIndex = 0;
		while (contentIndex < contentSpan.Length && patternIndex < patternSpan.Length)
		{
			ref readonly var patternItem = ref patternSpan[patternIndex];
			if (patternItem == zeroOrMoreChars)
			{
				while (true)
				{
					if (patternIndex < patternSpan.Length)
					{
						ref readonly char nextPatternItem = ref patternSpan[patternIndex];
						if (nextPatternItem == zeroOrMoreChars)
						{
							patternIndex++;
							continue;
						}
					}

					break;
				}

				if (patternIndex == patternSpan.Length)
				{
					return true;
				}

				while (contentIndex < contentSpan.Length)
				{
					if (LikeStringCore(contentSpan[contentIndex..], patternSpan[patternIndex..], in zeroOrMoreChars, in oneChar, equalsChar))
					{
						return true;
					}

					contentIndex++;
				}

				return false;
			}

			if (patternItem == oneChar)
			{
				contentIndex++;
				patternIndex++;
			}
			else
			{
				if (contentIndex >= contentSpan.Length)
				{
					return false;
				}

				ref readonly var contentItem = ref contentSpan[contentIndex];
				if (!equalsChar(in contentItem, in patternItem))
				{
					return false;
				}

				contentIndex++;
				patternIndex++;
			}
		}

		if (contentIndex == contentSpan.Length)
		{
			while (true)
			{
				if (patternIndex < patternSpan.Length)
				{
					ref readonly char nextPatternItem = ref patternSpan[patternIndex];
					if (nextPatternItem == zeroOrMoreChars)
					{
						patternIndex++;
						continue;
					}
				}

				break;
			}

			return patternIndex == patternSpan.Length;
		}

		return false;
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