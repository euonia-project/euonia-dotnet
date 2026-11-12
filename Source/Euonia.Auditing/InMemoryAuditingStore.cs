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

		ArgumentException.ThrowIfNullOrWhiteSpace(record.ScopeId);
		var key = record.ScopeId;
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
				var duplicate = key2 != null
					                ? existing.Actions.Any(x => x.IdempotencyKey == key2)
					                : existing.Actions.Any(x => x.ActionId == action.ActionId);
				if (!duplicate)
				{
					// 存副本：调用方在 SaveAsync 返回后修改动作对象不应影响已存储数据。
					existing.Actions.Add(Clone(action));
				}
			}

			existing.HttpStatusCode ??= record.HttpStatusCode;
			existing.IsSuccess ??= record.IsSuccess;
			existing.TenantId ??= record.TenantId;
			existing.TenantName ??= record.TenantName;
			existing.TargetTenantId ??= record.TargetTenantId;
			existing.CrossTenantReason ??= record.CrossTenantReason;
			existing.UserId ??= record.UserId;
			existing.UserName ??= record.UserName;
			existing.ClientId ??= record.ClientId;
			existing.CorrelationId ??= record.CorrelationId;
			existing.TraceId ??= record.TraceId;
			existing.SpanId ??= record.SpanId;
			existing.RequestTraceId ??= record.RequestTraceId;
			existing.ApplicationName ??= record.ApplicationName;
			existing.ClientIpAddress ??= record.ClientIpAddress;
			existing.ClientName ??= record.ClientName;
			existing.BrowserInfo ??= record.BrowserInfo;
			existing.HttpMethod ??= record.HttpMethod;
			existing.Url ??= record.Url;
			existing.ExtraProperties ??= record.ExtraProperties;
			existing.IsCrossTenant = existing.IsCrossTenant || record.IsCrossTenant;
			existing.ExecutionDuration = Math.Max(existing.ExecutionDuration, record.ExecutionDuration);
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
