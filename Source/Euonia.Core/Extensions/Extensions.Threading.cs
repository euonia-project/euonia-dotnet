using System.ComponentModel;
using Nerosoft.Euonia.Threading;

public static partial class Extensions
{
    /// <summary>
    /// 尝试完成 <see cref="TaskCompletionSource{TResult}"/>，传播 <paramref name="task"/> 的完成状态。
    /// </summary>
    /// <typeparam name="TResult">目标异步操作结果的类型。</typeparam>
    /// <typeparam name="TSourceResult">源异步操作结果的类型。</typeparam>
    /// <param name="this">任务完成源。不能为 <c>null</c>。</param>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <returns>如果此方法完成了任务完成源，则为 <c>true</c>；如果已经完成，则为 <c>false</c>。</returns>
    public static bool TryCompleteFromCompletedTask<TResult, TSourceResult>(this TaskCompletionSource<TResult> @this, Task<TSourceResult> task) where TSourceResult : TResult
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}

		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(task);
#endif

		if (task.IsFaulted)
        {
            return @this.TrySetException(task.Exception.InnerExceptions);
        }

        if (task.IsCanceled)
        {
            try
            {
                task.WaitAndUnwrapException();
            }
            catch (OperationCanceledException exception)
            {
                var token = exception.CancellationToken;
                return token.IsCancellationRequested ? @this.TrySetCanceled(token) : @this.TrySetCanceled();
            }
        }

        return @this.TrySetResult(task.Result);
    }

    /// <summary>
    /// 尝试完成 <see cref="TaskCompletionSource{TResult}"/>，传播 <paramref name="task"/> 的完成状态，但如果任务成功完成，则使用 <paramref name="resultFunc"/> 的结果值。
    /// </summary>
    /// <typeparam name="TResult">目标异步操作结果的类型。</typeparam>
    /// <param name="this">任务完成源。不能为 <c>null</c>。</param>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <param name="resultFunc">如果任务成功完成，用于返回结果以完成任务完成源的委托。不能为 <c>null</c>。</param>
    /// <returns>如果此方法完成了任务完成源，则为 <c>true</c>；如果已经完成，则为 <c>false</c>。</returns>
    public static bool TryCompleteFromCompletedTask<TResult>(this TaskCompletionSource<TResult> @this, Task task, Func<TResult> resultFunc)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
		if (resultFunc == null)
		{
			throw new ArgumentNullException(nameof(resultFunc));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(task);
		ArgumentNullException.ThrowIfNull(resultFunc);
#endif

		if (task.IsFaulted)
		{
			return @this.TrySetException(task.Exception.InnerExceptions);
		}
        if (task.IsCanceled)
        {
            try
            {
                task.WaitAndUnwrapException();
            }
            catch (OperationCanceledException exception)
            {
                var token = exception.CancellationToken;
                return token.IsCancellationRequested ? @this.TrySetCanceled(token) : @this.TrySetCanceled();
            }
        }

        return @this.TrySetResult(resultFunc());
    }

    /// <summary>
    /// 创建一个用于异步代码的新 TCS，并强制其延续异步执行。
    /// </summary>
    /// <typeparam name="TResult">TCS 的结果类型。</typeparam>
    internal static TaskCompletionSource<TResult> CreateAsyncTaskSource<TResult>()
    {
        return new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// 异步等待任务完成，或等待取消令牌被取消。
    /// </summary>
    /// <param name="this">要等待的任务。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">用于取消等待的取消令牌。</param>
    public static Task WaitAsync(this Task @this, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		if (!cancellationToken.CanBeCanceled)
        {
            return @this;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return DoWaitAsync(@this, cancellationToken);
    }

    private static async Task DoWaitAsync(Task task, CancellationToken cancellationToken)
    {
		using (var cancelTaskSource = new CancellationTokenTaskSource<object>(cancellationToken))
		{
			await await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false);
		}
    }

    /// <summary>
    /// 异步等待任务完成，或等待取消令牌被取消。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="this">要等待的任务。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">用于取消等待的取消令牌。</param>
    public static Task<TResult> WaitAsync<TResult>(this Task<TResult> @this, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		if (!cancellationToken.CanBeCanceled)
		{
			return @this;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<TResult>(cancellationToken);
		}

		return DoWaitAsync(@this, cancellationToken);
    }

    private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
    {
        using (var cancelTaskSource = new CancellationTokenTaskSource<TResult>(cancellationToken))
		{
			return await await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false);
		}
	}

    /// <summary>
    /// 异步等待任意一个源任务完成，或等待取消令牌被取消。
    /// </summary>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">用于取消等待的取消令牌。</param>
    public static Task<Task> WhenAny(this IEnumerable<Task> @this, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAny(@this).WaitAsync(cancellationToken);
    }

    /// <summary>
    /// 异步等待任意一个源任务完成。
    /// </summary>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    public static Task<Task> WhenAny(this IEnumerable<Task> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAny(@this);
    }

    /// <summary>
    /// 异步等待任意一个源任务完成，或等待取消令牌被取消。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">用于取消等待的取消令牌。</param>
    public static Task<Task<TResult>> WhenAny<TResult>(this IEnumerable<Task<TResult>> @this, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAny(@this).WaitAsync(cancellationToken);
    }

    /// <summary>
    /// 异步等待任意一个源任务完成。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    public static Task<Task<TResult>> WhenAny<TResult>(this IEnumerable<Task<TResult>> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAny(@this);
    }

    /// <summary>
    /// 异步等待所有源任务完成。
    /// </summary>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    public static Task WhenAll(this IEnumerable<Task> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAll(@this);
    }

    /// <summary>
    /// 异步等待所有源任务完成。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="this">要等待的任务集合。不能为 <c>null</c>。</param>
    public static Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif

		return Task.WhenAll(@this);
    }

    /// <summary>
    /// 危险！忽略此任务的完成。同时忽略异常。
    /// </summary>
    /// <param name="this">要忽略的任务。</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static async void Ignore(this Task @this)
    {
        try
        {
            await @this.ConfigureAwait(false);
        }
        catch
        {
            // 忽略
        }
    }

    /// <summary>
    /// 危险！忽略此任务的完成和结果。同时忽略异常。
    /// </summary>
    /// <param name="this">要忽略的任务。</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static async void Ignore<T>(this Task<T> @this)
    {
        try
        {
            await @this.ConfigureAwait(false);
        }
        catch
        {
            // 忽略
        }
    }

    /// <summary>
    /// 创建一个按完成顺序排列的新任务集合。
    /// </summary>
    /// <typeparam name="T">任务结果的类型。</typeparam>
    /// <param name="this">要按完成顺序排序的任务集合。不能为 <c>null</c>。</param>
    public static List<Task<T>> OrderByCompletion<T>(this IEnumerable<Task<T>> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif
		// 这是 Jon Skeet 的方法和 Stephen Toub 的方法的结合：
		//  http://msmvps.com/blogs/jon_skeet/archive/2012/01/16/eduasync-part-19-ordering-by-completion-ahead-of-time.aspx
		//  http://blogs.msdn.com/b/pfxteam/archive/2012/08/02/processing-tasks-as-they-complete.aspx

		// 具体化源任务序列。TODO: 更好的具体化方式。
		var taskArray = @this.ToArray();

        // 分配 TCS 数组和结果任务数组。
        var numTasks = taskArray.Length;
        var tcs = new TaskCompletionSource<T>[numTasks];
        var ret = new List<Task<T>>(numTasks);

        // 每个任务完成时，完成下一个 tcs。
        var lastIndex = -1;
		// ReSharper disable once ConvertToLocalFunction
		void Continuation(Task<T> task)
		{
			var index = Interlocked.Increment(ref lastIndex);
			tcs[index].TryCompleteFromCompletedTask(task);
		}

		// 填充数组并附加延续任务。
		for (var i = 0; i != numTasks; ++i)
        {
            tcs[i] = new TaskCompletionSource<T>();
            ret.Add(tcs[i].Task);
            taskArray[i].ContinueWith(Continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        return ret;
    }

    /// <summary>
    /// 创建一个按完成顺序排列的新任务集合。
    /// </summary>
    /// <param name="this">要按完成顺序排序的任务集合。不能为 <c>null</c>。</param>
    public static List<Task> OrderByCompletion(this IEnumerable<Task> @this)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
#endif
		// 这是 Jon Skeet 的方法和 Stephen Toub 的方法的结合：
		//  http://msmvps.com/blogs/jon_skeet/archive/2012/01/16/eduasync-part-19-ordering-by-completion-ahead-of-time.aspx
		//  http://blogs.msdn.com/b/pfxteam/archive/2012/08/02/processing-tasks-as-they-complete.aspx

		// 具体化源任务序列。TODO: 更好的具体化方式。
		var taskArray = @this.ToArray();

        // 分配 TCS 数组和结果任务数组。
        var numTasks = taskArray.Length;
        var tcs = new TaskCompletionSource<object>[numTasks];
        var ret = new List<Task>(numTasks);

        // 每个任务完成时，完成下一个 tcs。
        var lastIndex = -1;
		// ReSharper disable once ConvertToLocalFunction
		void Continuation(Task task)
		{
			var index = Interlocked.Increment(ref lastIndex);
			tcs[index].TryCompleteFromCompletedTask(task, NullResultFunc);
		}

		// 填充数组并附加延续任务。
		for (var i = 0; i != numTasks; ++i)
        {
            tcs[i] = new TaskCompletionSource<object>();
            ret.Add(tcs[i].Task);
            taskArray[i].ContinueWith(Continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        return ret;
    }

    /// <summary>
    /// 等待任务完成，并展开任何异常。
    /// </summary>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    public static void WaitAndUnwrapException(this Task task)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// 等待任务完成，并展开任何异常。
    /// </summary>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">等待任务完成时要观察的取消令牌。</param>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 在 <paramref name="task"/> 完成之前被取消，或 <paramref name="task"/> 引发了 <see cref="OperationCanceledException"/>。</exception>
    public static void WaitAndUnwrapException(this Task task, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		try
		{
            task.Wait(cancellationToken);
        }
        catch (AggregateException ex)
        {
            throw ex.InnerException.PrepareForRethrow();
        }
    }

    /// <summary>
    /// 等待任务完成，并展开任何异常。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <returns>任务的结果。</returns>
    public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// 等待任务完成，并展开任何异常。
    /// </summary>
    /// <typeparam name="TResult">任务结果的类型。</typeparam>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">等待任务完成时要观察的取消令牌。</param>
    /// <returns>任务的结果。</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 在 <paramref name="task"/> 完成之前被取消，或 <paramref name="task"/> 引发了 <see cref="OperationCanceledException"/>。</exception>
    public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		try
		{
            task.Wait(cancellationToken);
            return task.Result;
        }
        catch (AggregateException ex)
        {
            throw ex.InnerException.PrepareForRethrow();
        }
    }

    /// <summary>
    /// 等待任务完成，但不引发任务异常。任务异常（如果存在）将不被观察。
    /// </summary>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    public static void WaitWithoutException(this Task task)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		try
        {
            task.Wait();
        }
        catch (AggregateException)
        {
        }
    }

    /// <summary>
    /// 等待任务完成，但不引发任务异常。任务异常（如果存在）将不被观察。
    /// </summary>
    /// <param name="task">任务。不能为 <c>null</c>。</param>
    /// <param name="cancellationToken">等待任务完成时要观察的取消令牌。</param>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 在 <paramref name="task"/> 完成之前被取消。</exception>
    public static void WaitWithoutException(this Task task, CancellationToken cancellationToken)
    {
#if NETSTANDARD
		if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}
#else
		ArgumentNullException.ThrowIfNull(task);
#endif

		try
		{
            task.Wait(cancellationToken);
        }
        catch (AggregateException)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static Func<object> NullResultFunc { get; } = () => null;

    #region SynchronizationContext

    /// <summary>
    /// 在此同步上下文上同步执行委托。
    /// </summary>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static void Send(this SynchronizationContext context, Action action)
    {
        context.Send(state => ((Action)state!)(), action);
    }

    /// <summary>
    /// 在此同步上下文上同步执行委托并返回其结果。
    /// </summary>
    /// <typeparam name="T">结果的类型。</typeparam>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static T Send<T>(this SynchronizationContext context, Func<T> action)
    {
        var result = default(T);
        context.Send(state =>
        {
            result = ((Func<T>)state!)();
        }, action);
        return result;
    }

    /// <summary>
    /// 在此同步上下文上异步执行委托。
    /// </summary>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static Task PostAsync(this SynchronizationContext context, Action action)
    {
        var taskCompletionSource = CreateAsyncTaskSource<object>();
        context.Post(state =>
        {
            try
            {
                ((Action)state!)();
                taskCompletionSource.TrySetResult(null);
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }
        }, action);
        return taskCompletionSource.Task;
    }

    /// <summary>
    /// 在此同步上下文上异步执行委托并返回其结果。
    /// </summary>
    /// <typeparam name="T">结果的类型。</typeparam>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static Task<T> PostAsync<T>(this SynchronizationContext context, Func<T> action)
    {
        var taskCompletionSource = CreateAsyncTaskSource<T>();
        context.Post(state =>
        {
            try
            {
                taskCompletionSource.SetResult(((Func<T>)state!)());
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }
        }, action);
        return taskCompletionSource.Task;
    }

    /// <summary>
    /// 在此同步上下文上异步执行异步委托。
    /// </summary>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static Task PostAsync(this SynchronizationContext context, Func<Task> action)
    {
        var taskCompletionSource = CreateAsyncTaskSource<object>();

        async void PostCallback(object state)
        {
            try
            {
                await ((Func<Task>)state!)().ConfigureAwait(false);
                taskCompletionSource.TrySetResult(null);
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }
        }

        context.Post(PostCallback, action);
        return taskCompletionSource.Task;
    }

    /// <summary>
    /// 在此同步上下文上异步执行异步委托并返回其结果。
    /// </summary>
    /// <typeparam name="T">结果的类型。</typeparam>
    /// <param name="context">同步上下文。</param>
    /// <param name="action">要执行的委托。</param>
    public static Task<T> PostAsync<T>(this SynchronizationContext context, Func<Task<T>> action)
    {
        var taskCompletionSource = CreateAsyncTaskSource<T>();

        async void PostCallback(object state)
        {
            try
            {
                taskCompletionSource.SetResult(await ((Func<Task<T>>)state!)().ConfigureAwait(false));
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }
        }

        context.Post(PostCallback, action);
        return taskCompletionSource.Task;
    }

    #endregion

    #region TaskFactory

    /// <summary>
    /// 将工作排队到任务工厂，并返回表示该工作的 <see cref="Task"/>。如果任务工厂未指定任务调度器，则使用线程池任务调度器。
    /// </summary>
    /// <param name="this"><see cref="TaskFactory"/> 实例。不能为 <c>null</c>。</param>
    /// <param name="action">要执行的操作委托。不能为 <c>null</c>。</param>
    /// <returns>已启动的任务。</returns>
    public static Task Run(this TaskFactory @this, Action action)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(action);
#endif

		return @this.StartNew(action, @this.CancellationToken, @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default);
    }

    /// <summary>
    /// 将工作排队到任务工厂，并返回表示该工作的 <see cref="Task{TResult}"/>。如果任务工厂未指定任务调度器，则使用线程池任务调度器。
    /// </summary>
    /// <param name="this"><see cref="TaskFactory"/> 实例。不能为 <c>null</c>。</param>
    /// <param name="action">要执行的操作委托。不能为 <c>null</c>。</param>
    /// <returns>已启动的任务。</returns>
    public static Task<TResult> Run<TResult>(this TaskFactory @this, Func<TResult> action)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}

		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(action);
#endif

		return @this.StartNew(action, @this.CancellationToken, @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default);
    }

    /// <summary>
    /// 将工作排队到任务工厂，并返回表示该工作的代理 <see cref="Task"/>。如果任务工厂未指定任务调度器，则使用线程池任务调度器。
    /// </summary>
    /// <param name="this"><see cref="TaskFactory"/> 实例。不能为 <c>null</c>。</param>
    /// <param name="action">要执行的操作委托。不能为 <c>null</c>。</param>
    /// <returns>已启动的任务。</returns>
    public static Task Run(this TaskFactory @this, Func<Task> action)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}

		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(action);
#endif

		return @this.StartNew(action, @this.CancellationToken, @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default).Unwrap();
    }

    /// <summary>
    /// 将工作排队到任务工厂，并返回表示该工作的代理 <see cref="Task{TResult}"/>。如果任务工厂未指定任务调度器，则使用线程池任务调度器。
    /// </summary>
    /// <param name="this"><see cref="TaskFactory"/> 实例。不能为 <c>null</c>。</param>
    /// <param name="action">要执行的操作委托。不能为 <c>null</c>。</param>
    /// <returns>已启动的任务。</returns>
    public static Task<TResult> Run<TResult>(this TaskFactory @this, Func<Task<TResult>> action)
    {
#if NETSTANDARD
		if (@this == null)
		{
			throw new ArgumentNullException(nameof(@this));
		}
		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}
#else
		ArgumentNullException.ThrowIfNull(@this);
		ArgumentNullException.ThrowIfNull(action);
#endif
		return @this.StartNew(action, @this.CancellationToken, @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default).Unwrap();
    }

    #endregion

    /// <summary>
    /// 将 <see cref="ValueTask{TResult}"/> 转换为 <see cref="ValueTask"/>。
    /// </summary>
    /// <typeparam name="TResult">源 ValueTask 的结果类型。</typeparam>
    /// <param name="task">要转换的 ValueTask。</param>
    /// <returns>表示异步操作的 ValueTask。</returns>
    public static async ValueTask ConvertToVoid<TResult>(this ValueTask<TResult> task) => await task.ConfigureAwait(false);

    /// <summary>
    /// 将 <see cref="Task{T}"/> 转换为 <see cref="ValueTask{T}"/>。
    /// </summary>
    /// <typeparam name="T">任务结果的类型。</typeparam>
    /// <param name="task">要转换的 Task。</param>
    /// <returns>表示异步操作的 ValueTask&lt;T&gt;。</returns>
    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new(task);

    /// <summary>
    /// 将 <see cref="Task"/> 转换为 <see cref="ValueTask"/>。
    /// </summary>
    /// <param name="task">要转换的 Task。</param>
    /// <returns>表示异步操作的 ValueTask。</returns>
    public static ValueTask AsValueTask(this Task task) => new(task);

    /// <summary>
    /// 将值包装为 <see cref="ValueTask{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="value">要包装的值。</param>
    /// <returns>包含指定值的 ValueTask&lt;T&gt;。</returns>
    public static ValueTask<T> AsValueTask<T>(this T value) => new(value);
}