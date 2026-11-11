namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 子域名租户解析。
/// </summary>
internal static class TenantHostParser
{
	public static string? Parse(string host, string format)
	{
		var tokenIndex = format.IndexOf("{tenant}", StringComparison.Ordinal);
		if (tokenIndex < 0)
		{
			return null;
		}

		var suffix = format[(tokenIndex + "{tenant}".Length)..].TrimStart('.');
		if (suffix.Length == 0)
		{
			return null;
		}

		var normalizedHost = host.Split(':')[0].TrimEnd('.');
		if (!normalizedHost.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var prefix = normalizedHost[..^(suffix.Length + 1)];
		if (prefix.Length == 0 || prefix.Contains('.', StringComparison.Ordinal))
		{
			return null;
		}

		return prefix;
	}
}
