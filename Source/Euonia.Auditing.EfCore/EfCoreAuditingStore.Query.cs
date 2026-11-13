using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Auditing.EfCore;

/// <summary>
/// EF Core 审计存储的查询与保留实现。
/// </summary>
public sealed partial class EfCoreAuditingStore<TContext> : IAuditingStoreQuery
{
	/// <inheritdoc />
	public async Task<IReadOnlyList<AuditingRecord>> QueryAsync(AuditingQuery query, CancellationToken cancellationToken = default)
	{
		query = AuditingQueryAuthorizer.Normalize(query);
		ArgumentNullException.ThrowIfNull(query);

		await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		var set = context.Set<AuditingRecord>().AsNoTracking().Include(record => record.Actions).AsQueryable();

		if (query.TenantId != null)
		{
			set = set.Where(record => record.TenantId == query.TenantId);
		}

		if (query.UserId != null)
		{
			set = set.Where(record => record.UserId == query.UserId);
		}

		if (query.CorrelationId != null)
		{
			set = set.Where(record => record.CorrelationId == query.CorrelationId);
		}

		if (query.IsSuccess != null)
		{
			set = set.Where(record => record.IsSuccess == query.IsSuccess);
		}

		if (query.From != null)
		{
			set = set.Where(record => record.ExecutionTime >= query.From);
		}

		if (query.To != null)
		{
			set = set.Where(record => record.ExecutionTime < query.To);
		}

		return await set.OrderByDescending(record => record.ExecutionTime)
		                .Skip(Math.Max(0, query.Skip))
		                .Take(query.Take <= 0 ? 50 : query.Take)
		                .ToListAsync(cancellationToken)
		                .ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<AuditingRecord?> FindByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
		AuditingQueryAuthorizer.EnsureScopeAccess(scopeId);

		await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		return await context.Set<AuditingRecord>()
		                    .AsNoTracking()
		                    .Include(record => record.Actions)
		                    .FirstOrDefaultAsync(record => record.ScopeId == scopeId, cancellationToken)
		                    .ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<int> DeleteBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default)
	{
		AuditingQueryAuthorizer.EnsureDeleteAuthorized();

		await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		var query = context.Set<AuditingRecord>().Where(record => record.ExecutionTime < cutoff);
		if (AuditingQueryAuthorizer.DeleteTenantScope is { Length: > 0 } tenantId)
		{
			query = query.Where(record => record.TenantId == tenantId);
		}

		return await query.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
	}
}
