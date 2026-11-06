using System.Runtime.InteropServices;
using System.Security;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="DefaultAsyncTokenProvider"/> 类。
/// 实现 <see cref="IAsyncTokenProvider" />。
/// </summary>
/// <seealso cref="IAsyncTokenProvider" />
public class DefaultAsyncTokenProvider : IAsyncTokenProvider
{
    /// <summary>
    /// 获取令牌。
    /// </summary>
    /// <param name="task">任务。</param>
    /// <returns>IVolatileToken。</returns>
    public IVolatileToken GetToken(Action<Action<IVolatileToken>> task)
    {
        var token = new VolatileToken(task);
        token.QueueWorkItem();
        return token;
    }

    /// <summary>
    /// 在线程池上执行令牌收集委托的易变令牌：任务完成前始终视为有效，完成后以收集到的子令牌状态为准。
    /// </summary>
    private class VolatileToken : IVolatileToken
    {
        /// <summary>
        /// 用于启动异步令牌生成任务的回调。
        /// </summary>
        private readonly Action<Action<IVolatileToken>> _task;
        /// <summary>
        /// 任务令牌集合。
        /// </summary>
        private readonly List<IVolatileToken> _taskTokens = new();
        /// <summary>
        /// 保护 <see cref="_taskTokens"/> 的同步对象。
        /// </summary>
        private readonly object _lock = new();
        /// <summary>
        /// 任务异常。
        /// </summary>
        private volatile Exception? _taskException;
        /// <summary>
        /// 任务是否已完成。
        /// </summary>
        private volatile bool _isTaskFinished;

        /// <summary>
        /// 初始化 <see cref="VolatileToken"/> 类的新实例。
        /// </summary>
        /// <param name="task">任务。</param>
        public VolatileToken(Action<Action<IVolatileToken>> task)
        {
            _task = task;
        }

        /// <summary>
        /// 将工作项排队。
        /// </summary>
        public void QueueWorkItem()
        {
            // 启动一个工作项，将令牌收集到内部数组中
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _task(token =>
                    {
                        lock (_lock)
                        {
                            _taskTokens.Add(token);
                        }
                    });
                }
                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case SecurityException _:
                        case StackOverflowException _:
                        case OutOfMemoryException _:
                        case AccessViolationException _:
                        case AppDomainUnloadedException _:
                        case ThreadAbortException _:
                        case SEHException _:
                            throw;
                    }

                    _taskException = ex;
                }
                finally
                {
                    _isTaskFinished = true;
                }
            });
        }

        /// <summary>
        /// 获取一个值，指示此实例是否为当前状态。
        /// </summary>
        /// <value>若此实例为当前状态，则为 <c>true</c>；否则为 <c>false</c>。</value>
        public bool IsCurrent
        {
            get
            {
                // 任务完成前一直视为当前状态
                if (_taskException != null)
                {
                    return false;
                }

                if (_isTaskFinished)
                {
                    // 在锁内对集合做快照后再遍历，避免与后台写入并发导致异常
                    IVolatileToken[] snapshot;
                    lock (_lock)
                    {
                        snapshot = [.. _taskTokens];
                    }

                    return snapshot.All(t => t.IsCurrent);
                }

                return true;
            }
        }
    }
}
