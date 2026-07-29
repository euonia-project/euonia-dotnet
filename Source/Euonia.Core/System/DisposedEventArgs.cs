namespace System;

/// <summary>
/// 对象释放事件参数。
/// </summary>
public class DisposedEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="DisposedEventArgs"/> 类的新实例。
    /// </summary>
    public DisposedEventArgs()
    {
    }

    /// <summary>
    /// 初始化 <see cref="DisposedEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="hashCode">已释放对象的哈希码。</param>
    public DisposedEventArgs(int hashCode)
        : this()
    {
        HashCode = hashCode;
    }

    /// <summary>
    /// 获取已释放对象的哈希码。
    /// </summary>
    public int HashCode { get; }
}