using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 一个可等待的包装器，用于包装其结果可释放的任务。该包装器本身不可释放，因此可以防止诸如使用 "using (MyAsync())" 的错误用法，正确的用法应为 "using (await MyAsync())"。
/// </summary>
/// <typeparam name="T">底层任务结果的类型。</typeparam>
public struct AwaitableDisposable<T> where T : IDisposable
{
    /// <summary>
    /// 底层的任务。
    /// </summary>
    private readonly Task<T> _task;

    /// <summary>
    /// 初始化围绕指定任务的可等待包装器。
    /// </summary>
    /// <param name="task">要包装的底层任务。此参数不能为 <c>null</c>。</param>
    public AwaitableDisposable(Task<T> task)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    /// <summary>
    /// 返回底层的任务。
    /// </summary>
    public Task<T> AsTask()
    {
        return _task;
    }

    /// <summary>
    /// 隐式转换为底层任务。
    /// </summary>
    /// <param name="source">可等待的包装器。</param>
    public static implicit operator Task<T>(AwaitableDisposable<T> source)
    {
        return source.AsTask();
    }

    /// <summary>
    /// 基础设施。返回底层任务的任务等待器。
    /// </summary>
    public TaskAwaiter<T> GetAwaiter()
    {
        return _task.GetAwaiter();
    }

    /// <summary>
    /// 基础设施。返回底层任务的已配置任务等待器。
    /// </summary>
    /// <param name="continueOnCapturedContext">是否尝试将后续操作调度回捕获的上下文。</param>
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
    {
        return _task.ConfigureAwait(continueOnCapturedContext);
    }
}
