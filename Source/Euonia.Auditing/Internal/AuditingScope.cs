using System.Diagnostics;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计作用域实现：动作列表、幂等封口与提交。
/// </summary>
internal sealed class AuditingScope : IAuditingScope
{
	private readonly AuditingRecorder _recorder;
	private readonly AuditingOptions _options;
	private readonly AuditingScopeOptions _scopeOptions;
	private readonly Stopwatch _stopwatch;
	private readonly object _sync = new();
	private readonly Stack<Guid> _openActions = new();
	private int _completed;

	public AuditingScope(AuditingRecorder recorder, AuditingOptions options, AuditingScopeOptions scopeOptions, string scopeId)
	{
		_recorder = recorder;
		_options = options;
		_scopeOptions = scopeOptions;
		_stopwatch = Stopwatch.StartNew();

		ScopeId = scopeId;
		Record = new AuditingRecord
		{
			Id = AuditingRecordFactory.NewId(),
			ScopeId = scopeId,
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
			ApplicationName = scopeOptions.ApplicationName ?? options.ApplicationName,
			HttpMethod = scopeOptions.HttpMethod,
			Url = scopeOptions.Url,
			ClientIpAddress = scopeOptions.ClientIpAddress,
			ClientName = scopeOptions.ClientName,
			BrowserInfo = scopeOptions.BrowserInfo,
		};

		_recorder.ApplyContext(Record);
	}

	public string ScopeId { get; }

	public AuditingRecord Record { get; }

	public IAuditingAction BeginAction(string serviceName, string methodName, string? actionName = null)
	{
		ArgumentNullException.ThrowIfNull(serviceName);
		ArgumentNullException.ThrowIfNull(methodName);

		Guid? parentActionId;
		lock (_sync)
		{
			parentActionId = _openActions.Count > 0 ? _openActions.Peek() : null;
		}

		_recorder.ApplyContext(Record);

		var idempotencyKey = _scopeOptions.IdempotencyKey ?? $"{ScopeId}:{Guid.CreateVersion7():N}";
		var action = new AuditingActionImpl(
			AuditingRecordFactory.NewId(),
			parentActionId,
			serviceName,
			methodName,
			actionName,
			idempotencyKey,
			_options,
			OnActionSealed);

		lock (_sync)
		{
			_openActions.Push(action.ActionId);
		}

		return action;
	}

	private void OnActionSealed(AuditingActionImpl action, AuditingAction record)
	{
		lock (_sync)
		{
			if (_openActions.Count > 0 && _openActions.Peek() == action.ActionId)
			{
				_openActions.Pop();
			}

			Record.Actions.Add(record);

			if (_openActions.Count > 0 || !_scopeOptions.AutoComplete)
			{
				return;
			}
		}

		// 自动完成不得阻塞调用方：提交走后台队列。
		_ = CompleteAsync(CancellationToken.None);
	}

	public async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
	{
		if (Interlocked.Exchange(ref _completed, 1) == 1)
		{
			return;
		}

		_stopwatch.Stop();

		// 租户可能在作用域创建后才解析（HTTP/消息参与者顺序），完成前再回填一次缺失字段。
		_recorder.ApplyContext(Record);

		Record.ExecutionDuration = (int)_stopwatch.ElapsedMilliseconds;
		if (Record.IsSuccess == null)
		{
			Record.IsSuccess = Record.Actions.Count == 0
				                   ? null
				                   : Record.Actions.All(x => x.IsSuccess == true);
		}

		await _recorder.SubmitScopeAsync(this, cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask AbortAsync(Exception exception, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(exception);

		Record.IsSuccess = false;
		Record.ExtraProperties ??= System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
		{
			["error"] = exception.GetType().Name,
			["message"] = exception.Message,
		});

		await CompleteAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		return CompleteAsync(CancellationToken.None);
	}
}
