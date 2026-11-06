namespace Nerosoft.Euonia.Concurrency.Azure.Internal;

/// <summary>
/// Azure Blob 租约相关错误码常量，用于匹配服务端返回的错误代码。
/// </summary>
internal static class AzureErrors
{
    /// <summary>
    /// 租约操作可能返回的错误码：Blob 不存在、已存在其他租约、请求缺少租约 ID。
    /// </summary>
    public const string BlobNotFound = nameof(BlobNotFound),
                        LeaseAlreadyPresent = nameof(LeaseAlreadyPresent),
                        LeaseIdMissing = nameof(LeaseIdMissing);
}