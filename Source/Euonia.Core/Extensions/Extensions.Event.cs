public static partial class Extensions
{
    /// <summary>
    /// 安全地触发给定事件。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="sender">事件源。</param>
    public static void CheckAndInvoke(this EventHandler handler, object sender)
    {
        handler.CheckAndInvoke(sender, EventArgs.Empty);
    }

    /// <summary>
    /// 安全地触发给定事件。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="sender">事件源。</param>
    /// <param name="e">事件参数。</param>
    public static void CheckAndInvoke(this EventHandler handler, object sender, EventArgs e)
    {
        handler?.Invoke(sender, e);
    }

    /// <summary>
    /// 安全地触发给定事件。
    /// </summary>
    /// <typeparam name="TEventArgs"><see cref="EventArgs"/> 的类型。</typeparam>
    /// <param name="handler">事件处理器。</param>
    /// <param name="sender">事件源。</param>
    /// <param name="e">事件参数。</param>
    public static void CheckAndInvoke<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs e)
        where TEventArgs : EventArgs
    {
        handler?.Invoke(sender, e);
    }
}