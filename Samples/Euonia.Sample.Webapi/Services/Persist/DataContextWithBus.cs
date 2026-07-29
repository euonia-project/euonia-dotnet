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
/// Base data context with bus and request context support.
/// </summary>
internal abstract class DataContextWithBus<TContext> : DataContextBase<TContext>
	where TContext : DbContext, IRepositoryContext
{
	private readonly List<object> _unchangedEntities = [];
	private readonly IBus _bus;
	private readonly IRequestContextAccessor _request;

	/// <summary>
	/// Initializes a new instance of the <see cref="DataContextWithBus{TContext}"/> class.
	/// </summary>
	/// <param name="options">The options for this context.</param>
	/// <param name="bus">The <see cref="IBus"/> to publish domain events.</param>
	/// <param name="request">The accessor to get current request information.</param>
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
	/// Gets the DateTimeKind used for date and time values.
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
					await _bus.PublishAsync(@event, null, options, null, cancellationToken);
				}
			}
		}

		ChangeTracker.AutoDetectChangesEnabled = true;

		return result;
	}

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
				case EntityState.Deleted:
					entry.State = EntityState.Modified;
					auditing.DeletedBy = user;
					auditing.DeletedAt = dateTime;
					auditing.IsDeleted = true;

					break;
				case EntityState.Modified:
					auditing.UpdatedBy = user;
					auditing.UpdatedAt = dateTime;
					break;
			}
		}
	}

	/// <summary>
	/// Gets the current user from the request context.
	/// </summary>
	/// <returns></returns>
	private string GetCurrentUser()
	{
		var principal = _request?.Context?.User;

		if (principal == null)
		{
			return null;
		}

		return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity?.Name ?? null;
	}

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

	public override void Dispose()
	{
		ChangeTracker.DetectedEntityChanges -= OnDetectedEntityChanges;
		base.Dispose();
	}

	public override ValueTask DisposeAsync()
	{
		ChangeTracker.DetectedEntityChanges -= OnDetectedEntityChanges;
		return base.DisposeAsync();
	}

	protected virtual void OnDetectedEntityChanges(object sender, DetectedEntityChangesEventArgs args)
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
