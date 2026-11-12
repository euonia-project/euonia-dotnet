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
	private readonly HashSet<Guid> _openActions = new();
	private readonly List<Guid> _actionStack = new();
	private int _completed;
	private int _actionSequence;

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

	/// <inheritdoc />
	public bool IsCompleted => Volatile.Read(ref _completed) == 1;

	public IAuditingAction BeginAction(string serviceName, string methodName, string? actionName = null)
	{
		ArgumentNullException.ThrowIfNull(serviceName);
		ArgumentNullException.ThrowIfNull(methodName);

		AuditingActionImpl action;
		lock (_sync)
		{
			// 完成状态检查与动作登记必须在同一临界区，否则并发 Complete 会静默丢弃已开始的动作。
			if (Volatile.Read(ref _completed) == 1)
			{
				throw new InvalidOperationException($"Auditing scope '{ScopeId}' is already completed; actions can no longer be recorded.");
			}

			Guid? parentActionId = _actionStack.Count > 0 ? _actionStack[^1] : null;

			// 幂等键按“作用域前缀 + 动作唯一后缀”生成：同一作用域内的多个动作不能共用键，
			// 否则 EF 的 (log_id, idempotency_key) 唯一索引会直接冲突。
			var idempotencyPrefix = _scopeOptions.IdempotencyKey is { Length: > 0 } configuredKey
				                        ? configuredKey
				                        : ScopeId;
			var sequence = Interlocked.Increment(ref _actionSequence);
			var suffix = Truncate($"{serviceName}.{methodName}#{sequence}", 280);
			var idempotencyKey = $"{Truncate(idempotencyPrefix, 320 - suffix.Length - 1)}:{suffix}";
			action = new AuditingActionImpl(
				AuditingRecordFactory.NewId(),
				parentActionId,
				serviceName,
				methodName,
				actionName,
				idempotencyKey,
				_options,
				OnActionSealed);

			_actionStack.Add(action.ActionId);
			_openActions.Add(action.ActionId);
		}

		_recorder.ApplyContext(Record);
		return action;
	}

	private static string Truncate(string value, int maxLength)
	{
		if (maxLength <= 0)
		{
			return string.Empty;
		}

		return value.Length <= maxLength ? value : value[..maxLength];
	}

	private void OnActionSealed(AuditingActionImpl action, AuditingAction record)
	{
		lock (_sync)
		{
			if (Volatile.Read(ref _completed) == 1)
			{
				// 作用域已提交：迟到动作只丢弃，避免修改已序列化/落库的记录。
				return;
			}

			// 完成顺序不保证与开始顺序相反：完成簿记用集合，栈只用于父子关系。
			_openActions.Remove(action.ActionId);
			// 乱序完成时从栈中移除任意位置，避免已完成的父动作继续被后续动作引用。
			_actionStack.Remove(action.ActionId);

			Record.Actions.Add(record);

			if (_openActions.Count > 0 || !_scopeOptions.AutoComplete)
			{
				return;
			}
		}

		// 自动完成不得阻塞调用方：先同步分离，再后台提交并观察异常。
		_recorder.CompleteInBackground(this);
	}

	public async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
	{
		AuditingAction[] actions;
		bool alreadyCompleted;
		lock (_sync)
		{
			alreadyCompleted = Volatile.Read(ref _completed) == 1;
			if (!alreadyCompleted)
			{
				// 置完成与取快照在同一把锁内，避免快照之后还有动作被静默丢弃。
				Volatile.Write(ref _completed, 1);
				_stopwatch.Stop();
			}

			actions = Record.Actions.ToArray();
		}

		if (alreadyCompleted)
		{
			_recorder.Detach(this);
			return;
		}

		// 租户可能在作用域创建后才解析（HTTP/消息参与者顺序），完成前再回填一次缺失字段。
		_recorder.ApplyContext(Record);

		Record.ExecutionDuration = (int)_stopwatch.ElapsedMilliseconds;

		if (Record.IsSuccess == null)
		{
			Record.IsSuccess = actions.Length == 0
				                   ? null
				                   : actions.All(x => x.IsSuccess == true);
		}

		await _recorder.SubmitScopeAsync(this, cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask AbortAsync(Exception exception, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(exception);

		lock (_sync)
		{
			if (Volatile.Read(ref _completed) == 1)
			{
				// 已提交的记录不可再修改：避免 writer/存储读取到“提交后变更”。
				return;
			}

			Record.IsSuccess = false;
			Record.MergeExtraProperties(new Dictionary<string, string>
			{
				["error"] = exception.GetType().Name,
				["message"] = AuditingRecordExtensions.MaskExceptionMessage(exception.Message) ?? string.Empty,
			});
		}

		await CompleteAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		return CompleteAsync(CancellationToken.None);
	}
}
