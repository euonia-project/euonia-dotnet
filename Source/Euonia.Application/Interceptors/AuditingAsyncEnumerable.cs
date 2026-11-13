using System.Runtime.CompilerServices;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 包装异步流：枚举完成时封口成功，枚举异常时记录失败。
/// </summary>
internal sealed class AuditingAsyncEnumerable<T> : IAsyncEnumerable<T>
{
	private readonly IAsyncEnumerable<T> _inner;
	private readonly IAuditingAction _action;
	private int _sealed;

	public AuditingAsyncEnumerable(IAsyncEnumerable<T> inner, IAuditingAction action)
	{
		_inner = inner;
		_action = action;
	}

	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		var enumerator = _inner.GetAsyncEnumerator(cancellationToken);
		try
		{
			while (true)
			{
				bool moved;
				try
				{
					moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
				}
				catch (Exception exception)
				{
					if (Interlocked.Exchange(ref _sealed, 1) == 0)
					{
						_action.Fail(exception);
					}

					throw;
				}

				if (!moved)
				{
					break;
				}

				yield return enumerator.Current;
			}

			if (Interlocked.Exchange(ref _sealed, 1) == 0)
			{
				_action.Complete(null);
			}
		}
		finally
		{
			await enumerator.DisposeAsync().ConfigureAwait(false);

			// 调用方提前停止枚举（break/Dispose）时也要封口，避免作用域永远不提交。
			if (Interlocked.Exchange(ref _sealed, 1) == 0)
			{
				_action.Complete(null);
			}
		}
	}
}
