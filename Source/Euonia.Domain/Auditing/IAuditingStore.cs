namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 审计存储的契约。
/// </summary>
public interface IAuditingStore
{
    /// <summary>
    /// 将 <see cref="AuditingRecord"/> 保存到存储中。
    /// </summary>
    /// <param name="record">要保存的审计记录。</param>
    /// <returns>表示异步保存操作的任务。</returns>
    Task SaveAsync(AuditingRecord record);
}