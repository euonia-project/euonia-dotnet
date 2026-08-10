namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用于处理繁忙状态更改的委托。
/// </summary>
/// <param name="sender">事件发送方。</param>
/// <param name="e">繁忙状态更改事件的事件参数。</param>
public delegate void BusyChangedEventHandler(object sender, BusyChangedEventArgs e);