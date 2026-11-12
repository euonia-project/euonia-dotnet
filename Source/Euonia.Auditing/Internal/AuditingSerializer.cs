using System.Text.Json;
using System.Text.RegularExpressions;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计参数的脱敏与序列化。
/// </summary>
internal static class AuditingSerializer
{
	private static readonly JsonSerializerOptions _options = new()
	{
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
	};

	/// <summary>
	/// 脱敏后序列化；失败时返回类型占位而不是抛出。
	/// </summary>
	public static string? Serialize(object? value, string mask = "***", int maxDepth = 8)
	{
		if (value == null)
		{
			return null;
		}

		try
		{
			return JsonSerializer.Serialize(SensitiveDataMasker.Mask(value, mask, maxDepth), _options);
		}
		catch (Exception)
		{
			return JsonSerializer.Serialize($"<{value.GetType().Name}>", _options);
		}
	}

	/// <summary>
	/// 脱敏异常消息：截断并遮蔽常见的凭据片段。
	/// </summary>
	/// <param name="message">异常消息。</param>
	/// <returns>脱敏后的消息。</returns>
	public static string? MaskExceptionMessage(string? message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return message;
		}

		var masked = SensitivePattern.Replace(message, "$1=***");
		masked = BareTokenPattern.Replace(masked, "$1=***");
		masked = ConnectionStringPattern.Replace(masked, "$1=***");
		if (masked.Length <= 2000)
		{
			return masked;
		}

		// 不截断代理对：边界落在高代理项时回退一位。
		var length = char.IsHighSurrogate(masked[1999]) ? 1999 : 2000;
		return masked[..length];
	}

	/// <summary>
	/// 合并扩展属性：保留已有 JSON，新值覆盖同名键。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="values">要写入的键值。</param>
	public static void MergeExtraProperties(AuditingRecord record, IReadOnlyDictionary<string, string> values)
	{
		var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
		if (!string.IsNullOrWhiteSpace(record.ExtraProperties))
		{
			try
			{
				properties = JsonSerializer.Deserialize<Dictionary<string, object?>>(record.ExtraProperties, _options) ?? properties;
			}
			catch (JsonException)
			{
				// 损坏的旧值不阻断新属性写入。
			}
		}

		foreach (var pair in values)
		{
			properties[pair.Key] = pair.Value;
		}

		record.ExtraProperties = JsonSerializer.Serialize(properties, _options);
	}

	private static readonly Regex ConnectionStringPattern = new(
		"""(?i)\b(password|pwd|user id|userid|accesskey|access key|privatekey|private key|client_secret)\s*=\s*[^;\r\n]*""",
		RegexOptions.Compiled);

	private static readonly Regex SensitivePattern = new(
		"""(?i)(password|pwd|token|secret|api[_-]?key|connectionstring|authorization|cookie|client_secret)\s*[=:]\s*("[^"]*"|'[^']*'|[^"'\s,\r\n]+)""",
		RegexOptions.Compiled);

	private static readonly Regex BareTokenPattern = new(
		"""(?i)\b(password|pwd|token|secret|api[_-]?key)\s+([^\s,;]+)""",
		RegexOptions.Compiled);
}
