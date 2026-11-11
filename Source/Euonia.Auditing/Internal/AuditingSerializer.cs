using System.Text.Json;
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
	public static string? Serialize(object? value, string mask = "***")
	{
		if (value == null)
		{
			return null;
		}

		try
		{
			return JsonSerializer.Serialize(SensitiveDataMasker.Mask(value, mask), _options);
		}
		catch (Exception)
		{
			return JsonSerializer.Serialize($"<{value.GetType().Name}>", _options);
		}
	}
}
