using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Auditing.EfCore;

/// <summary>
/// 基于 EF Core 的审计存储：按 <see cref="AuditingRecord.ScopeId"/> 幂等，动作按幂等键去重。
/// </summary>
/// <typeparam name="TContext">审计数据上下文类型。</typeparam>
public sealed partial class EfCoreAuditingStore<TContext> : IAuditingStore
	where TContext : DbContext
{
	private readonly IDbContextFactory<TContext> _factory;

	/// <summary>
	/// 初始化 <see cref="EfCoreAuditingStore{TContext}"/> 的新实例。
	/// </summary>
	/// <param name="factory">独立上下文工厂；不得复用业务上下文。</param>
	public EfCoreAuditingStore(IDbContextFactory<TContext> factory)
	{
		_factory = factory;
	}

	/// <inheritdoc />
	public async Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);

		await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		await SaveCoreAsync(context, record, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task SaveManyAsync(IReadOnlyCollection<AuditingRecord> records, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(records);
		if (records.Count == 0)
		{
			return;
		}

		await using var context = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

		// 一次预取批内已有幂等键，避免逐动作 AnyAsync 的 N+1 查询。
		var keys = records.SelectMany(record => record.Actions)
		                  .Select(action => action.IdempotencyKey)
		                  .Distinct(StringComparer.Ordinal)
		                  .ToArray();

		var scopeIds = records.Select(record => record.ScopeId!).Distinct(StringComparer.Ordinal).ToArray();
		var existingIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
		if (scopeIds.Length > 0)
		{
			var existingRecords = await context.Set<AuditingRecord>()
			                                  .Where(record => scopeIds.Contains(record.ScopeId!))
			                                  .Select(record => new { ScopeId = record.ScopeId!, record.Id })
			                                  .ToListAsync(cancellationToken)
			                                  .ConfigureAwait(false);

			foreach (var item in existingRecords)
			{
				existingIds[item.ScopeId] = item.Id;
			}
		}

		var existingActions = new HashSet<(Guid LogId, string IdempotencyKey)>();
		if (keys.Length > 0)
		{
			var existing = await context.Set<AuditingAction>()
			                            .Where(action => keys.Contains(action.IdempotencyKey))
			                            .Select(action => new
			                            {
				                            LogId = EF.Property<Guid>(action, "LogId"),
				                            action.IdempotencyKey,
			                            })
			                            .ToListAsync(cancellationToken)
			                            .ConfigureAwait(false);

			foreach (var item in existing)
			{
				existingActions.Add((item.LogId, item.IdempotencyKey ?? string.Empty));
			}
		}

		foreach (var record in records)
		{
			await MergeAsync(context, record, existingIds, existingActions, cancellationToken).ConfigureAwait(false);
		}

		try
		{
			await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException exception) when (IsUniqueViolation(exception))
		{
			// 仅唯一键冲突才降级逐条重试；截断/必填等真实数据错误原样抛出。
			context.ChangeTracker.Clear();
			foreach (var record in records)
			{
				await SaveAsync(record, cancellationToken).ConfigureAwait(false);
			}
		}
	}

	private static async Task SaveCoreAsync(TContext context, AuditingRecord record, CancellationToken cancellationToken)
	{
		await MergeAsync(context, record, cancellationToken).ConfigureAwait(false);

		try
		{
			await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException exception) when (IsUniqueViolation(exception))
		{
			// 并发重复写入：重新加载后合并，再保存一次。
			context.ChangeTracker.Clear();
			await MergeAsync(context, record, cancellationToken).ConfigureAwait(false);
			await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	private static bool IsUniqueViolation(DbUpdateException exception)
	{
		for (Exception? current = exception.InnerException; current != null; current = current.InnerException)
		{
			var typeName = current.GetType().FullName ?? string.Empty;
			var number = TryGetInt32(current, "Number");
			var sqliteErrorCode = TryGetInt32(current, "SqliteErrorCode");
			var sqlState = current is System.Data.Common.DbException dbException ? dbException.SqlState : null;

			// 按提供程序错误码判断，避免 ToString() 中的业务数字/消息造成误判。
			if ((typeName.Contains("SqlClient", StringComparison.Ordinal)
			     || typeName.Contains("SqlException", StringComparison.Ordinal))
			    && number is 2627 or 2601)
			{
				return true;
			}

			if (typeName.Contains("Npgsql", StringComparison.Ordinal) && sqlState == "23505")
			{
				return true;
			}

			if (typeName.Contains("MySql", StringComparison.Ordinal) && number == 1062)
			{
				return true;
			}

			if (typeName.Contains("Sqlite", StringComparison.Ordinal)
			    && sqliteErrorCode == 19
			    && current.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static int? TryGetInt32(Exception exception, string propertyName)
	{
		var property = exception.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		return property?.GetValue(exception) is int value ? value : null;
	}

	private static async Task MergeAsync(TContext context, AuditingRecord record, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(record.ScopeId);
		await MergeAsync(context, record, null, null, cancellationToken).ConfigureAwait(false);
	}

	private static async Task MergeAsync(
		TContext context,
		AuditingRecord record,
		Dictionary<string, Guid>? existingIds,
		HashSet<(Guid LogId, string IdempotencyKey)>? existingActions,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(record.ScopeId);
		var set = context.Set<AuditingRecord>();
		var existingId = existingIds != null && existingIds.TryGetValue(record.ScopeId!, out var knownId)
			                 ? knownId
			                 : await set.Where(item => item.ScopeId == record.ScopeId!)
			                            .Select(item => (Guid?)item.Id)
			                            .FirstOrDefaultAsync(cancellationToken)
			                            .ConfigureAwait(false);

		if (existingId == null)
		{
			// 新记录只插入，避免导航集合参与跟踪后产生更新语义。
			foreach (var action in record.Actions)
			{
				context.Entry(action).Property("LogId").CurrentValue = record.Id;
			}

			existingIds?.Add(record.ScopeId!, record.Id);
			await set.AddAsync(record, cancellationToken).ConfigureAwait(false);

			// 同批后续记录会命中该 Id：把动作键写入集合，避免重复插入触发唯一索引。
			if (existingActions != null)
			{
				foreach (var action in record.Actions)
				{
					existingActions.Add((record.Id, action.IdempotencyKey ?? string.Empty));
				}
			}

			return;
		}

		// 同批重复 ScopeId：合并终态到已跟踪的实体，避免第二条记录的终态被静默丢弃。
		var tracked = set.Local.FirstOrDefault(item => item.Id == existingId.Value);
		if (tracked != null && !ReferenceEquals(tracked, record))
		{
			// 与未跟踪路径保持一致：终态首次非空优先，耗时取最大。
			tracked.IsSuccess ??= record.IsSuccess;
			tracked.HttpStatusCode ??= record.HttpStatusCode;
			tracked.ExecutionDuration = Math.Max(tracked.ExecutionDuration, record.ExecutionDuration);
		}

		if (tracked == null && (record.IsSuccess != null || record.ExecutionDuration > 0 || record.HttpStatusCode != null))
		{
			// 已存在的行不在当前跟踪器：用原子 UPDATE 合并终态（null→值，耗时取最大）。
			var terminal = context.Set<AuditingRecord>().Where(item => item.Id == existingId.Value);

			// DbContext 不是线程安全的：所有终态字段合并到一次（串行）UPDATE。
			await terminal.ExecuteUpdateAsync(
				      setters =>
				      {
					      if (record.IsSuccess != null)
					      {
						      setters = setters.SetProperty(item => item.IsSuccess, item => item.IsSuccess ?? record.IsSuccess);
					      }

					      if (record.HttpStatusCode != null)
					      {
						      setters = setters.SetProperty(item => item.HttpStatusCode, item => item.HttpStatusCode ?? record.HttpStatusCode);
					      }

					      if (record.ExecutionDuration > 0)
					      {
						      setters = setters.SetProperty(
							      item => item.ExecutionDuration,
							      item => item.ExecutionDuration < record.ExecutionDuration ? record.ExecutionDuration : item.ExecutionDuration);
					      }
				      },
				      cancellationToken)
			      .ConfigureAwait(false);
		}

		// 幂等：同一作用域重复保存时按幂等键合并动作；动作按实体显式插入，不修改已存在的行。
		var actions = context.Set<AuditingAction>();
		foreach (var action in record.Actions)
		{
			// 契约允许 null 幂等键，但 EF 列非空：用动作 Id 生成稳定键。
			action.IdempotencyKey ??= action.ActionId.ToString("N");
		}

		if (existingActions == null && record.Actions.Count > 0)
		{
			// 单条保存/降级路径也做一次动作键预取，避免每个动作一次 AnyAsync。
			var keys = record.Actions.Select(action => action.IdempotencyKey).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
			var storedKeys = await actions
			                       .Where(action => EF.Property<Guid>(action, "LogId") == existingId.Value && keys.Contains(action.IdempotencyKey!))
			                       .Select(action => action.IdempotencyKey)
			                       .ToListAsync(cancellationToken)
			                       .ConfigureAwait(false);
			existingActions = storedKeys.Where(key => key != null).Select(key => (existingId.Value, key!)).ToHashSet();
		}

		foreach (var action in record.Actions)
		{
			var key = (LogId: existingId.Value, IdempotencyKey: action.IdempotencyKey ?? string.Empty);
			var exists = existingActions?.Contains(key)
			             ?? await actions.AnyAsync(
				                item => EF.Property<Guid>(item, "LogId") == existingId.Value
				                        && item.IdempotencyKey == action.IdempotencyKey,
				                cancellationToken)
			                .ConfigureAwait(false);

			if (exists)
			{
				continue;
			}

			existingActions?.Add(key);
			context.Entry(action).Property("LogId").CurrentValue = existingId.Value;
			await actions.AddAsync(action, cancellationToken).ConfigureAwait(false);
		}
	}
}
