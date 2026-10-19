namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 后台任务的统一启动入口。
/// </summary>
internal static class BackgroundExecution
{
	/// <summary>
	/// 在不继承创建方 <see cref="ExecutionContext"/> 的后台任务中执行操作。
	/// </summary>
	/// <param name="operation">要执行的操作。</param>
	/// <remarks>
	/// <see cref="System.Threading.Timer"/> 在创建时捕获 <see cref="ExecutionContext"/>；
	/// 若创建发生在某个请求内，后台轮询会继承该请求的异步状态并解析到已释放的作用域。
	/// 后台重试与请求无关，因此统一抑制执行流。
	/// </remarks>
	public static void Run(Func<Task> operation)
	{
		ArgumentNullException.ThrowIfNull(operation);

		using (ExecutionContext.SuppressFlow())
		{
			_ = Task.Run(operation);
		}
	}
}
