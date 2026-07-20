namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 异步释放操作的执行标志。
/// </summary>
[Flags]
public enum DisposeFlags
{
    /// <summary>
    /// 并发执行多个异步释放方法。所有异步释放方法同时启动，然后异步等待全部完成。
    /// </summary>
    ExecuteConcurrently = 1,

    /// <summary>
    /// 串行执行多个异步释放方法。每个异步释放方法在前一个完成后才会启动。
    /// </summary>
    ExecuteSerially = 2,
}