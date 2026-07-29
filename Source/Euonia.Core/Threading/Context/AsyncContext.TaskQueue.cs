using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Threading;

public sealed partial class AsyncContext
{
    /// <summary>
    /// 一个阻塞队列。
    /// </summary>
    private sealed class TaskQueue : IDisposable
    {
        /// <summary>
        /// 底层的阻塞集合。
        /// </summary>
        private readonly BlockingCollection<Tuple<Task, bool>> _queue;

        /// <summary>
        /// 初始化 <see cref="TaskQueue"/> 类的新实例。
        /// </summary>
        public TaskQueue()
        {
            _queue = new BlockingCollection<Tuple<Task, bool>>();
        }

        /// <summary>
        /// 获取一个阻塞枚举器，用于从队列中移除项。此枚举器仅在调用 <see cref="CompleteAdding"/> 之后才会完成。
        /// </summary>
        /// <returns>一个阻塞枚举器，用于从队列中移除项。</returns>
        public IEnumerable<Tuple<Task, bool>> GetConsumingEnumerable()
        {
            return _queue.GetConsumingEnumerable();
        }

        /// <summary>
        /// 生成当前排队等待调度器执行的 <see cref="T:System.Threading.Tasks.Task"/> 实例的枚举。
        /// </summary>
        /// <returns>允许遍历当前排队等待此调度器的任务的枚举。</returns>
        [System.Diagnostics.DebuggerNonUserCode]
        internal IEnumerable<Task> GetScheduledTasks()
        {
            foreach (var item in _queue)
                yield return item.Item1;
        }

        /// <summary>
        /// 尝试将项添加到队列中。如果队列已被标记为已完成添加，则此方法返回 <c>false</c>。
        /// </summary>
        /// <param name="item">要入队的项。</param>
        /// <param name="propagateExceptions">一个值，指示此任务上的异常是否应传播到主循环之外。</param>
        public bool TryAdd(Task item, bool propagateExceptions)
        {
            try
            {
                return _queue.TryAdd(Tuple.Create(item, propagateExceptions));
            }
            catch (InvalidOperationException)
            {
                // 令人烦恼的异常
                return false;
            }
        }

        /// <summary>
        /// 将队列标记为已完成添加，允许从 <see cref="GetConsumingEnumerable"/> 返回的枚举器最终完成。此方法可多次调用。
        /// </summary>
        public void CompleteAdding()
        {
            _queue.CompleteAdding();
        }

        /// <summary>
        /// 执行与释放、释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            _queue.Dispose();
        }
    }
}