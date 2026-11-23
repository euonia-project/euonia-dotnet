using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

public class DataContextAuditTests
{
	[Fact]
	public async Task SaveChangesAsync_SetsAuditFieldsAndConvertsDeleteToSoftDelete()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<AuditContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new AuditContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new AuditRecord { Id = Guid.NewGuid(), Name = "first" };
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		Assert.Equal("user-1", record.CreatedBy);
		Assert.Equal("user-1", record.UpdatedBy);
		Assert.NotEqual(default, record.CreatedAt);

		var createdAt = record.CreatedAt;
		record.Name = "second";
		await context.SaveChangesAsync(cancellationToken);

		Assert.Equal("user-1", record.UpdatedBy);
		Assert.Equal(createdAt, record.CreatedAt);

		context.Remove(record);
		await context.SaveChangesAsync(cancellationToken);

		Assert.True(record.IsDeleted);
		Assert.Equal("user-1", record.DeletedBy);
		Assert.NotNull(record.DeletedAt);

		var stored = await context.Set<AuditRecord>()
		                          .IgnoreQueryFilters()
		                          .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.True(stored.IsDeleted);
		Assert.Equal("user-1", stored.DeletedBy);
	}

	[Fact]
	public async Task SaveChangesAsync_UsesConfiguredUserFieldNames()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<CustomFieldContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new CustomFieldContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new CustomAuditRecord { Id = Guid.NewGuid(), Name = "custom" };
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		Assert.Equal("user-1", record.CreatorId);
		Assert.Equal("user-1", record.ModifierId);

		context.Remove(record);
		await context.SaveChangesAsync(cancellationToken);

		Assert.True(record.IsDeleted);
		Assert.Equal("user-1", record.RemoverId);
	}

	private sealed class CustomFieldContext(DbContextOptions<CustomFieldContext> options)
		: DataContextBase<CustomFieldContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		protected override object? GetCurrentUserId() => "user-1";

		protected override string CreatedByFieldName => "CreatorId";

		protected override string UpdatedByFieldName => "ModifierId";

		protected override string DeletedByFieldName => "RemoverId";

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<CustomAuditRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);
		}
	}

	private sealed class CustomAuditRecord : IHasCreateTime, IHasUpdateTime, IHasDeleteTime, ITombstone
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public DateTime? DeletedAt { get; set; }

		public bool IsDeleted { get; set; }

		public string CreatorId { get; set; } = string.Empty;

		public string ModifierId { get; set; } = string.Empty;

		public string RemoverId { get; set; } = string.Empty;
	}

	private sealed class AuditContext(DbContextOptions<AuditContext> options)
		: DataContextBase<AuditContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		protected override object? GetCurrentUserId()
		{
			return "user-1";
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AuditRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);
		}
	}

	private sealed class AuditRecord : IAuditable<string>
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public string CreatedBy { get; set; } = string.Empty;

		public string? UpdatedBy { get; set; }

		public DateTime? DeletedAt { get; set; }

		public bool IsDeleted { get; set; }

		public string? DeletedBy { get; set; }

		public object[] GetKeys() => [Id];
	}
}
