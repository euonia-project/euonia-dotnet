using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 为消息处理安装审计作用域：处理器结束后提交一条记录，消息标识作为动作幂等键。
/// </summary>
public sealed class AuditingMessageScopeParticipant : IMessageExecutionScopeParticipant
{
	/// <inheritdoc />
	public int Order => 200;

	/// <inheritdoc />
	public ValueTask<MessageExecutionScopeEntry> EnterAsync(IMessageContext message, IServiceProvider services, CancellationToken cancellationToken = default)
	{
		var recorder = services.GetService<IAuditingRecorder>();
		if (recorder is not { IsEnabled: true })
		{
			return ValueTask.FromResult(new MessageExecutionScopeEntry());
		}

		var scope = recorder.EnsureScope(new AuditingScopeOptions
		{
			OperationKey = $"msg:{message.MessageId}",
			IdempotencyKey = message.MessageId,
			AutoComplete = false,
		});

		Func<IDisposable?> install = () => new ScopeLease(scope, message);
		return ValueTask.FromResult(new MessageExecutionScopeEntry(install));
	}

	/// <summary>
	/// 作用域租约：记录消息处理结果并完成审计作用域。
	/// </summary>
	private sealed class ScopeLease : IDisposable
	{
		private readonly IAuditingScope _scope;
		private readonly MessageContext? _context;
		private readonly EventHandler<Exception>? _onFailed;
		private readonly EventHandler<MessageHandledEventArgs>? _onCompleted;
		private bool _disposed;

		public ScopeLease(IAuditingScope scope, IMessageContext message)
		{
			_scope = scope;
			_context = message as MessageContext;
			if (_context == null)
			{
				return;
			}

			_onFailed = (_, exception) =>
			{
				_scope.Record.IsSuccess = false;
				_scope.Record.ExtraProperties = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
				{
					["error"] = exception.GetType().Name,
					["message"] = exception.Message,
				});
			};

			_onCompleted = (_, args) => _scope.Record.IsSuccess ??= true;

			_context.Failed += _onFailed;
			_context.Completed += _onCompleted;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (_context != null)
			{
				_context.Failed -= _onFailed;
				_context.Completed -= _onCompleted;
			}

			// 作用域在消息结束后完成；提交走后台队列，不阻塞接收器。
			_ = _scope.CompleteAsync(CancellationToken.None);
		}
	}
}
