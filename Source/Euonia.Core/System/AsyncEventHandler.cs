namespace System;

/// <summary>
/// 表示异步事件处理器。
/// </summary>
/// <typeparam name="TEventArgs">事件参数类型。</typeparam>
/// <param name="sender">事件发送者。</param>
/// <param name="args">事件参数。</param>
public delegate Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs args);