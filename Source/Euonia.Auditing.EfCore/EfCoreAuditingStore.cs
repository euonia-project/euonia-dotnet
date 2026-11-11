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
		foreach (var record in records)
		{
			await MergeAsync(context, record, cancellationToken).ConfigureAwait(false);
		}

		await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	private static async Task SaveCoreAsync(TContext context, AuditingRecord record, CancellationToken cancellationToken)
	{
		await MergeAsync(context, record, cancellationToken).ConfigureAwait(false);

		try
		{
			await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException)
		{
			// 并发重复写入：重新加载后合并，再保存一次。
			context.ChangeTracker.Clear();
			await MergeAsync(context, record, cancellationToken).ConfigureAwait(false);
			await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	private static async Task MergeAsync(TContext context, AuditingRecord record, CancellationToken cancellationToken)
	{
		var set = context.Set<AuditingRecord>();
		var existing = await set.FirstOrDefaultAsync(item => item.ScopeId == record.ScopeId, cancellationToken)
		                        .ConfigureAwait(false);

		if (existing == null)
		{
			// 新记录只插入，避免导航集合参与跟踪后产生更新语义。
			foreach (var action in record.Actions)
			{
				context.Entry(action).Property("LogId").CurrentValue = record.Id;
			}

			await set.AddAsync(record, cancellationToken).ConfigureAwait(false);
			return;
		}

		// 幂等：同一作用域重复保存时按幂等键合并动作；动作按实体显式插入，不修改已存在的行。
		var actions = context.Set<AuditingAction>();
		foreach (var action in record.Actions)
		{
			var exists = await actions.AnyAsync(
				             item => EF.Property<Guid>(item, "LogId") == existing.Id
				                     && item.IdempotencyKey == action.IdempotencyKey,
				             cancellationToken)
			             .ConfigureAwait(false);

			if (exists)
			{
				continue;
			}

			context.Entry(action).Property("LogId").CurrentValue = existing.Id;
			await actions.AddAsync(action, cancellationToken).ConfigureAwait(false);
		}
	}
}
