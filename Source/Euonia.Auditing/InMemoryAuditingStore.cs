using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 内存审计存储，适用于测试与本地开发。
/// </summary>
public sealed partial class InMemoryAuditingStore : IAuditingStore
{
	private readonly ConcurrentDictionary<string, AuditingRecord> _records = new(StringComparer.Ordinal);
	private readonly object _sync = new();

	/// <summary>
	/// 获取当前记录快照。
	/// </summary>
	public IReadOnlyCollection<AuditingRecord> Records
	{
		get
		{
			lock (_sync)
			{
				return _records.Values.Select(Clone).ToArray();
			}
		}
	}

	/// <inheritdoc />
	public Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);

		var key = record.ScopeId ?? record.Id.ToString("N");
		lock (_sync)
		{
			if (!_records.TryGetValue(key, out var existing))
			{
				_records[key] = Clone(record);
				return Task.CompletedTask;
			}

			// 幂等：同一作用域重复保存时按幂等键合并动作，不产生重复行。
			foreach (var action in record.Actions)
			{
				var key2 = action.IdempotencyKey;
				if (key2 == null || existing.Actions.All(x => x.IdempotencyKey != key2))
				{
					existing.Actions.Add(action);
				}
			}

			existing.HttpStatusCode ??= record.HttpStatusCode;
			existing.IsSuccess ??= record.IsSuccess;
			existing.TenantId ??= record.TenantId;
			existing.TargetTenantId ??= record.TargetTenantId;
			existing.UserId ??= record.UserId;
			existing.ExtraProperties ??= record.ExtraProperties;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 清空存储。
	/// </summary>
	public void Clear()
	{
		lock (_sync)
		{
			_records.Clear();
		}
	}

	private static AuditingRecord Clone(AuditingRecord record)
	{
		return new AuditingRecord
		{
			Id = record.Id,
			ScopeId = record.ScopeId,
			TenantId = record.TenantId,
			TenantName = record.TenantName,
			TargetTenantId = record.TargetTenantId,
			IsCrossTenant = record.IsCrossTenant,
			CrossTenantReason = record.CrossTenantReason,
			UserId = record.UserId,
			UserName = record.UserName,
			ClientId = record.ClientId,
			CorrelationId = record.CorrelationId,
			TraceId = record.TraceId,
			SpanId = record.SpanId,
			RequestTraceId = record.RequestTraceId,
			UnitOfWorkId = record.UnitOfWorkId,
			ApplicationName = record.ApplicationName,
			ExecutionTime = record.ExecutionTime,
			ExecutionDuration = record.ExecutionDuration,
			ClientIpAddress = record.ClientIpAddress,
			ClientName = record.ClientName,
			BrowserInfo = record.BrowserInfo,
			HttpMethod = record.HttpMethod,
			HttpStatusCode = record.HttpStatusCode,
			Url = record.Url,
			IsSuccess = record.IsSuccess,
			ExtraProperties = record.ExtraProperties,
			RecordedAt = record.RecordedAt,
			Actions = record.Actions.Select(Clone).ToList(),
		};
	}

	private static AuditingAction Clone(AuditingAction action)
	{
		return new AuditingAction
		{
			ActionId = action.ActionId,
			ParentActionId = action.ParentActionId,
			IdempotencyKey = action.IdempotencyKey,
			ServiceName = action.ServiceName,
			MethodName = action.MethodName,
			ActionName = action.ActionName,
			ExecutionTime = action.ExecutionTime,
			ExecutionDuration = action.ExecutionDuration,
			IsSuccess = action.IsSuccess,
			Parameters = action.Parameters,
			Result = action.Result,
			ExceptionType = action.ExceptionType,
			ExceptionMessage = action.ExceptionMessage,
			ExceptionStackTrace = action.ExceptionStackTrace,
			ExtraProperties = action.ExtraProperties,
		};
	}
}
