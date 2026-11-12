using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 基于执行上下文的审计记录器。
/// </summary>
/// <remarks>
/// 一个异步流最多一个开启作用域；作用域完成时提交一条记录，动作列表允许为空。
/// </remarks>
public sealed class AuditingRecorder : IAuditingRecorder, IAsyncDisposable
{
	private readonly AsyncLocal<AuditingScope?> _current = new();

	private readonly AuditingOptions _options;
	private readonly IAuditingWriter? _writer;
	private readonly ILogger<AuditingRecorder>? _logger;
	private readonly ConcurrentDictionary<long, Task> _background = new();
	private long _backgroundId;

	/// <summary>
	/// 初始化 <see cref="AuditingRecorder"/> 的新实例。
	/// </summary>
	/// <param name="options">审计选项。</param>
	/// <param name="writer">审计写入器；未注册时为 <c>null</c>，记录会被丢弃并记录一次警告。</param>
	/// <param name="logger">日志记录器。</param>
	public AuditingRecorder(AuditingOptions options, IAuditingWriter? writer = null, ILogger<AuditingRecorder>? logger = null)
	{
		_options = options;
		_writer = writer;
		_logger = logger;
	}

	/// <inheritdoc />
	public bool IsEnabled => _options.Enabled;

	/// <inheritdoc />
	public IAuditingScope? CurrentScope => _current.Value;

	/// <inheritdoc />
	public IAuditingScope EnsureScope(AuditingScopeOptions? options = null)
	{
		return EnsureScope(options, force: false);
	}

	/// <inheritdoc />
	public IAuditingScope EnsureScope(AuditingScopeOptions? options, bool force)
	{
		if (!force && !IsEnabled)
		{
			return NullAuditingScope.Instance;
		}

		var existing = _current.Value;
		if (existing is { IsCompleted: true })
		{
			// 已完成的作用域可能仍留在调用方 AsyncLocal 中（完成发生在子异步流）；不得复用。
			_current.Value = null;
			existing = null;
		}

		if (existing != null)
		{
			return existing;
		}

		var scopeOptions = options ?? new AuditingScopeOptions();
		var scope = new AuditingScope(this, _options, scopeOptions, BuildScopeId(scopeOptions));
		_current.Value = scope;
		return scope;
	}

	/// <inheritdoc />
	public IAuditingScope CreateScope(AuditingScopeOptions? options = null)
	{
		if (!IsEnabled)
		{
			return NullAuditingScope.Instance;
		}

		if (_current.Value is { IsCompleted: false } previous)
		{
			// CreateScope 语义是“替换当前作用域”：旧作用域不能静默悬挂，否则记录永不提交。
			_logger?.LogWarning("Replacing active auditing scope {ScopeId}; completing it in the background.", previous.ScopeId);
			CompleteInBackground(previous);
		}

		return CreateDetachedScope(options);
	}

	/// <inheritdoc />
	public IAuditingScope CreateDetachedScope(AuditingScopeOptions? options = null)
	{
		if (!IsEnabled)
		{
			return NullAuditingScope.Instance;
		}

		var scopeOptions = options ?? new AuditingScopeOptions();
		var scope = new AuditingScope(this, _options, scopeOptions, BuildScopeId(scopeOptions));
		_current.Value = scope;
		return scope;
	}

	/// <inheritdoc />
	public void Attach(IAuditingScope? scope)
	{
		if (scope is AuditingScope { IsCompleted: false } concrete && _current.Value == null)
		{
			_current.Value = concrete;
		}
	}

	/// <summary>
	/// 从执行上下文回填记录字段。
	/// </summary>
	/// <param name="record">目标记录。</param>
	internal void ApplyContext(AuditingRecord record)
	{
		var context = AmbientExecutionContext.Current;
		if (context != null)
		{
			record.UserId ??= context.UserId;
			record.UserName ??= context.UserName;
			record.TenantId ??= context.ActorTenantId ?? context.TenantId;
			record.TenantName ??= context.TenantName;
			record.TargetTenantId ??= context.TargetTenantId;
			record.IsCrossTenant = record.IsCrossTenant || context.IsCrossTenant;
			record.CrossTenantReason ??= context.CrossTenantReason;
			record.CorrelationId ??= context.CorrelationId;
			record.RequestTraceId ??= context.RequestTraceId;
		}

		var activity = Activity.Current;
		if (activity != null)
		{
			record.TraceId ??= activity.TraceId.ToHexString();
			record.SpanId ??= activity.SpanId.ToHexString();
		}
	}

	/// <inheritdoc />
	public void Detach(IAuditingScope scope)
	{
		ArgumentNullException.ThrowIfNull(scope);

		if (ReferenceEquals(_current.Value, scope))
		{
			_current.Value = null;
		}
	}

	/// <inheritdoc />
	public void CompleteInBackground(IAuditingScope scope)
	{
		ArgumentNullException.ThrowIfNull(scope);

		// 必须先同步分离：CompleteAsync 的继续执行流不会把 AsyncLocal 清理回流到调用方。
		Detach(scope);

		var id = Interlocked.Increment(ref _backgroundId);

		// 先登记占位再启动任务：FlushAsync 不会在“Detach 之后、登记之前”的窗口漏掉这条记录。
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_background[id] = completion.Task;
		_ = CompleteTrackedAsync(scope, id, completion);
	}

	private async Task CompleteTrackedAsync(IAuditingScope scope, long id, TaskCompletionSource completion)
	{
		try
		{
			await CompleteDetachedAsync(scope).ConfigureAwait(false);
			completion.TrySetResult();
		}
		catch (Exception exception)
		{
			completion.TrySetException(exception);
		}
		finally
		{
			_background.TryRemove(id, out _);
		}
	}

	private async Task CompleteDetachedAsync(IAuditingScope scope)
	{
		try
		{
			await scope.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to complete auditing scope {ScopeId} in background.", scope.ScopeId);
		}
	}

	/// <inheritdoc />
	public async ValueTask SubmitAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		if (record.RecordedAt == default)
		{
			record.RecordedAt = DateTime.UtcNow;
		}

		if (_writer == null)
		{
			_logger?.LogWarning("Auditing is enabled but no IAuditingWriter is registered; the record {ScopeId} was dropped.", record.ScopeId);
			return;
		}

		try
		{
			await _writer.WriteAsync(record, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			// 审计失败开放：只记录日志，不影响业务结果。
			_logger?.LogError(exception, "Failed to enqueue auditing record {ScopeId}.", record.ScopeId);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		// 宿主停机：先等待后台完成，再交给 writer 的 StopAsync 排空队列。
		await FlushAsync(CancellationToken.None).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task FlushAsync(CancellationToken cancellationToken = default)
	{
		// 后台完成可能尚未入队（spool 持久化会先 await）：先等待这些任务，再刷新 writer。
		while (!_background.IsEmpty)
		{
			await Task.WhenAll(_background.Values.ToArray()).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		if (_writer != null)
		{
			await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// 提交作用域记录并清理当前上下文。
	/// </summary>
	internal async ValueTask SubmitScopeAsync(AuditingScope scope, CancellationToken cancellationToken)
	{
		try
		{
			await SubmitAsync(scope.Record, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			if (ReferenceEquals(_current.Value, scope))
			{
				_current.Value = null;
			}
		}
	}

	private string BuildScopeId(AuditingScopeOptions options)
	{
		var actorTenantId = AmbientExecutionContext.Current?.ActorTenantId
		                    ?? AmbientExecutionContext.Current?.TenantId;
		var operationKey = options.OperationKey ?? Guid.CreateVersion7().ToString("N");
		var raw = $"{actorTenantId ?? "-"}:{operationKey}";
		if (raw.Length <= 256)
		{
			return raw;
		}

		// ScopeId 列宽 256：超长 OperationKey（消息 id 等）截断并追加稳定哈希，避免唯一性丢失。
		var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)))[..16]
		                  .ToLowerInvariant();
		return $"{raw[..(256 - 1 - hash.Length)]}:{hash}";
	}
}
