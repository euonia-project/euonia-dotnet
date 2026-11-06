namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息被回复时触发的事件参数。
/// </summary>
/// <seealso cref="EventArgs" />
public class MessageRepliedEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="MessageRepliedEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="result">回复的结果；只有完成通知、没有响应结果时可为 <c>null</c>。</param>
    public MessageRepliedEventArgs(object? result)
    {
        Result = result;
    }

    /// <summary>
    /// 获取回复的结果；只有完成通知、没有响应结果时为 <c>null</c>。
    /// </summary>
    /// <value>回复的结果。</value>
    public object? Result { get; }
}