namespace Nerosoft.Euonia.Threading.Interop;

/// <summary>
/// 传递给遵循标准 <c>sender, arguments</c> 事件模式的 .NET 事件的参数。
/// </summary>
/// <typeparam name="TSender">事件发送者的类型。通常为 <see cref="object"/>。</typeparam>
/// <typeparam name="TEventArgs">事件参数的类型。通常为 <see cref="EventArgs"/> 或其派生类型。</typeparam>
public struct EventArguments<TSender, TEventArgs>
{
    /// <summary>
    /// 事件的发送者。
    /// </summary>
    public TSender Sender { get; set; }

    /// <summary>
    /// 事件参数。
    /// </summary>
    public TEventArgs EventArgs { get; set; }
}
