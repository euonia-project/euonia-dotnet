namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 内存审计存储的查询与保留实现。
/// </summary>
public sealed partial class InMemoryAuditingStore : IAuditingStoreQuery
{
	/// <inheritdoc />
	public Task<IReadOnlyList<AuditingRecord>> QueryAsync(AuditingQuery query, CancellationToken cancellationToken = default)
	{
		query = AuditingQueryAuthorizer.Normalize(query);
		ArgumentNullException.ThrowIfNull(query);

		lock (_sync)
		{
			var items = _records.Values
			                    .Where(record => query.TenantId == null || record.TenantId == query.TenantId)
			                    .Where(record => query.UserId == null || record.UserId == query.UserId)
			                    .Where(record => query.CorrelationId == null || record.CorrelationId == query.CorrelationId)
			                    .Where(record => query.IsSuccess == null || record.IsSuccess == query.IsSuccess)
			                    .Where(record => query.From == null || record.ExecutionTime >= query.From)
			                    .Where(record => query.To == null || record.ExecutionTime < query.To)
			                    .OrderByDescending(record => record.ExecutionTime)
			                    .Skip(Math.Max(0, query.Skip))
			                    .Take(query.Take <= 0 ? 50 : query.Take)
			                    .Select(Clone)
			                    .ToArray();

			return Task.FromResult<IReadOnlyList<AuditingRecord>>(items);
		}
	}

	/// <inheritdoc />
	public Task<AuditingRecord?> FindByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
		AuditingQueryAuthorizer.EnsureScopeAccess(scopeId);

		lock (_sync)
		{
			return Task.FromResult(_records.TryGetValue(scopeId, out var record) ? Clone(record) : null);
		}
	}

	/// <inheritdoc />
	public Task<int> DeleteBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default)
	{
		AuditingQueryAuthorizer.EnsureDeleteAuthorized();
		return DeleteBeforeCoreAsync(cutoff, cancellationToken);
	}

	private Task<int> DeleteBeforeCoreAsync(DateTime cutoff, CancellationToken cancellationToken)
	{
		lock (_sync)
		{
			var tenantId = AuditingQueryAuthorizer.DeleteTenantScope;
			var keys = _records.Where(item => item.Value.ExecutionTime < cutoff
			                                  && (tenantId == null || item.Value.TenantId == tenantId))
			                   .Select(item => item.Key)
			                   .ToArray();

			foreach (var key in keys)
			{
				_records.TryRemove(key, out _);
			}

			return Task.FromResult(keys.Length);
		}
	}
}
