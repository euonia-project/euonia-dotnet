using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

// ReSharper disable MemberCanBePrivate.Global

public static partial class Extensions
{
	/// <summary>
	/// 匹配电话号码的正则表达式。
	/// </summary>
	internal const string PhoneNumberRegex = @"^[+]?(\d{1,3})?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$";

	/// <summary>
	/// 匹配仅包含字母的字符串的正则表达式。
	/// </summary>
	internal const string CharactersRegex = "^[A-Za-z]+$";

	/// <summary>
	/// 匹配电子邮件地址的正则表达式。
	/// </summary>
	/// <remarks>来自 https://emailregex.com 的通用电子邮件正则表达式（RFC 5322 官方标准）。</remarks>
	internal const string EmailRegex = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";

	/// <summary>
	/// 用于移除 HTML 标签的正则表达式。
	/// </summary>
	private const string REMOVE_HTML_TAGS_REGEX = """(?></?\w+)(?>(?:[^>'"]+|'[^']*'|"[^"]*")*)>""";

	/// <summary>
	/// 用于移除 HTML 注释的正则表达式。
	/// </summary>
	private static readonly Regex _removeHtmlCommentsRegex = new("<!--.*?-->", RegexOptions.Singleline);

	/// <summary>
	/// 用于移除 HTML 脚本的正则表达式。
	/// </summary>
	private static readonly Regex _removeHtmlScriptsRegex = new(@"(?s)<script.*?(/>|</script>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

	/// <summary>
	/// 用于移除 HTML 样式的正则表达式。
	/// </summary>
	private static readonly Regex _removeHtmlStylesRegex = new(@"(?s)<style.*?(/>|</style>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

	/// <param name="source">要搜索的源字符串</param>
	extension(string source)
	{
		/// <summary>
		/// 如果字符串不以指定字符结尾，则将该字符添加到末尾。
		/// </summary>
		/// <param name="c">要添加的字符。</param>
		/// <param name="comparisonType">用于比较的字符串比较类型。</param>
		public string EnsureEndsWith(char c, StringComparison comparisonType = StringComparison.Ordinal)
		{
			Check.EnsureNotNull(source, nameof(source));

			if (source.EndsWith(c.ToString(), comparisonType))
			{
				return source;
			}

			return source + c;
		}

		/// <summary>
		/// 如果字符串不以指定字符开头，则将该字符添加到开头。
		/// </summary>
		/// <param name="c">要添加的字符。</param>
		/// <param name="comparisonType">用于比较的字符串比较类型。</param>
		public string EnsureStartsWith(char c, StringComparison comparisonType = StringComparison.Ordinal)
		{
			Check.EnsureNotNull(source, nameof(source));

			if (source.StartsWith(c.ToString(), comparisonType))
			{
				return source;
			}

			return c + source;
		}

		/// <summary>
		/// 指示此字符串是否为 null 或空字符串。
		/// </summary>
		/// <returns>true 如果字符串为 null 或空字符串；否则为 false。</returns>
		public bool IsNullOrEmpty()
		{
			return string.IsNullOrEmpty(source);
		}

		/// <summary>
		/// 指示此字符串是否为 null、空或仅由空白字符组成。
		/// </summary>
		/// <returns>true 如果字符串为 null、空或仅由空白字符组成；否则为 false。</returns>
		public bool IsNullOrWhiteSpace()
		{
			return string.IsNullOrWhiteSpace(source);
		}

		/// <summary>
		/// 从字符串开头获取指定长度的子字符串。
		/// </summary>
		/// <param name="length">要获取的子字符串的长度。</param>
		/// <returns>从字符串开头获取的子字符串。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 null 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="length"/> 大于字符串长度时抛出。</exception>
		public string Left(int length)
		{
			Check.EnsureNotNull(source, nameof(source));

			if (source.Length < length)
			{
				throw new ArgumentException("length argument can not be bigger than given string's length!");
			}

			return source[..length];
		}

		/// <summary>
		/// 从字符串末尾获取指定长度的子字符串。
		/// </summary>
		/// <param name="length">要获取的子字符串的长度。</param>
		/// <returns>从字符串末尾获取的子字符串。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 null 时抛出。</exception>
		/// <exception cref="ArgumentException">当 <paramref name="length"/> 大于字符串长度时抛出。</exception>
		public string Right(int length)
		{
			Check.EnsureNotNull(source, nameof(source));

			if (source.Length < length)
			{
				throw new ArgumentException("length argument can not be bigger than given string's length!");
			}

			return source.Substring(source.Length - length, length);
		}

		/// <summary>
		/// 将字符串中的换行符转换为 <see cref="Environment.NewLine"/>。
		/// </summary>
		/// <returns>转换后的字符串。</returns>
		public string NormalizeLineEndings()
		{
			return source.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
		}

		/// <summary>
		/// 获取字符串中指定字符第 n 次出现的索引。
		/// </summary>
		/// <param name="c">要在源字符串中搜索的字符。</param>
		/// <param name="n">出现的次数。</param>
		public int NthIndexOf(char c, int n)
		{
			Check.EnsureNotNull(source, nameof(source));

			var count = 0;
			for (var i = 0; i < source.Length; i++)
			{
				if (source[i] != c)
				{
					continue;
				}

				if ((++count) == n)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// 从字符串末尾移除第一个匹配的后缀。
		/// </summary>
		/// <param name="postFixes">要移除的后缀数组。</param>
		/// <returns>移除后缀后的字符串。</returns>
		public string RemovePostFix(params string[] postFixes)
		{
			return source.RemovePostFix(StringComparison.Ordinal, postFixes);
		}

		/// <summary>
		/// 从字符串末尾移除第一个匹配的后缀。
		/// </summary>
		/// <param name="comparisonType">用于比较的字符串比较类型。</param>
		/// <param name="postFixes">要移除的后缀数组。</param>
		/// <returns>移除后缀后的字符串。</returns>
		public string RemovePostFix(StringComparison comparisonType, params string[] postFixes)
		{
			if (source.IsNullOrEmpty())
			{
				return null;
			}

			if (postFixes.IsNullOrEmpty())
			{
				return source;
			}

			foreach (var postFix in postFixes)
			{
				if (source.EndsWith(postFix, comparisonType))
				{
					return source.Left(source.Length - postFix.Length);
				}
			}

			return source;
		}

		/// <summary>
		/// 从字符串开头移除第一个匹配的前缀。
		/// </summary>
		/// <param name="preFixes">要移除的前缀数组。</param>
		/// <returns>移除前缀后的字符串。</returns>
		public string RemovePreFix(params string[] preFixes)
		{
			return source.RemovePreFix(StringComparison.Ordinal, preFixes);
		}

		/// <summary>
		/// 从字符串开头移除第一个匹配的前缀。
		/// </summary>
		/// <param name="comparisonType">用于比较的字符串比较类型。</param>
		/// <param name="preFixes">要移除的前缀数组。</param>
		/// <returns>移除前缀后的字符串。</returns>
		public string RemovePreFix(StringComparison comparisonType, params string[] preFixes)
		{
			if (source.IsNullOrEmpty())
			{
				return null;
			}

			if (preFixes.IsNullOrEmpty())
			{
				return source;
			}

			foreach (var preFix in preFixes)
			{
				if (source.StartsWith(preFix, comparisonType))
				{
					return source.Right(source.Length - preFix.Length);
				}
			}

			return source;
		}

		/// <summary>
		/// 替换字符串中第一个匹配的搜索字符串。
		/// </summary>
		public string ReplaceFirst(string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
		{
			Check.EnsureNotNull(source, nameof(source));

			var pos = source.IndexOf(search, comparisonType);
			if (pos < 0)
			{
				return source;
			}

			return source[..pos] + replace + source[(pos + search.Length)..];
		}

		/// <summary>
		/// 使用指定分隔符分割字符串。
		/// </summary>
		/// <param name="separator">用于分割的字符串。</param>
		/// <returns>分割后的字符串数组。</returns>
		public string[] Split(string separator)
		{
			return source.Split([separator], StringSplitOptions.None);
		}

		/// <summary>
		/// 使用指定分隔符和选项分割字符串。
		/// </summary>
		/// <param name="separator">用于分割的字符串。</param>
		/// <param name="options">指定是否返回空字符串的选项。</param>
		/// <returns>分割后的字符串数组。</returns>
		public string[] Split(string separator, StringSplitOptions options)
		{
			return source.Split([separator], options);
		}

		/// <summary>
		/// 使用 <see cref="Environment.NewLine"/> 分割字符串为行。
		/// </summary>
		/// <returns>分割后的字符串数组。</returns>
		public string[] SplitToLines()
		{
			return source.Split(Environment.NewLine);
		}

		/// <summary>
		/// 使用 <see cref="Environment.NewLine"/> 和指定选项分割字符串为行。
		/// </summary>
		/// <param name="options">指定是否返回空字符串的选项。</param>
		/// <returns>分割后的字符串数组。</returns>
		public string[] SplitToLines(StringSplitOptions options)
		{
			return source.Split(Environment.NewLine, options);
		}

		/// <summary>
		/// 将 PascalCase 字符串转换为 camelCase 字符串。
		/// </summary>
		/// <param name="useCurrentCulture">是否使用当前文化进行大小写转换。</param>
		/// <returns>转换后的 camelCase 字符串。</returns>
		public string ToCamelCase(bool useCurrentCulture = false)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return source;
			}

			if (source.Length == 1)
			{
				return useCurrentCulture ? source.ToLower() : source.ToLowerInvariant();
			}

			return (useCurrentCulture ? char.ToLower(source[0]) : char.ToLowerInvariant(source[0])) + source[1..];
		}

		/// <summary>
		/// 将 PascalCase/camelCase 字符串转换为句子格式（通过空格分隔单词）。
		/// 例如："ThisIsSampleSentence" 转换为 "This is a sample sentence"。
		/// </summary>
		/// <param name="useCurrentCulture">是否使用当前文化进行大小写转换。</param>
		/// <returns>转换后的句子格式字符串。</returns>
		public string ToSentenceCase(bool useCurrentCulture = false)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return source;
			}

			return useCurrentCulture
				? Regex.Replace(source, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
				: Regex.Replace(source, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
		}

		/// <summary>
		/// 将 PascalCase/camelCase 字符串转换为 kebab-case 格式。
		/// </summary>
		/// <param name="useCurrentCulture">是否使用当前文化进行大小写转换。</param>
		/// <returns>转换后的 kebab-case 字符串。</returns>
		public string ToKebabCase(bool useCurrentCulture = false)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return source;
			}

			source = source.ToCamelCase();

			return useCurrentCulture
				? Regex.Replace(source, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLower(m.Value[1]))
				: Regex.Replace(source, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
		}

		/// <summary>
		/// 将 PascalCase/camelCase 字符串转换为 snake_case 格式。
		/// 例如："ThisIsSampleSentence" 转换为 "this_is_a_sample_sentence"。
		/// </summary>
		/// <returns>转换后的 snake_case 字符串。</returns>
		public string ToSnakeCase()
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return source;
			}

			var builder = new StringBuilder(source.Length + Math.Min(2, source.Length / 5));
			var previousCategory = default(UnicodeCategory?);

			for (var currentIndex = 0; currentIndex < source.Length; currentIndex++)
			{
				var currentChar = source[currentIndex];
				if (currentChar == '_')
				{
					builder.Append('_');
					previousCategory = null;
					continue;
				}

				var currentCategory = char.GetUnicodeCategory(currentChar);
				switch (currentCategory)
				{
					case UnicodeCategory.UppercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
						if (previousCategory == UnicodeCategory.SpaceSeparator ||
						    previousCategory == UnicodeCategory.LowercaseLetter ||
						    previousCategory != UnicodeCategory.DecimalDigitNumber &&
						    previousCategory != null &&
						    currentIndex > 0 &&
						    currentIndex + 1 < source.Length &&
						    char.IsLower(source[currentIndex + 1]))
						{
							builder.Append('_');
						}

						currentChar = char.ToLower(currentChar);
						break;

					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.DecimalDigitNumber:
						if (previousCategory == UnicodeCategory.SpaceSeparator)
						{
							builder.Append('_');
						}

						break;

					default:
						if (previousCategory != null)
						{
							previousCategory = UnicodeCategory.SpaceSeparator;
						}

						continue;
				}

				builder.Append(currentChar);
				previousCategory = currentCategory;
			}

			return builder.ToString();
		}

		/// <summary>
		/// 将字符串转换为枚举值。
		/// </summary>
		/// <typeparam name="T">枚举类型</typeparam>
		/// <returns>枚举对象。</returns>
		public T ToEnum<T>()
			where T : struct
		{
			Check.EnsureNotNull(source, nameof(source));
			return (T)Enum.Parse(typeof(T), source);
		}

		/// <summary>
		/// 将字符串转换为枚举值。
		/// </summary>
		/// <typeparam name="T">枚举类型</typeparam>
		/// <param name="ignoreCase">是否忽略大小写。</param>
		/// <returns>枚举对象。</returns>
		public T ToEnum<T>(bool ignoreCase)
			where T : struct
		{
			Check.EnsureNotNull(source, nameof(source));
			return (T)Enum.Parse(typeof(T), source, ignoreCase);
		}

		/// <summary>
		/// 计算字符串的 MD5 哈希值。
		/// </summary>
		/// <returns>字符串的 MD5 哈希值。</returns>
		public string ToMd5()
		{
			using var md5 = MD5.Create();
			var inputBytes = Encoding.UTF8.GetBytes(source);
			var hashBytes = md5.ComputeHash(inputBytes);

			var sb = new StringBuilder();
			foreach (var hashByte in hashBytes)
			{
				sb.Append(hashByte.ToString("X2"));
			}

			return sb.ToString();
		}

		/// <summary>
		/// 将 camelCase 字符串转换为 PascalCase 字符串。
		/// </summary>
		/// <param name="useCurrentCulture">是否使用当前文化进行大小写转换。</param>
		/// <returns>转换后的 PascalCase 字符串。</returns>
		public string ToPascalCase(bool useCurrentCulture = false)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return source;
			}

			if (source.Length == 1)
			{
				return useCurrentCulture ? source.ToUpper() : source.ToUpperInvariant();
			}

			return (useCurrentCulture ? char.ToUpper(source[0]) : char.ToUpperInvariant(source[0])) + source[1..];
		}

		/// <summary>
		/// 如果字符串超过最大长度，则从末尾截取子字符串。
		/// </summary>
		/// <param name="maxLength">最大长度。</param>
		/// <returns>截取后的字符串。</returns>
		public string TruncateFromBeginning(int maxLength)
		{
			if (source == null)
			{
				return null;
			}

			if (source.Length <= maxLength)
			{
				return source;
			}

			return source.Right(maxLength);
		}

		/// <summary>
		/// 如果字符串超过最大长度，则从开头截取并添加 "..." 后缀。返回的字符串长度不会超过指定的最大长度。
		/// </summary>
		/// <param name="maxLength">最大长度。</param>
		/// <returns>截取后的字符串。</returns>
		public string TruncateWithPostfix(int maxLength)
		{
			return source.TruncateWithPostfix(maxLength, "...");
		}

		/// <summary>
		/// 如果字符串超过最大长度，则从开头截取并添加指定后缀。返回的字符串长度不会超过指定的最大长度。
		/// </summary>
		/// <param name="maxLength">最大长度。</param>
		/// <param name="postfix">后缀字符串。</param>
		/// <returns>截取后的字符串。</returns>
		public string TruncateWithPostfix(int maxLength, string postfix)
		{
			if (source == null)
			{
				return null;
			}

			if (source == string.Empty || maxLength == 0)
			{
				return string.Empty;
			}

			if (source.Length <= maxLength)
			{
				return source;
			}

			if (maxLength <= postfix.Length)
			{
				return postfix.Left(maxLength);
			}

			return source.Left(maxLength - postfix.Length) + postfix;
		}

		/// <summary>
		/// 使用 <see cref="Encoding.UTF8"/> 编码将字符串转换为字节数组。
		/// </summary>
		/// <returns>字符串的字节数组表示。</returns>
		public byte[] GetUtf8Bytes()
		{
			return source.GetBytes(Encoding.UTF8);
		}

		/// <summary>
		/// 确定字符串是否为有效的电子邮件地址。
		/// </summary>
		/// <returns>如果字符串是有效的电子邮件地址，则为 <c>true</c>；否则为 <c>false</c>。</returns>
		public bool IsEmail() => Regex.IsMatch(source, EmailRegex);

		/// <summary>
		/// 确定字符串是否为有效的十进制数字。
		/// </summary>
		/// <returns>如果字符串是有效的十进制数字，则为 <c>true</c>；否则为 <c>false</c>。</returns>
		public bool IsDecimal() => decimal.TryParse(source, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

		/// <summary>
		/// 确定字符串是否为有效的整数。
		/// </summary>
		/// <returns>如果字符串是有效的整数，则为 <c>true</c>；否则为 <c>false</c>。</returns>
		public bool IsNumeric() => int.TryParse(source, out _);

		/// <summary>
		/// 确定字符串是否为有效的电话号码。
		/// </summary>
		/// <returns>如果字符串是有效的电话号码，则为 <c>true</c>；否则为 <c>false</c>。</returns>
		public bool IsPhoneNumber() => Regex.IsMatch(source, PhoneNumberRegex);

		/// <summary>
		/// 确定字符串是否仅包含字母。
		/// </summary>
		/// <returns>如果字符串仅包含字母，则为 <c>true</c>；否则为 <c>false</c>。</returns>
		public bool IsCharacterString() => Regex.IsMatch(source, CharactersRegex);

		/// <summary>
		/// 返回移除 HTML 注释、脚本、样式和标签后的字符串。
		/// </summary>
		/// <returns>移除 HTML 内容后的字符串。</returns>
		public string DecodeHtml()
		{
			if (source == null)
			{
				return null;
			}

			var ret = source.FixHtml();

			// 移除 HTML 标签
			ret = new Regex(REMOVE_HTML_TAGS_REGEX).Replace(ret, string.Empty);

			return WebUtility.HtmlDecode(ret);
		}

		/// <summary>
		/// 返回移除 HTML 注释、脚本和样式后的字符串。
		/// </summary>
		/// <returns>移除 HTML 内容后的字符串。</returns>
		public string FixHtml()
		{
			// 移除注释
			var withoutComments = _removeHtmlCommentsRegex.Replace(source, string.Empty);

			// 移除脚本
			var withoutScripts = _removeHtmlScriptsRegex.Replace(withoutComments, string.Empty);

			// 移除样式
			var withoutStyles = _removeHtmlStylesRegex.Replace(withoutScripts, string.Empty);

			return withoutStyles;
		}

		/// <summary>
		/// 将字符串截断到指定长度。
		/// </summary>
		/// <param name="length">要截断的长度。</param>
		/// <returns>截断后的字符串。</returns>
		public string Truncate(int length) => Truncate(source, length, false);

		/// <summary>
		/// 使用参数格式化字符串。
		/// </summary>
		/// <param name="args">要格式化的参数。</param>
		/// <returns>格式化后的字符串。</returns>
		public string AsFormat(params object[] args) => string.Format(source, args);

		/// <summary>
		/// 将字符串截断到指定长度。
		/// </summary>
		/// <param name="length">要截断的长度。</param>
		/// <param name="ellipsis">是否添加省略号。</param>
		/// <returns>截断后的字符串。</returns>
		public string Truncate(int length, bool ellipsis)
		{
			if (!string.IsNullOrEmpty(source))
			{
				source = source.Trim();
				if (source.Length > length)
				{
					if (ellipsis)
					{
						return source[..length] + "...";
					}

					return source[..length];
				}
			}

			{
			}

			return source ?? string.Empty;
		}

		/// <summary>
		/// 按指定方式修剪文本并返回新字符串。
		/// </summary>
		/// <param name="type">要应用的修剪类型。</param>
		/// <returns>修剪后的字符串。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当指定的修剪类型无效时抛出。</exception>
		public string Trim(TextTrimType type)
		{
			if (string.IsNullOrEmpty(source))
			{
				return source;
			}

			return type switch
			{
				TextTrimType.Head => source.TrimStart(),
				TextTrimType.Tail => source.TrimEnd(),
				TextTrimType.Both => source.Trim(),
				TextTrimType.All => Regex.Replace(source, @"\s+", string.Empty),
				TextTrimType.None => source,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}

		/// <summary>
		/// 按指定大小写格式规范化文本并返回新字符串。
		/// </summary>
		/// <param name="caseType">要应用的大小写类型。</param>
		/// <returns>规范化后的字符串。</returns>
		public string Normalize(TextCaseType caseType)
		{
			if (string.IsNullOrEmpty(source))
			{
				return source;
			}

			var text = CultureInfo.CurrentCulture.TextInfo;
			return caseType switch
			{
				TextCaseType.Upper => text.ToUpper(source),
				TextCaseType.Lower => text.ToLower(source),
				TextCaseType.Title => text.ToTitleCase(source),
				TextCaseType.None => source,
				_ => throw new ArgumentOutOfRangeException(nameof(caseType), caseType, null)
			};
		}

		/// <summary>
		/// 对字符串的指定部分进行掩码处理。
		/// </summary>
		/// <param name="start">要掩码的起始索引。</param>
		/// <param name="length">要掩码的长度。</param>
		/// <param name="maskChar">用于掩码的字符，默认为 '*'。</param>
		/// <returns>掩码处理后的字符串。</returns>
		public string Mask(int start, int length, char maskChar = '*')
		{
			var end = start + length;
			if (source.Length <= start)
			{
				return string.Empty;
			}

			if (source.Length < end)
			{
				return source[..start] + "".PadLeft(source.Length - start, maskChar);
			}

			return source[..start] + "".PadLeft(length, maskChar) + source[end..];
		}

		/// <summary>
		/// 如果字符串为 null 或空白则返回默认值。
		/// </summary>
		/// <param name="default">默认值。</param>
		/// <returns>原字符串或默认值。</returns>
		public string DefaultIfNullOrWhiteSpace([NotNull] string @default)
		{
			return string.IsNullOrWhiteSpace(source) ? @default : source;
		}

		/// <summary>
		/// 使用 Base64 编码字符串。
		/// </summary>
		/// <returns>Base64 编码后的字符串。</returns>
		public string ToBase64()
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return string.Empty;
			}

			var plainTextBytes = Encoding.UTF8.GetBytes(source);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		/// <summary>
		/// 安全截取子字符串，避免越界异常。
		/// </summary>
		/// <param name="index">起始索引。</param>
		/// <param name="length">要截取的长度，默认为 0，表示截取到字符串末尾。</param>
		/// <returns>截取后的子字符串。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当索引或长度超出范围时抛出。</exception>
		public string SafeSubstring(int index, int length = 0)
		{
			if (length < 0 || index < 0 || index > source.Length - 1)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			if (length == 0 || length > (source.Length - index))
			{
				return source[index..];
			}

			return source.Substring(index, length);
		}

		/// <summary>
		/// 使用指定编码将字符串转换为字节数组。
		/// </summary>
		/// <param name="encoding">用于编码的编码对象。</param>
		/// <returns>字符串的字节数组表示。</returns>
		public byte[] GetBytes([NotNull] Encoding encoding)
		{
			Check.EnsureNotNull(source, nameof(source));
			Check.EnsureNotNull(encoding, nameof(encoding));

			return encoding.GetBytes(source);
		}
	}

	/// <summary>
	/// 提供对字符串的扩展方法。
	/// </summary>
	extension(string)
	{
		/// <summary>
		/// 返回第一个非 null 且非空的字符串部分作为结果。
		/// </summary>
		/// <param name="parts">要检查的字符串数组。</param>
		/// <returns>第一个非 null 且非空的字符串部分，如果没有，则返回空字符串。</returns>
		public static string Collapse(params string[] parts)
		{
			foreach (var part in parts)
			{
				if (!string.IsNullOrEmpty(part))
				{
					return part;
				}
			}

			return string.Empty;
		}
	}
}