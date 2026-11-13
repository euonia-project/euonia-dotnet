using System.ComponentModel;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 一个 <see langword="class"/>，用于可接收多个回复的请求消息，既可以直接使用，也可以通过派生类使用。
/// </summary>
/// <typeparam name="T">要发出的请求类型。</typeparam>
public class AsyncCollectionRequestMessage<T> : IAsyncEnumerable<T>
{
	/// <summary>
	/// 已接收回复的集合。我们同时接受 <see cref="Task{TResult}"/> 实例（表示已在运行、
	/// 可以并行执行的操作）和 <see cref="Func{T,TResult}"/> 实例（可用于确保多个异步操作
	/// 仅从 <see cref="GetAsyncEnumerator"/> 顺序启动，并且在时间上不重叠）。
	/// </summary>
	private readonly List<(Task<T>?, Func<CancellationToken, Task<T>>?)> _responses = new();

	/// <summary>
	/// 用于将传递给 <see cref="GetAsyncEnumerator"/> 的令牌与传递给消息所有订阅方的
	/// 令牌关联起来的 <see cref="CancellationTokenSource"/> 实例。
	/// </summary>
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	/// <summary>
	/// 获取将与异步枚举已接收响应所用令牌关联的 <see cref="System.Threading.CancellationToken"/>
	/// 实例。如果不再需要从此请求消息获取新项，可用于取消仍在处理的异步回复。
	/// 考虑以下示例，我们定义一条消息来检索当前打开的文档：
	/// <code>
	/// public class OpenDocumentsRequestMessage : AsyncCollectionRequestMessage&lt;XmlDocument&gt; { }
	/// </code>
	/// 然后我们可以像这样请求并枚举结果：
	/// <code>
	/// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;())
	/// {
	///     // Process each document here...
	/// }
	/// </code>
	/// 如果我们还想控制传递给消息每个订阅方的令牌的取消，可以在开始枚举之前
	/// 将一个由我们控制的令牌传递给返回的消息（<see cref="TaskAsyncEnumerableExtensions.WithCancellation{T}"/>）。
	/// 加上这一改动后的前面代码片段如下所示：
	/// <code>
	/// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;().WithCancellation(cts.Token))
	/// {
	///     // Process each document here...
	/// }
	/// </code>
	/// 当不再需要新项时（或根据具体情况出于任何其他原因），可以取消传递给枚举器的
	/// 令牌（通过调用 <see cref="CancellationTokenSource.Cancel()"/>），这也会通知请求消息中
	/// 剩余的任务。消息本身公开的令牌将自动与传递给枚举器的令牌关联并一起取消。
	/// </summary>
	public CancellationToken CancellationToken => _cancellationTokenSource.Token;
	
	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复该请求消息的响应。</param>
	public void Reply(T response)
	{
		Reply(Task.FromResult(response));
	}

	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复该请求消息的响应。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="response"/> 为 <see langword="null"/> 时引发。</exception>
	public void Reply(Task<T> response)
	{
		ArgumentNullException.ThrowIfNull(response);

		_responses.Add((response, null));
	}

	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复该请求消息的响应。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="response"/> 为 <see langword="null"/> 时引发。</exception>
	public void Reply(Func<CancellationToken, Task<T>> response)
	{
		ArgumentNullException.ThrowIfNull(response);

		_responses.Add((null, response));
	}

	/// <summary>
	/// 获取已接收响应项的集合。
	/// </summary>
	/// <param name="cancellationToken">用于停止操作的 <see cref="System.Threading.CancellationToken"/> 值。</param>
	/// <returns>已接收响应项的集合。</returns>
	public async Task<IReadOnlyCollection<T>> GetResponsesAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.CanBeCanceled)
		{
			_ = cancellationToken.Register(_cancellationTokenSource.Cancel);
		}

		List<T> results = new(_responses.Count);

		await foreach (T response in this.WithCancellation(cancellationToken).ConfigureAwait(false))
		{
			results.Add(response);
		}

		return results;
	}

	/// <inheritdoc/>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.CanBeCanceled)
		{
			_ = cancellationToken.Register(_cancellationTokenSource.Cancel);
		}

		foreach (var (task, func) in _responses)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				yield break;
			}

			if (task is not null)
			{
				yield return await task.ConfigureAwait(false);
			}
			else
			{
				yield return await func!(cancellationToken).ConfigureAwait(false);
			}
		}
	}
}