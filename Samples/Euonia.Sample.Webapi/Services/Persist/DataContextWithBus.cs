using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 支持消息总线与请求上下文的基础数据上下文。
/// </summary>
internal abstract class DataContextWithBus<TContext> : DataContextBase<TContext>
	where TContext : DbContext, IRepositoryContext
{
	private readonly List<object?> _unchangedEntities = [];
	private readonly IBus _bus;
	private readonly IRequestContextAccessor _request;

	/// <summary>
	/// 初始化 <see cref="DataContextWithBus{TContext}"/> 类的新实例。
	/// </summary>
	/// <param name="options">此上下文的选项。</param>
	/// <param name="bus">用于发布领域事件的 <see cref="IBus"/>。</param>
	/// <param name="request">用于获取当前请求信息的访问器。</param>
	protected DataContextWithBus(DbContextOptions<TContext> options, IBus bus, IRequestContextAccessor request)
		: base(options)
	{
		_bus = bus;
		_request = request;
		ChangeTracker.DetectedEntityChanges += OnDetectedEntityChanges;
	}

	/// <inheritdoc/>
	protected override bool AutoSetEntryValues => true;


	/// <summary>
	/// 获取用于日期和时间值的 DateTimeKind。
	/// </summary>
	protected override DateTimeKind DateTimeKind => DateTimeKind.Utc;

	/// <inheritdoc />
	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

		ChangeTracker.AutoDetectChangesEnabled = false;

		if (_bus != null)
		{
			var events = GetTrackedEvents();

			if (result > 0 && events.Count > 0)
			{
				var options = new PublishOptions
				{
					RequestTraceId = _request?.Context?.TraceIdentifier
				};
				foreach (var @event in events)
				{
					await _bus.PublishAsync(@event, options, null, cancellationToken);
				}
			}
		}

		ChangeTracker.AutoDetectChangesEnabled = true;

		return result;
	}

	/// <inheritdoc/>
	/// <remarks>
	/// 为 <see cref="IAuditable"/> 实体写入创建/更新审计字段；对实现 <see cref="ITombstone"/> 的删除请求
	/// 改为写入删除标记与删除审计字段，并将状态转为 <see cref="EntityState.Modified"/> 以保留数据行。
	/// 本次未检测到实际变更的实体不参与赋值，避免产生无意义的更新。
	/// </remarks>
	protected override void SetEntryValues(IEnumerable<EntityEntry> entries)
	{
		if (!AutoSetEntryValues)
		{
			return;
		}

		foreach (var entry in entries)
		{
			if (entry.State == EntityState.Unchanged)
			{
				continue;
			}

			if (entry.State == EntityState.Modified && _unchangedEntities.Contains(entry.CurrentValues["Id"]))
			{
				continue;
			}

			if (entry.Entity is not IAuditable auditing)
			{
				continue;
			}

			var user = GetCurrentUser();

			if (string.IsNullOrWhiteSpace(user))
			{
				return;
			}

			var dateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
			switch (entry.State)
			{
				case EntityState.Added:
					auditing.CreatedBy = user;
					auditing.UpdatedBy = user;
					auditing.CreatedAt = dateTime;
					auditing.UpdatedAt = dateTime;

					break;
				case EntityState.Deleted when entry.Entity is ITombstone tombstone:
				{
					tombstone.IsDeleted = true;
					entry.Property("DeletedBy").CurrentValue = user;
					entry.Property("DeletedAt").CurrentValue = dateTime;
					entry.State = EntityState.Modified;
				}

					break;
				case EntityState.Modified:
					auditing.UpdatedBy = user;
					auditing.UpdatedAt = dateTime;
					break;
			}
		}
	}

	/// <summary>
	/// 从请求上下文中获取当前用户。
	/// </summary>
	/// <returns>当前用户的名称标识；无法获取时为 <see langword="null"/>。</returns>
	private string? GetCurrentUser()
	{
		var principal = _request?.Context?.User;

		if (principal == null)
		{
			return null;
		}

		return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity?.Name ?? null;
	}

	/// <inheritdoc/>
	/// <remarks>为 <see cref="DateTime"/> 与 <see cref="Nullable{DateTime}"/> 属性统一注册 UTC 时间转换器。</remarks>
	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);
		configurationBuilder.Properties<DateTime>()
		                    .HaveConversion<UniversalTimeConverter>();
		configurationBuilder.Properties<DateTime?>()
		                    .HaveConversion<UniversalTimeConverter>();
	}

	private List<DomainEvent> GetTrackedEvents()
	{
		var entries = ChangeTracker.Entries<IHasDomainEvents>();

		var events = new List<DomainEvent>();

		foreach (var entry in entries)
		{
			var aggregate = entry.Entity;

			aggregate.AttachToEvents();
			events.AddRange(aggregate.GetEvents());
			aggregate.ClearEvents();
		}

		return events;
	}

	/// <inheritdoc/>
	public override void Dispose()
	{
		ChangeTracker.DetectedEntityChanges -= OnDetectedEntityChanges;
		base.Dispose();
	}

	/// <inheritdoc/>
	public override ValueTask DisposeAsync()
	{
		ChangeTracker.DetectedEntityChanges -= OnDetectedEntityChanges;
		return base.DisposeAsync();
	}

	/// <summary>
	/// 处理 EF Core 的实体变更检测事件：记录“状态为已修改但未检测到实际变更”的实体标识。
	/// </summary>
	/// <param name="sender">事件源，即当前 <see cref="ChangeTracker"/>。</param>
	/// <param name="args">包含实体条目与变更检测结果的参数。</param>
	/// <remarks>记录的标识用于在 <see cref="SetEntryValues"/> 中跳过这些实体，避免写入审计字段后产生空更新。</remarks>
	protected virtual void OnDetectedEntityChanges(object? sender, DetectedEntityChangesEventArgs args)
	{
		Debug.WriteLine(args.Entry.DebugView.LongView);

		if (args.Entry.State != EntityState.Modified || args.ChangesFound)
		{
			return;
		}

		var id = args.Entry.CurrentValues["Id"]; //ExpressionHelper.GetPropertyValue(args.Entry.CurrentValues["Id"]);
		_unchangedEntities.Add(id);
	}
}