using System.Runtime.InteropServices;
using System.Security;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Class DefaultAsyncTokenProvider.
/// Implements the <see cref="IAsyncTokenProvider" />
/// </summary>
/// <seealso cref="IAsyncTokenProvider" />
public class DefaultAsyncTokenProvider : IAsyncTokenProvider
{
    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>IVolatileToken.</returns>
    public IVolatileToken GetToken(Action<Action<IVolatileToken>> task)
    {
        var token = new VolatileToken(task);
        token.QueueWorkItem();
        return token;
    }

    private class VolatileToken : IVolatileToken
    {
        /// <summary>
        /// The task
        /// </summary>
        private readonly Action<Action<IVolatileToken>> _task;
        /// <summary>
        /// The task tokens
        /// </summary>
        private readonly List<IVolatileToken> _taskTokens = new();
        /// <summary>
        /// 保护 <see cref="_taskTokens"/> 的同步对象。
        /// </summary>
        private readonly object _lock = new();
        /// <summary>
        /// The task exception
        /// </summary>
        private volatile Exception _taskException;
        /// <summary>
        /// The is task finished
        /// </summary>
        private volatile bool _isTaskFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolatileToken"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        public VolatileToken(Action<Action<IVolatileToken>> task)
        {
            _task = task;
        }

        /// <summary>
        /// Queues the work item.
        /// </summary>
        public void QueueWorkItem()
        {
            // Start a work item to collect tokens in our internal array
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
        /// Gets a value indicating whether this instance is current.
        /// </summary>
        /// <value><c>true</c> if this instance is current; otherwise, <c>false</c>.</value>
        public bool IsCurrent
        {
            get
            {
                // We are current until the task has finished
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
