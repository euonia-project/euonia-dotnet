namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息处理类型的枚举。
/// </summary>
public enum MessageProcessType
{
    /// <summary>
    /// 发送消息。
    /// </summary>
    Send,

    /// <summary>
    /// 投递（分发）消息。
    /// </summary>
    Dispatch,

    /// <summary>
    /// 接收消息。
    /// </summary>
    Receive
}