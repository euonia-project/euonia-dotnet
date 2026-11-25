using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.Tests;

/// <summary>
/// 针对软删除与审计填充的回归测试：匿名用户场景（D1）、stub 删除（D2）与未映射 IsDeleted（中3）。
/// </summary>
public class DataContextSoftDeleteRegressionTests
{
	/// <summary>
	/// D1：未重写 GetCurrentUserId（返回 null）时，Remove 仍应转换为软删除并填充时间戳，
	/// 而不是物理删除 ITombstone 实体；仅跳过操作用户字段写入。
	/// </summary>
	[Fact]
	public async Task SaveChangesAsync_WithoutCurrentUser_StillSoftDeletesAndSetsTimestamps()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<AnonymousContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new AnonymousContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new AnonymousRecord { Id = Guid.NewGuid(), Name = "anon" };
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		// 时间戳仍应填充，操作用户字段不写入。
		Assert.NotEqual(default, record.CreatedAt);
		Assert.Equal(string.Empty, record.CreatedBy);

		context.Remove(record);
		await context.SaveChangesAsync(cancellationToken);

		// 修复前：userId 为 null 时直接 return，Deleted 状态未被转换，实体被物理删除，
		// IgnoreQueryFilters 也查不到该行（SingleAsync 抛异常）。
		var stored = await context.Set<AnonymousRecord>()
		                          .IgnoreQueryFilters()
		                          .AsNoTracking()
		                          .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.True(stored.IsDeleted);
		Assert.NotNull(stored.DeletedAt);
		Assert.Null(stored.DeletedBy);
		Assert.Equal("anon", stored.Name);
	}

	/// <summary>
	/// D2：仅含主键的 stub 实体 Remove 后，生成的 UPDATE 只应包含软删除与审计列，
	/// 不得把其余列（如 Name）写成 default 值。
	/// </summary>
	[Fact]
	public async Task SaveChangesAsync_StubRemove_OnlyUpdatesSoftDeleteColumns()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<UserContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new UserContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new StubRecord { Id = Guid.NewGuid(), Name = "keep-me", CreatedBy = "creator" };
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		var createdAt = record.CreatedAt;

		// 模拟新作用域中仅携带主键的删除请求：先解除对完整实体的跟踪。
		context.Entry(record).State = EntityState.Detached;

		var stub = new StubRecord { Id = record.Id };
		context.Remove(stub);
		await context.SaveChangesAsync(cancellationToken);

		// 修复前：entry.State = Modified 将全部属性标记修改，Name/CreatedAt 等被写成 default。
		var stored = await context.Set<StubRecord>()
		                          .IgnoreQueryFilters()
		                          .AsNoTracking()
		                          .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.True(stored.IsDeleted);
		Assert.Equal("keep-me", stored.Name);
		// CreatedBy 在 Add 时被 GetCurrentUserId() 覆盖为 "user-1"，不是构造时的 "creator"。
		Assert.Equal("user-1", stored.CreatedBy);
		Assert.Equal(createdAt, stored.CreatedAt);
		Assert.Equal("user-1", stored.DeletedBy);
		Assert.NotNull(stored.DeletedAt);
		Assert.NotNull(stored.UpdatedAt);
	}

	/// <summary>
	/// D2：匿名用户的 stub 删除不得把未赋值的审计列（UpdatedBy/DeletedBy）写成 null。
	/// </summary>
	[Fact]
	public async Task SaveChangesAsync_AnonymousStubRemove_PreservesUnwrittenAuditColumns()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<AnonymousContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new AnonymousContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new AnonymousRecord
		{
			Id = Guid.NewGuid(),
			Name = "keep-me",
			CreatedBy = "creator",
			UpdatedBy = "updater"
		};
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		context.Entry(record).State = EntityState.Detached;

		var stub = new AnonymousRecord { Id = record.Id };
		context.Remove(stub);
		await context.SaveChangesAsync(cancellationToken);

		var stored = await context.Set<AnonymousRecord>()
		                          .IgnoreQueryFilters()
		                          .AsNoTracking()
		                          .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.True(stored.IsDeleted);
		Assert.Equal("keep-me", stored.Name);
		Assert.Equal("creator", stored.CreatedBy);
		Assert.Equal("updater", stored.UpdatedBy);
		Assert.Null(stored.DeletedBy);
		Assert.NotNull(stored.DeletedAt);
		Assert.NotNull(stored.UpdatedAt);
	}

	/// <summary>
	/// 中3：IsDeleted 被 [NotMapped] 标记时，SaveChanges 不应因 entry.Property 访问未映射属性而抛异常。
	/// </summary>
	[Fact]
	public async Task SaveChangesAsync_NotMappedIsDeleted_DoesNotThrow()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(cancellationToken);

		var options = new DbContextOptionsBuilder<NotMappedContext>()
		              .UseSqlite(connection)
		              .Options;

		await using var context = new NotMappedContext(options);
		await context.Database.EnsureCreatedAsync(cancellationToken);

		var record = new NotMappedRecord { Id = Guid.NewGuid(), Name = "not-mapped" };
		context.Add(record);
		await context.SaveChangesAsync(cancellationToken);

		// 修复前：Modified 分支 entry.Property(nameof(ITombstone.IsDeleted)) 抛 InvalidOperationException。
		// IsDeleted 未映射、不被跟踪，须同时修改已映射属性使条目进入 Modified 状态。
		record.Name = "changed";
		record.IsDeleted = true;
		await context.SaveChangesAsync(cancellationToken);

		// Remove 路径同样不应抛异常；IsDeleted 未映射，行仍保留且 DeletedAt 已写入。
		context.Remove(record);
		await context.SaveChangesAsync(cancellationToken);

		var stored = await context.Set<NotMappedRecord>()
		                          .IgnoreQueryFilters()
		                          .AsNoTracking()
		                          .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.NotNull(stored.DeletedAt);
		Assert.Equal("changed", stored.Name);
	}

	private sealed class AnonymousContext(DbContextOptions<AnonymousContext> options)
		: DataContextBase<AnonymousContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		// 故意不重写 GetCurrentUserId()，使用基类默认返回 null（匿名/后台场景）。

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AnonymousRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);
		}
	}

	private sealed class AnonymousRecord : IAuditable<string>
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
	}

	private sealed class UserContext(DbContextOptions<UserContext> options)
		: DataContextBase<UserContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		protected override object? GetCurrentUserId() => "user-1";

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<StubRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);
		}
	}

	private sealed class StubRecord : IAuditable<string>
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
	}

	private sealed class NotMappedContext(DbContextOptions<NotMappedContext> options)
		: DataContextBase<NotMappedContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		protected override object? GetCurrentUserId() => "user-1";

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<NotMappedRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);

			// IsDeleted 未映射，基类按 ITombstone 接口设置的软删除过滤器无法翻译，此处移除以隔离 SaveChanges 路径。
			modelBuilder.Entity<NotMappedRecord>().Metadata.SetQueryFilter(TenantFilterNames.Tombstone, null);
		}
	}

	private sealed class NotMappedRecord : ITombstone, IHasDeleteTime
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public DateTime? DeletedAt { get; set; }

		[NotMapped]
		public bool IsDeleted { get; set; }
	}
}
