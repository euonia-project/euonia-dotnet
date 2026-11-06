using System.ComponentModel;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// A <see langword="class"/> for request messages that can receive multiple replies, which can either be used directly or through derived classes.
/// </summary>
/// <typeparam name="T">The type of request to make.</typeparam>
public class AsyncCollectionRequestMessage<T> : IAsyncEnumerable<T>
{
	/// <summary>
	/// The collection of received replies. We accept both <see cref="Task{TResult}"/> instance, representing already running
	/// operations that can be executed in parallel, or <see cref="Func{T,TResult}"/> instances, which can be used so that multiple
	/// asynchronous operations are only started sequentially from <see cref="GetAsyncEnumerator"/> and do not overlap in time.
	/// </summary>
	private readonly List<(Task<T>?, Func<CancellationToken, Task<T>>?)> _responses = new();

	/// <summary>
	/// The <see cref="CancellationTokenSource"/> instance used to link the token passed to
	/// <see cref="GetAsyncEnumerator"/> and the one passed to all subscribers to the message.
	/// </summary>
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	/// <summary>
	/// Gets the <see cref="System.Threading.CancellationToken"/> instance that will be linked to the
	/// one used to asynchronously enumerate the received responses. This can be used to cancel asynchronous
	/// replies that are still being processed, if no new items are needed from this request message.
	/// Consider the following example, where we define a message to retrieve the currently opened documents:
	/// <code>
	/// public class OpenDocumentsRequestMessage : AsyncCollectionRequestMessage&lt;XmlDocument&gt; { }
	/// </code>
	/// We can then request and enumerate the results like so:
	/// <code>
	/// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;())
	/// {
	///     // Process each document here...
	/// }
	/// </code>
	/// If we also want to control the cancellation of the token passed to each subscriber to the message,
	/// we can do so by passing a token we control to the returned message before starting the enumeration
	/// (<see cref="TaskAsyncEnumerableExtensions.WithCancellation{T}"/>).
	/// The previous snippet with this additional change looks as follows:
	/// <code>
	/// await foreach (var document in Messenger.Default.Send&lt;OpenDocumentsRequestMessage&gt;().WithCancellation(cts.Token))
	/// {
	///     // Process each document here...
	/// }
	/// </code>
	/// When no more new items are needed (or for any other reason depending on the situation), the token
	/// passed to the enumerator can be canceled (by calling <see cref="CancellationTokenSource.Cancel()"/>),
	/// and that will also notify the remaining tasks in the request message. The token exposed by the message
	/// itself will automatically be linked and canceled with the one passed to the enumerator.
	/// </summary>
	public CancellationToken CancellationToken => _cancellationTokenSource.Token;
	
	/// <summary>
	/// Replies to the current request message.
	/// </summary>
	/// <param name="response">The response to use to reply to the request message.</param>
	public void Reply(T response)
	{
		Reply(Task.FromResult(response));
	}

	/// <summary>
	/// Replies to the current request message.
	/// </summary>
	/// <param name="response">The response to use to reply to the request message.</param>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="response"/> is <see langword="null"/>.</exception>
	public void Reply(Task<T> response)
	{
		ArgumentAssert.ThrowIfNull(response);

		_responses.Add((response, null));
	}

	/// <summary>
	/// Replies to the current request message.
	/// </summary>
	/// <param name="response">The response to use to reply to the request message.</param>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="response"/> is <see langword="null"/>.</exception>
	public void Reply(Func<CancellationToken, Task<T>> response)
	{
		ArgumentAssert.ThrowIfNull(response);

		_responses.Add((null, response));
	}

	/// <summary>
	/// Gets the collection of received response items.
	/// </summary>
	/// <param name="cancellationToken">A <see cref="System.Threading.CancellationToken"/> value to stop the operation.</param>
	/// <returns>The collection of received response items.</returns>
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