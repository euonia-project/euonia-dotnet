using System.Reflection;
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

		var options = new AuditingScopeOptions
		{
			OperationKey = $"msg:{message.MessageId}",
			IdempotencyKey = message.MessageId,
			AutoComplete = false,
		};

		// EnsureScope 会修改 AsyncLocal；必须放到 Install 中，由执行作用域在调用方同步执行，
		// 否则处理器内的审计拦截器看不到该作用域，会另建作用域并产生重复记录。
		Func<IDisposable?> install = () =>
		{
			// 消息作用域必须自有：复用外层 HTTP 作用域会导致它被提前提交（劫持）。
			var previous = recorder.CurrentScope;
			var scope = recorder.CreateDetachedScope(options);
			return new ScopeLease(scope, message, recorder, previous);
		};
		return ValueTask.FromResult(new MessageExecutionScopeEntry(install));
	}

	/// <summary>
	/// 作用域租约：记录消息处理结果并完成审计作用域。
	/// </summary>
	private sealed class ScopeLease : IDisposable
	{
		private readonly IAuditingScope _scope;
		private readonly IAuditingRecorder _recorder;
		private readonly IAuditingScope? _previous;
		private readonly IMessageContext _message;
		private readonly EventInfo? _failedEvent;
		private readonly EventInfo? _completedEvent;
		private readonly Delegate? _onFailed;
		private readonly Delegate? _onCompleted;
		private bool _disposed;

		public ScopeLease(IAuditingScope scope, IMessageContext message, IAuditingRecorder recorder, IAuditingScope? previous)
		{
			_scope = scope;
			_recorder = recorder;
			_previous = previous;
			_message = message;

			// 不向下转型具体 MessageContext：任何实现只要暴露 Failed/Completed 事件即可获得终态。
			_failedEvent = message.GetType().GetEvent("Failed");
			_completedEvent = message.GetType().GetEvent("Completed");

			if (_failedEvent?.EventHandlerType != null)
			{
				_onFailed = Delegate.CreateDelegate(_failedEvent.EventHandlerType, this, nameof(OnFailed), ignoreCase: false, throwOnBindFailure: false);
				if (_onFailed == null)
				{
					System.Diagnostics.Trace.TraceWarning($"Auditing participant cannot bind Failed event of '{message.GetType().FullName}'; failure outcome will not be recorded.");
				}
			}

			if (_completedEvent?.EventHandlerType != null)
			{
				_onCompleted = Delegate.CreateDelegate(_completedEvent.EventHandlerType, this, nameof(OnCompleted), ignoreCase: false, throwOnBindFailure: false);
			}

			if (_onFailed != null)
			{
				_failedEvent!.AddEventHandler(message, _onFailed);
			}

			if (_onCompleted != null)
			{
				_completedEvent!.AddEventHandler(message, _onCompleted);
			}
		}

		private void OnFailed(object? sender, Exception exception)
		{
			_scope.Record.IsSuccess = false;
			_scope.Record.MergeExtraProperties(new Dictionary<string, string>
			{
				["error"] = exception.GetType().Name,
				["message"] = AuditingRecordExtensions.MaskExceptionMessage(exception.Message) ?? string.Empty,
			});
		}

		private void OnCompleted(object? sender, MessageHandledEventArgs args)
		{
			_scope.Record.IsSuccess ??= true;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (_onFailed != null)
			{
				_failedEvent!.RemoveEventHandler(_message, _onFailed);
			}

			if (_onCompleted != null)
			{
				_completedEvent!.RemoveEventHandler(_message, _onCompleted);
			}

			// 先同步分离再从后台完成，避免已完成的 scope 残留在调用方的 AsyncLocal 中；
			// 随后恢复外层作用域（如果存在）。
			_recorder.CompleteInBackground(_scope);
			_recorder.Attach(_previous);
		}
	}
}
