using System.Diagnostics;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 作用域内的动作实现；封口幂等。
/// </summary>
internal sealed class AuditingActionImpl : IAuditingAction
{
	private readonly Dictionary<string, object?> _parameters = new(StringComparer.Ordinal);
	private readonly Dictionary<string, object?> _extraProperties = new(StringComparer.Ordinal);
	private readonly Action<AuditingActionImpl, AuditingAction> _onSealed;
	private readonly AuditingOptions _options;
	private readonly Stopwatch _stopwatch;
	private int _sealed;

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
		_onSealed = onSealed;
		ExecutionTime = DateTime.UtcNow;
		_stopwatch = Stopwatch.StartNew();
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
		lock (_parameters)
		{
			_parameters[name] = value;
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
					parameters = AuditingSerializer.Serialize(new Dictionary<string, object?>(_parameters, StringComparer.Ordinal));
				}
			}
		}

		string? extra = null;
		lock (_extraProperties)
		{
			if (_extraProperties.Count > 0)
			{
				extra = AuditingSerializer.Serialize(new Dictionary<string, object?>(_extraProperties, StringComparer.Ordinal));
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
			Result = success && _options.RecordResult ? AuditingSerializer.Serialize(result) : null,
			ExceptionType = exception?.GetType().FullName,
			ExceptionMessage = exception?.Message,
			ExceptionStackTrace = exception?.StackTrace,
			ExtraProperties = extra,
		};
	}
}
