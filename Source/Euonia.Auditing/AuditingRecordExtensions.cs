namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计记录的外部扩展：合并扩展属性与异常消息脱敏。
/// </summary>
public static class AuditingRecordExtensions
{
	/// <summary>
	/// 合并扩展属性：保留已有 JSON，新值覆盖同名键。
	/// </summary>
	/// <param name="record">审计记录。</param>
	/// <param name="values">要写入的键值。</param>
	public static void MergeExtraProperties(this AuditingRecord record, IReadOnlyDictionary<string, string> values)
	{
		ArgumentNullException.ThrowIfNull(record);
		ArgumentNullException.ThrowIfNull(values);

		AuditingSerializer.MergeExtraProperties(record, values);
	}

	/// <summary>
	/// 脱敏异常消息（截断并遮蔽凭据片段）。
	/// </summary>
	/// <param name="message">异常消息。</param>
	/// <returns>脱敏后的消息。</returns>
	public static string? MaskExceptionMessage(string? message)
	{
		return AuditingSerializer.MaskExceptionMessage(message);
	}
}
