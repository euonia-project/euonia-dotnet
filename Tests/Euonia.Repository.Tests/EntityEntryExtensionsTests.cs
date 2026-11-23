using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

public class EntityEntryExtensionsTests
{
	[Fact]
	public void TrySetProperty_ExistingProperty_SetsValue()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid() };
		var entry = context.Entry(entity);

		entry.TrySetProperty(nameof(EntryTestEntity.Name), "updated");

		Assert.Equal("updated", entity.Name);
	}

	[Fact]
	public void TrySetProperty_MissingProperty_DoesNothing()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid(), Name = "original" };
		var entry = context.Entry(entity);

		entry.TrySetProperty("Missing", "value");

		Assert.Equal("original", entity.Name);
	}

	private static EntryTestContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<EntryTestContext>()
		              .UseSqlite("DataSource=:memory:")
		              .Options;

		return new EntryTestContext(options);
	}

	private sealed class EntryTestContext(DbContextOptions<EntryTestContext> options) : DbContext(options)
	{
		public DbSet<EntryTestEntity> Entities => Set<EntryTestEntity>();
	}

	private sealed class EntryTestEntity
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}
}
