namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 提供已完成的任务常量。
/// </summary>
public static class TaskConstants
{
    /// <summary>
    /// 以 <c>true</c> 值完成的任务。
    /// </summary>
    public static Task<bool> BooleanTrue => Task.FromResult(true);

    /// <summary>
    /// 以 <c>false</c> 值完成的任务。
    /// </summary>
    public static Task<bool> BooleanFalse => TaskConstants<bool>.Default;

    /// <summary>
    /// 以 <c>0</c> 值完成的任务。
    /// </summary>
    public static Task<int> Int32Zero => TaskConstants<int>.Default;

    /// <summary>
    /// 以 <c>-1</c> 值完成的任务。
    /// </summary>
    public static Task<int> Int32NegativeOne => Task.FromResult(-1);

    /// <summary>
    /// 已完成的 <see cref="Task"/>。
    /// </summary>
    public static Task Completed => Task.CompletedTask;

    /// <summary>
    /// 已取消的任务。
    /// </summary>
    public static Task Canceled => TaskConstants<object>.Canceled;
}

/// <summary>
/// 提供已完成的任务常量。
/// </summary>
/// <typeparam name="T">任务结果的类型。</typeparam>
public static class TaskConstants<T>
{
    /// <summary>
    /// 以 <typeparamref name="T"/> 的默认值完成的任务。
    /// </summary>
    public static Task<T> Default => Task.FromResult(default(T));

    /// <summary>
    /// 已取消的任务。
    /// </summary>
    public static Task<T> Canceled => Task.FromCanceled<T>(new CancellationToken(true));
}
