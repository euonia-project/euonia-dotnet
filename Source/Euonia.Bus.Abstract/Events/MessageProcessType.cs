namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义消息处理类型的枚举。
/// </summary>
public enum MessageProcessType
{
    /// <summary>
    /// 发送。
    /// </summary>
    Send,

    /// <summary>
    /// 投递。
    /// </summary>
    Dispatch,

    /// <summary>
    /// 接收。
    /// </summary>
    Receive
}