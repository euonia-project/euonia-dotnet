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
    /// <param name="result">回复的结果。</param>
    public MessageRepliedEventArgs(object result)
    {
        Result = result;
    }

    /// <summary>
    /// 获取回复的结果。
    /// </summary>
    /// <value>回复的结果。</value>
    public object Result { get; }
}