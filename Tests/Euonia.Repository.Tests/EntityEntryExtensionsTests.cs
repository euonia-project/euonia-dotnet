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

	/// <summary>
	/// 轻11：值与属性类型不兼容时应静默跳过，而不是在赋值时抛类型转换异常。
	/// </summary>
	[Fact]
	public void TrySetProperty_TypeMismatch_DoesNothing()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid(), Count = 7 };
		var entry = context.Entry(entity);

		// 修复前：CurrentValue = "not-a-number" 写入 int 属性会抛异常。
		entry.TrySetProperty(nameof(EntryTestEntity.Count), "not-a-number");

		Assert.Equal(7, entity.Count);
	}

	/// <summary>
	/// 轻11：null 不能写入非可空值类型属性，应跳过。
	/// </summary>
	[Fact]
	public void TrySetProperty_NullToNonNullableValueType_DoesNothing()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid(), Count = 9 };
		var entry = context.Entry(entity);

		entry.TrySetProperty(nameof(EntryTestEntity.Count), (int?)null);

		Assert.Equal(9, entity.Count);
	}

	/// <summary>
	/// 轻11：可转换的数值类型（int → long）应转换后写入。
	/// </summary>
	[Fact]
	public void TrySetProperty_ConvertibleValue_SetsConvertedValue()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid() };
		var entry = context.Entry(entity);

		entry.TrySetProperty(nameof(EntryTestEntity.Total), 42);

		Assert.Equal(42L, entity.Total);
	}

	/// <summary>
	/// 轻11：不可转换的引用类型（Guid → string）应跳过。
	/// </summary>
	[Fact]
	public void TrySetProperty_IncompatibleReferenceType_DoesNothing()
	{
		using var context = CreateContext();
		var entity = new EntryTestEntity { Id = Guid.NewGuid(), Name = "original" };
		var entry = context.Entry(entity);

		entry.TrySetProperty(nameof(EntryTestEntity.Name), Guid.NewGuid());

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

		public int Count { get; set; }

		public long Total { get; set; }
	}
}
