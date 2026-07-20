using System.ComponentModel;

namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 用于包装 <see cref="Task"/> 的帮助类，提供更多可用于 UI 数据绑定场景的信息。详见 MSDN Magazine：https://msdn.microsoft.com/magazine/dn605875。
/// </summary>
/// <typeparam name="TResult">任务返回结果的类型。</typeparam>
public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
{
    /// <summary>
    /// 初始化 <see cref="NotifyTaskCompletion{TResult}"/> 类的新实例。
    /// </summary>
    /// <param name="task">要等待的任务。</param>
    public NotifyTaskCompletion(Task<TResult> task)
    {
        Task = task;
        if (!task.IsCompleted)
        {
            TaskCompletion = WatchTaskAsync(task);
        }
    }

    private async Task WatchTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            //
        }

        if (PropertyChanged == null)
        {
            return;
        }

        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotCompleted)));

        if (task.IsCanceled)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
        }
        else if (task.IsFaulted)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(InnerException)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
        }
        else
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
        }
    }

    /// <summary>
    /// 获取正在等待的任务。
    /// </summary>
    public Task<TResult> Task { get; }

    /// <summary>
    /// 获取任务包装器任务。
    /// </summary>
    public Task TaskCompletion { get; }

    /// <summary>
    /// 获取指定任务的结果。
    /// </summary>
    public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : default;

    /// <summary>
    /// 获取任务的状态。
    /// </summary>
    public TaskStatus Status => Task.Status;

    /// <summary>
    /// 获取一个值，指示任务是否已完成。
    /// </summary>
    public bool IsCompleted => Task.IsCompleted;

    /// <summary>
    /// 获取一个值，指示任务是否未完成。
    /// </summary>
    public bool IsNotCompleted => !Task.IsCompleted;

    /// <summary>
    /// 获取一个值，指示任务是否已成功完成。
    /// </summary>
    public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

    /// <summary>
    /// 获取一个值，指示任务是否已被取消。
    /// </summary>
    public bool IsCanceled => Task.IsCanceled;

    /// <summary>
    /// 获取一个值，指示任务是否发生错误。
    /// </summary>
    public bool IsFaulted => Task.IsFaulted;

    /// <summary>
    /// 获取任务上发生的异常（如果发生）。
    /// </summary>
    public AggregateException Exception => Task.Exception;

    /// <summary>
    /// 获取任务的内部异常。
    /// </summary>
    public Exception InnerException => Exception?.InnerException;

    /// <summary>
    /// 获取任务的错误消息。
    /// </summary>
    public string ErrorMessage => InnerException?.Message ?? Exception.Message;

    /// <summary>
    /// 属性更改事件。
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
}

/// <summary>
/// 用于包装 <see cref="Task"/> 的帮助类，提供更多可用于 UI 数据绑定场景的信息。详见 MSDN Magazine：https://msdn.microsoft.com/magazine/dn605875。
/// </summary>
public sealed class NotifyTaskCompletion : INotifyPropertyChanged
{
    /// <summary>
    /// 初始化 <see cref="NotifyTaskCompletion"/> 类的新实例。
    /// </summary>
    /// <param name="task">要等待的任务。</param>
    public NotifyTaskCompletion(Task task)
    {
        Task = task;
        if (!task.IsCompleted)
        {
            TaskCompletion = WatchTaskAsync(task);
        }
    }

    private async Task WatchTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            //
        }

        if (PropertyChanged == null)
        {
            return;
        }

        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
        PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotCompleted)));

        if (task.IsCanceled)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
        }
        else if (task.IsFaulted)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(InnerException)));
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
        }
        else
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
        }
    }

    /// <summary>
    /// 获取正在等待的任务。
    /// </summary>
    public Task Task { get; }

    /// <summary>
    /// 获取任务包装器任务。
    /// </summary>
    public Task TaskCompletion { get; }

    /// <summary>
    /// 获取任务的状态。
    /// </summary>
    public TaskStatus Status => Task.Status;

    /// <summary>
    /// 获取一个值，指示任务是否已完成。
    /// </summary>
    public bool IsCompleted => Task.IsCompleted;

    /// <summary>
    /// 获取一个值，指示任务是否未完成。
    /// </summary>
    public bool IsNotCompleted => !Task.IsCompleted;

    /// <summary>
    /// 获取一个值，指示任务是否已成功完成。
    /// </summary>
    public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

    /// <summary>
    /// 获取一个值，指示任务是否已被取消。
    /// </summary>
    public bool IsCanceled => Task.IsCanceled;

    /// <summary>
    /// 获取一个值，指示任务是否发生错误。
    /// </summary>
    public bool IsFaulted => Task.IsFaulted;

    /// <summary>
    /// 获取任务上发生的异常（如果发生）。
    /// </summary>
    public AggregateException Exception => Task.Exception;

    /// <summary>
    /// 获取任务的内部异常。
    /// </summary>
    public Exception InnerException => Exception?.InnerException;

    /// <summary>
    /// 获取任务的错误消息。
    /// </summary>
    public string ErrorMessage => InnerException?.Message ?? Exception.Message;

    /// <summary>
    /// 属性更改事件。
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
}