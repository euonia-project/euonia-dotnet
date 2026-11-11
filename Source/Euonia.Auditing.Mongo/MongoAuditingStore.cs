using MongoDB.Driver;

namespace Nerosoft.Euonia.Auditing.Mongo;

/// <summary>
/// 基于 MongoDB 的审计存储：按 <see cref="AuditingRecord.ScopeId"/> 幂等，动作按幂等键去重。
/// </summary>
public sealed class MongoAuditingStore : IAuditingStore
{
	/// <summary>
	/// 默认集合名称。
	/// </summary>
	public const string DefaultCollectionName = "euonia_audit_log";

	private readonly IMongoCollection<AuditingRecord> _collection;

	/// <summary>
	/// 初始化 <see cref="MongoAuditingStore"/> 的新实例。
	/// </summary>
	/// <param name="database">审计库。</param>
	/// <param name="collectionName">集合名称。</param>
	public MongoAuditingStore(IMongoDatabase database, string collectionName = DefaultCollectionName)
	{
		ArgumentNullException.ThrowIfNull(database);
		ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

		_collection = database.GetCollection<AuditingRecord>(collectionName);
	}

	/// <summary>
	/// 创建作用域唯一索引；宿主启动时调用一次。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示建索引操作的任务。</returns>
	public Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
	{
		var model = new CreateIndexModel<AuditingRecord>(
			Builders<AuditingRecord>.IndexKeys.Ascending(record => record.ScopeId),
			new CreateIndexOptions { Unique = true, Name = "ux_audit_log_scope" });

		return _collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
	}

	/// <inheritdoc />
	public async Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);

		var existing = await _collection
		                     .Find(item => item.ScopeId == record.ScopeId)
		                     .FirstOrDefaultAsync(cancellationToken)
		                     .ConfigureAwait(false);

		if (existing == null)
		{
			await _collection.ReplaceOneAsync(
				          item => item.ScopeId == record.ScopeId,
				          record,
				          new ReplaceOptions { IsUpsert = true },
				          cancellationToken)
			          .ConfigureAwait(false);
			return;
		}

		// 幂等：同一作用域重复保存时按幂等键合并动作。
		var keys = existing.Actions.Select(action => action.IdempotencyKey).ToHashSet(StringComparer.Ordinal);
		foreach (var action in record.Actions)
		{
			if (action.IdempotencyKey != null && keys.Add(action.IdempotencyKey))
			{
				existing.Actions.Add(action);
			}
		}

		await _collection.ReplaceOneAsync(
			          item => item.Id == existing.Id,
			          existing,
			          new ReplaceOptions { IsUpsert = true },
			          cancellationToken)
		          .ConfigureAwait(false);
	}
}
