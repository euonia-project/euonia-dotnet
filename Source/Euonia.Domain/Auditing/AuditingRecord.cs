namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 审计记录的实体。
/// </summary>
public class AuditingRecord
{
    /// <summary>
    /// 获取或设置用户标识符。
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 获取或设置租户标识符。
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 获取或设置租户名称。
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// 获取或设置操作执行时间。
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// 获取或设置操作执行时长（毫秒）。
    /// </summary>
    public int ExecutionDuration { get; set; }

    /// <summary>
    /// 获取或设置客户端标识符。
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// 获取或设置关联标识符。
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// 获取或设置客户端 IP 地址。
    /// </summary>
    public string ClientIpAddress { get; set; }

    /// <summary>
    /// 获取或设置客户端名称。
    /// </summary>
    public string ClientName { get; set; }

    /// <summary>
    /// 获取或设置浏览器信息。
    /// </summary>
    public string BrowserInfo { get; set; }

    /// <summary>
    /// 获取或设置 HTTP 方法。
    /// </summary>
    public string HttpMethod { get; set; }

    /// <summary>
    /// 获取或设置 HTTP 状态码。
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// 获取或设置请求 URL。
    /// </summary>
    public string Url { get; set; }
}