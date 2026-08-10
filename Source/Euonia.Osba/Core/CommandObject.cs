namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 命令对象。
/// </summary>
/// <typeparam name="T">命令对象的具体类型。</typeparam>
public abstract class CommandObject<T> : BusinessObject<T>, ICommandObject
	where T : CommandObject<T>
{
	/// <summary>
	/// 执行命令。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步命令执行操作的任务。</returns>
	protected internal virtual Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 创建新的命令对象。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步创建操作的任务。</returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}