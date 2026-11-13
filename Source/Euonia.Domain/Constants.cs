namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 定义领域事件元数据字典中使用的键名常量。
/// </summary>
internal static class Constants
{
    /// <summary>
    /// 事件意图元数据键。
    /// </summary>
    public const string EventIntentMetadataKey = "$nerosoft.euonia.event.intent";

    /// <summary>
    /// 事件发起方类型键。
    /// </summary>
    public const string EventOriginTypeKey = "$nerosoft.euonia.event.originator.type";

    /// <summary>
    /// 事件发起方标识符键。
    /// </summary>
    public const string EventOriginatorId = "$nerosoft.euonia.event.originator.id";
}