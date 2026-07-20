namespace Nerosoft.Euonia.Threading;

public sealed partial class AsyncContext
{
	/// <summary>
	/// <see cref="AsyncContext"/> 使用的 <see cref="SynchronizationContext"/> 实现。
	/// </summary>
	private sealed class AsyncContextSynchronizationContext : SynchronizationContext
	{
		/// <summary>
		/// 初始化 <see cref="AsyncContextSynchronizationContext"/> 类的新实例。
		/// </summary>
		/// <param name="context">异步上下文。</param>
		public AsyncContextSynchronizationContext(AsyncContext context)
		{
			Context = context;
		}

		/// <summary>
		/// 获取异步上下文。
		/// </summary>
		public AsyncContext Context { get; }

		/// <summary>
		/// 将异步消息分发到异步上下文。如果所有任务已完成且未完成的异步操作计数为零，则此方法的行为是未定义的。
		/// </summary>
		/// <param name="d">要调用的 <see cref="T:System.Threading.SendOrPostCallback"/> 委托。不能为 <c>null</c>。</param>
		/// <param name="state">传递给委托的对象。</param>
		public override void Post(SendOrPostCallback d, object state)
		{
			Context.Enqueue(Context._taskFactory.Run(() => d(state)), true);
		}

		/// <summary>
		/// 将异步消息分发到异步上下文，并等待其完成。
		/// </summary>
		/// <param name="d">要调用的 <see cref="T:System.Threading.SendOrPostCallback"/> 委托。不能为 <c>null</c>。</param>
		/// <param name="state">传递给委托的对象。</param>
		public override void Send(SendOrPostCallback d, object state)
		{
			if (AsyncContext.Current == Context)
			{
				d(state);
			}
			else
			{
				var task = Context._taskFactory.Run(() => d(state));
				task.WaitAndUnwrapException();
			}
		}

		/// <summary>
		/// 通过增加未完成的异步操作计数来响应操作已启动的通知。
		/// </summary>
		public override void OperationStarted()
		{
			Context.OperationStarted();
		}

		/// <summary>
		/// 通过减少未完成的异步操作计数来响应操作已完成的通知。
		/// </summary>
		public override void OperationCompleted()
		{
			Context.OperationCompleted();
		}

		/// <summary>
		/// 创建同步上下文的副本。
		/// </summary>
		/// <returns>一个新的 <see cref="T:System.Threading.SynchronizationContext"/> 对象。</returns>
		public override SynchronizationContext CreateCopy()
		{
			return new AsyncContextSynchronizationContext(Context);
		}

		/// <summary>
		/// 返回此实例的哈希码。
		/// </summary>
		/// <returns>此实例的哈希码，适用于哈希算法和哈希表等数据结构。</returns>
		public override int GetHashCode()
		{
			return Context.GetHashCode();
		}

		/// <summary>
		/// 确定指定的 <see cref="object"/> 是否等于此实例。如果它引用与此实例相同的底层异步上下文，则认为相等。
		/// </summary>
		/// <param name="obj">要与此实例比较的 <see cref="object"/>。</param>
		/// <returns>如果指定的 <see cref="object"/> 等于此实例，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
		public override bool Equals(object obj)
		{
			if (obj is not AsyncContextSynchronizationContext other)
			{
				return false;
			}
			return (Context == other.Context);
		}
	}
}