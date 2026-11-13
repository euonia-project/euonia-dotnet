using MongoDB.Driver;

namespace Nerosoft.Euonia.Auditing.Mongo;

/// <summary>
/// 基于 MongoDB 的审计存储：按 <see cref="AuditingRecord.ScopeId"/> 幂等，动作按幂等键去重。
/// </summary>
public sealed partial class MongoAuditingStore : IAuditingStore, IAuditingStoreQuery
{
	/// <summary>
	/// 默认集合名称。
	/// </summary>
	public const string DefaultCollectionName = "euonia_audit_log";

	private readonly IMongoCollection<AuditingRecord> _collection;
	private readonly SemaphoreSlim _indexLock = new(1, 1);
	private Task? _ensureIndexesTask;

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
	public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
	{
		if (_ensureIndexesTask is { IsCompletedSuccessfully: true })
		{
			return;
		}

		await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (_ensureIndexesTask is { IsCompletedSuccessfully: true })
			{
				return;
			}

			// 失败不缓存 faulted 任务：下次写入重试建索引。
			var task = EnsureCoreAsync(cancellationToken);
			await task.ConfigureAwait(false);
			_ensureIndexesTask = task;
		}
		catch
		{
			_ensureIndexesTask = null;
			throw;
		}
		finally
		{
			_indexLock.Release();
		}
	}

	private Task EnsureCoreAsync(CancellationToken cancellationToken)
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
		ArgumentException.ThrowIfNullOrWhiteSpace(record.ScopeId);

		// 单条原子 upsert：并发保存同一 ScopeId 不会互相覆盖（幂等索引兜底）。
		var update = Builders<AuditingRecord>.Update
		                                      .SetOnInsert(item => item.Id, record.Id)
		                                      .SetOnInsert(item => item.ScopeId, record.ScopeId)
		                                      .SetOnInsert(item => item.TenantId, record.TenantId)
		                                      .SetOnInsert(item => item.TenantName, record.TenantName)
		                                      .SetOnInsert(item => item.TargetTenantId, record.TargetTenantId)
		                                      .SetOnInsert(item => item.IsCrossTenant, record.IsCrossTenant)
		                                      .SetOnInsert(item => item.CrossTenantReason, record.CrossTenantReason)
		                                      .SetOnInsert(item => item.UserId, record.UserId)
		                                      .SetOnInsert(item => item.UserName, record.UserName)
		                                      .SetOnInsert(item => item.ClientId, record.ClientId)
		                                      .SetOnInsert(item => item.CorrelationId, record.CorrelationId)
		                                      .SetOnInsert(item => item.TraceId, record.TraceId)
		                                      .SetOnInsert(item => item.SpanId, record.SpanId)
		                                      .SetOnInsert(item => item.RequestTraceId, record.RequestTraceId)
		                                      .SetOnInsert(item => item.UnitOfWorkId, record.UnitOfWorkId)
		                                      .SetOnInsert(item => item.ApplicationName, record.ApplicationName)
		                                      .SetOnInsert(item => item.ExecutionTime, record.ExecutionTime)
		                                      .SetOnInsert(item => item.ClientIpAddress, record.ClientIpAddress)
		                                      .SetOnInsert(item => item.ClientName, record.ClientName)
		                                      .SetOnInsert(item => item.BrowserInfo, record.BrowserInfo)
		                                      .SetOnInsert(item => item.HttpMethod, record.HttpMethod)
		                                      .SetOnInsert(item => item.Url, record.Url)
		                                      .SetOnInsert(item => item.RecordedAt, record.RecordedAt)
		                                      .SetOnInsert(item => item.ExtraProperties, record.ExtraProperties)
		                                      .SetOnInsert(item => item.IsSuccess, record.IsSuccess)
		                                      .SetOnInsert(item => item.HttpStatusCode, record.HttpStatusCode);

		// IsSuccess/HttpStatusCode 采用“首次非空值”，与 EF/InMemory 的合并语义一致；耗时取最大。
		if (record.ExecutionDuration > 0)
		{
			update = update.Max(item => item.ExecutionDuration, record.ExecutionDuration);
		}

		if (record.Actions.Count > 0)
		{
			// 先按幂等键 Pull 旧动作再 Push，保证“同 key 更新”而不是按整文档去重。
			foreach (var action in record.Actions)
			{
				if (action.IdempotencyKey is { Length: > 0 } key)
				{
					var expectedKey = key;
					update = update.PullFilter(item => item.Actions, item => item.IdempotencyKey == expectedKey);
				}
				else
				{
					// 空幂等键用 ActionId 去重，避免重放时无限追加。
					var expectedActionId = action.ActionId;
					update = update.PullFilter(item => item.Actions, item => item.ActionId == expectedActionId);
				}
			}

			update = update.PushEach(item => item.Actions, record.Actions);
		}

		await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			await _collection.UpdateOneAsync(
				          item => item.ScopeId == record.ScopeId,
				          update,
				          new UpdateOptions { IsUpsert = true },
				          cancellationToken)
			          .ConfigureAwait(false);

		}
		catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
		{
			// 并发 upsert 的唯一键冲突：重试一次；动作按幂等键 Pull+Push，天然可重放。
			await _collection.UpdateOneAsync(
				          item => item.ScopeId == record.ScopeId,
				          update,
				          new UpdateOptions { IsUpsert = true },
				          cancellationToken)
			          .ConfigureAwait(false);
		}

		// 首次 upsert 时终态可能还是 null：成功与唯一键重试后都要回填（仅 null→值）。
		if (record.IsSuccess != null)
		{
			await _collection.UpdateOneAsync(
				          item => item.ScopeId == record.ScopeId && item.IsSuccess == null,
				          Builders<AuditingRecord>.Update.Set(item => item.IsSuccess, record.IsSuccess),
				          cancellationToken: cancellationToken)
			          .ConfigureAwait(false);
		}

		if (record.HttpStatusCode != null)
		{
			await _collection.UpdateOneAsync(
				          item => item.ScopeId == record.ScopeId && item.HttpStatusCode == null,
				          Builders<AuditingRecord>.Update.Set(item => item.HttpStatusCode, record.HttpStatusCode),
				          cancellationToken: cancellationToken)
			          .ConfigureAwait(false);
		}
	}
}
