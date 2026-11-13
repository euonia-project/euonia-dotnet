namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 通道 → 端点的通配符匹配：供 <see cref="GrpcBusOptions.Endpoints"/> 中带通配符的键自动推断使用。
/// </summary>
/// <remarks>
/// <see cref="GrpcBusOptions.Endpoints"/> 的键包含 <c>*</c>/<c>?</c> 即视为通配符模式（Glob 风格、通道全名锚定、忽略大小写）：
/// <list type="bullet">
/// <item><description><c>*</c> 匹配任意字符序列（不含 <c>.</c>，即不跨段）；</description></item>
/// <item><description><c>?</c> 匹配单个字符（不含 <c>.</c>）；</description></item>
/// <item><description><c>**</c> 匹配任意字符序列，可跨段（含 <c>.</c>）。</description></item>
/// </list>
/// 端点值作为模板，支持 <c>{channel}</c>（整个通道名）与 <c>{1}</c>…<c>{9}</c>
/// （按 <c>*</c>/<c>**</c>/<c>?</c> 出现顺序的匹配段），形如 <c>http://{1}:5081</c>；未引用的段忽略即可。
/// </remarks>
internal sealed class GrpcEndpointPattern
{
	/// <summary>
	/// 通配符匹配段（<c>*</c>/<c>**</c>/<c>?</c>）在模式中的捕获组名前缀。
	/// </summary>
	private const string SegmentGroupPrefix = "s";

	private readonly System.Text.RegularExpressions.Regex _regex;

	private GrpcEndpointPattern(string pattern, string endpointTemplate)
	{
		Pattern = pattern;
		EndpointTemplate = endpointTemplate;
		_regex = new System.Text.RegularExpressions.Regex(BuildRegexPattern(pattern), System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
	}

	/// <summary>
	/// 获取通配符模式。
	/// </summary>
	public string Pattern { get; }

	/// <summary>
	/// 获取端点地址模板。
	/// </summary>
	/// <remarks>
	/// 支持占位符：<c>{channel}</c>（整个通道名）、<c>{1}</c>…<c>{9}</c>（第 N 个通配符段匹配值）。
	/// </remarks>
	public string EndpointTemplate { get; }

	/// <summary>
	/// 判断键是否为通配符模式：包含 <c>*</c> 或 <c>?</c> 即是。
	/// </summary>
	/// <param name="key">Endpoints 的键（通道名或模式）。</param>
	/// <returns>是通配符模式则为 <c>true</c>；否则为精确通道名。</returns>
	internal static bool IsWildcard(string key)
	{
		return !string.IsNullOrEmpty(key) && key.AsSpan().IndexOfAny('*', '?') >= 0;
	}

	/// <summary>
	/// 创建通配符匹配。
	/// </summary>
	/// <param name="pattern">通配符模式，如 <c>billing.*</c>、<c>*.ship</c>、<c>order.**</c>。</param>
	/// <param name="endpointTemplate">端点模板，可含 <c>{channel}</c> / <c>{1}</c>…<c>{9}</c> 占位符。</param>
	/// <returns>匹配器实例。</returns>
	internal static GrpcEndpointPattern Create(string pattern, string endpointTemplate)
	{
		return new GrpcEndpointPattern(pattern, endpointTemplate);
	}

	/// <summary>
	/// 尝试用本模式解析通道对应的端点地址。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="endpoint">解析出的端点地址；未命中时为 <c>null</c>。</param>
	/// <returns>命中则为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool TryResolve(string channel, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string endpoint)
	{
		endpoint = null!;
		if (string.IsNullOrWhiteSpace(channel))
		{
			return false;
		}

		var match = _regex.Match(channel);
		if (!match.Success)
		{
			return false;
		}

		endpoint = Render(EndpointTemplate, channel, match);
		return true;
	}

	/// <summary>
	/// 把通配符模式编译为锚定的正则表达式：<c>*</c> → <c>[^.]*</c>、<c>?</c> → <c>[^.]</c>、
	/// <c>**</c> → <c>.*</c>，每个通配符段包捕获组（组名带 <see cref="SegmentGroupPrefix"/> 前缀供模板渲染识别）。
	/// </summary>
	private static string BuildRegexPattern(string pattern)
	{
		var builder = new System.Text.StringBuilder("^");
		var groupIndex = 0;
		var index = 0;

		while (index < pattern.Length)
		{
			var current = pattern[index];

			// ** 跨段通配：整体一个捕获组。
			if (current == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
			{
				groupIndex++;
				builder.Append($"(?<{SegmentGroupPrefix}{groupIndex}>.*)");
				index += 2;
				continue;
			}

			if (current == '*')
			{
				groupIndex++;
				builder.Append($"(?<{SegmentGroupPrefix}{groupIndex}>[^.]*)");
				index++;
				continue;
			}

			if (current == '?')
			{
				groupIndex++;
				builder.Append($"(?<{SegmentGroupPrefix}{groupIndex}>[^.])");
				index++;
				continue;
			}

			builder.Append(System.Text.RegularExpressions.Regex.Escape(current.ToString()));
			index++;
		}

		builder.Append('$');
		return builder.ToString();
	}

	/// <summary>
	/// 渲染端点模板：替换 <c>{channel}</c> 与 <c>{N}</c> 占位符（转义后写入）。
	/// </summary>
	private static string Render(string template, string channel, System.Text.RegularExpressions.Match match)
	{
		return System.Text.RegularExpressions.Regex.Replace(
			template,
			@"\{([A-Za-z0-9_]+)\}",
			m =>
			{
				var name = m.Groups[1].Value;
				if (name.Equals("channel", StringComparison.OrdinalIgnoreCase))
				{
					return Uri.EscapeDataString(channel);
				}

				// {N} 引用第 N 个通配符段。
				if (name.Length == 1 && char.IsDigit(name[0]) && name[0] != '0')
				{
					var group = match.Groups[$"{SegmentGroupPrefix}{name[0]}"];
					if (group.Success)
					{
						return Uri.EscapeDataString(group.Value);
					}
				}

				// 未知占位符原样保留，便于暴露配置错误。
				return m.Value;
			},
			System.Text.RegularExpressions.RegexOptions.CultureInvariant);
	}
}