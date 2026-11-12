using System.Diagnostics;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 作用域内的动作实现；封口幂等。
/// </summary>
internal sealed class AuditingActionImpl : IAuditingAction
{
	// 参数在 SetParameter 时立即序列化，避免调用方后续修改对象影响审计内容。
	private readonly Dictionary<string, string?> _parameters = new(StringComparer.Ordinal);
	private readonly Dictionary<string, object?> _extraProperties = new(StringComparer.Ordinal);
	private readonly Action<AuditingActionImpl, AuditingAction> _onSealed;
	private readonly AuditingOptions _options;
	private readonly Stopwatch _stopwatch;
	private int _sealed;
	private int _recordResult;

	public AuditingActionImpl(
		Guid actionId,
		Guid? parentActionId,
		string serviceName,
		string methodName,
		string? actionName,
		string idempotencyKey,
		AuditingOptions options,
		Action<AuditingActionImpl, AuditingAction> onSealed)
	{
		ActionId = actionId;
		ParentActionId = parentActionId;
		ServiceName = serviceName;
		MethodName = methodName;
		ActionName = actionName;
		IdempotencyKey = idempotencyKey;
		_options = options;
		_recordResult = options.RecordResult ? 1 : 0;
		_onSealed = onSealed;
		ExecutionTime = DateTime.UtcNow;
		_stopwatch = Stopwatch.StartNew();
	}

	public void SetRecordResult(bool recordResult)
	{
		Volatile.Write(ref _recordResult, recordResult ? 1 : 0);
	}

	public Guid ActionId { get; }

	public Guid? ParentActionId { get; }

	public string IdempotencyKey { get; }

	public string ServiceName { get; }

	public string MethodName { get; }

	public string? ActionName { get; }

	public DateTime ExecutionTime { get; }

	public void SetParameter(string name, object? value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		if (!_options.RecordParameters)
		{
			return;
		}

		var snapshot = AuditingSerializer.Serialize(value, maxDepth: _options.MaxDepth);
		lock (_parameters)
		{
			_parameters[name] = snapshot;
		}
	}

	public void SetExtraProperty(string name, object? value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		lock (_extraProperties)
		{
			_extraProperties[name] = value;
		}
	}

	public void Complete(object? result)
	{
		if (Interlocked.Exchange(ref _sealed, 1) == 1)
		{
			return;
		}

		_onSealed(this, Build(result, null, true));
	}

	public void Fail(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception);

		if (Interlocked.Exchange(ref _sealed, 1) == 1)
		{
			return;
		}

		_onSealed(this, Build(null, exception, false));
	}

	private AuditingAction Build(object? result, Exception? exception, bool success)
	{
		_stopwatch.Stop();

		string? parameters = null;
		if (_options.RecordParameters)
		{
			lock (_parameters)
			{
				if (_parameters.Count > 0)
				{
					// 各参数已是 JSON 片段，直接拼装为对象，保持原始类型结构。
					parameters = "{" + string.Join(
						             ",",
						             _parameters.Select(pair => $"{System.Text.Json.JsonSerializer.Serialize(pair.Key)}:{pair.Value ?? "null"}")) + "}";
				}
			}
		}

		string? extra = null;
		lock (_extraProperties)
		{
			if (_extraProperties.Count > 0)
			{
				extra = AuditingSerializer.Serialize(new Dictionary<string, object?>(_extraProperties, StringComparer.Ordinal), maxDepth: _options.MaxDepth);
			}
		}

		return new AuditingAction
		{
			ActionId = ActionId,
			ParentActionId = ParentActionId,
			IdempotencyKey = IdempotencyKey,
			ServiceName = ServiceName,
			MethodName = MethodName,
			ActionName = ActionName,
			ExecutionTime = ExecutionTime,
			ExecutionDuration = (int)_stopwatch.ElapsedMilliseconds,
			IsSuccess = success,
			Parameters = parameters,
			Result = success && Volatile.Read(ref _recordResult) == 1 ? AuditingSerializer.Serialize(result, maxDepth: _options.MaxDepth) : null,
			ExceptionType = exception?.GetType().FullName,
			ExceptionMessage = AuditingSerializer.MaskExceptionMessage(exception?.Message),
			ExceptionStackTrace = _options.RecordStackTrace ? exception?.StackTrace : null,
			ExtraProperties = extra,
		};
	}
}
