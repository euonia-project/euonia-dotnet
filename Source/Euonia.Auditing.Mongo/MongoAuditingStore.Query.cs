using MongoDB.Driver;

namespace Nerosoft.Euonia.Auditing.Mongo;

public sealed partial class MongoAuditingStore
{
	/// <inheritdoc />
	public async Task<IReadOnlyList<AuditingRecord>> QueryAsync(AuditingQuery query, CancellationToken cancellationToken = default)
	{
		query = AuditingQueryAuthorizer.Normalize(query);

		var filters = new List<FilterDefinition<AuditingRecord>>();
		var builder = Builders<AuditingRecord>.Filter;
		if (query.TenantId != null)
		{
			filters.Add(builder.Eq(record => record.TenantId, query.TenantId));
		}

		if (!string.IsNullOrEmpty(query.UserId))
		{
			filters.Add(builder.Eq(record => record.UserId, query.UserId));
		}

		if (!string.IsNullOrEmpty(query.CorrelationId))
		{
			filters.Add(builder.Eq(record => record.CorrelationId, query.CorrelationId));
		}

		if (query.IsSuccess != null)
		{
			filters.Add(builder.Eq(record => record.IsSuccess, query.IsSuccess));
		}

		if (query.From != null)
		{
			filters.Add(builder.Gte(record => record.ExecutionTime, query.From.Value));
		}

		if (query.To != null)
		{
			filters.Add(builder.Lt(record => record.ExecutionTime, query.To.Value));
		}

		var filter = filters.Count == 0 ? builder.Empty : builder.And(filters);
		return await _collection.Find(filter)
		                        .SortByDescending(record => record.ExecutionTime)
		                        .Skip(query.Skip)
		                        .Limit(query.Take)
		                        .ToListAsync(cancellationToken)
		                        .ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<AuditingRecord?> FindByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
		AuditingQueryAuthorizer.EnsureScopeAccess(scopeId);

		return await _collection.Find(record => record.ScopeId == scopeId)
		                        .FirstOrDefaultAsync(cancellationToken)
		                        .ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<int> DeleteBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default)
	{
		AuditingQueryAuthorizer.EnsureDeleteAuthorized();

		var filters = new List<FilterDefinition<AuditingRecord>>
		{
			Builders<AuditingRecord>.Filter.Lt(record => record.ExecutionTime, cutoff),
		};

		if (AuditingQueryAuthorizer.DeleteTenantScope is { Length: > 0 } tenantId)
		{
			filters.Add(Builders<AuditingRecord>.Filter.Eq(record => record.TenantId, tenantId));
		}

		var result = await _collection.DeleteManyAsync(
			                              Builders<AuditingRecord>.Filter.And(filters),
			                              cancellationToken)
		                              .ConfigureAwait(false);
		return (int)result.DeletedCount;
	}
}
