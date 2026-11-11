using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;

namespace Nerosoft.Euonia.Application.Tests.Auditing;

/// <summary>
/// EF Core 审计存储：表结构、幂等落库与动作去重。
/// </summary>
public sealed class EfCoreAuditingStoreTests : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<AuditingDbContext> _options;

	/// <summary>
	/// 初始化测试并创建内存 SQLite 库。
	/// </summary>
	public EfCoreAuditingStoreTests()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<AuditingDbContext>()
		           .UseSqlite(_connection)
		           .Options;

		using var context = new AuditingDbContext(_options);
		context.Database.EnsureCreated();
	}

	/// <summary>
	/// 同一作用域重复保存不产生重复记录与重复动作。
	/// </summary>
	[Fact]
	public async Task SaveAsync_ShouldBeIdempotent()
	{
		var store = new EfCoreAuditingStore<AuditingDbContext>(new TestFactory(_options));
		var actionId = Guid.CreateVersion7();

		var record = CreateRecord("t1:req:1", actionId, "k1");
		await store.SaveAsync(record, TestContext.Current.CancellationToken);

		// 第二次保存：同一作用域 + 同一动作键，追加一个不同的动作。
		var duplicate = CreateRecord("t1:req:1", Guid.CreateVersion7(), "k2");
		await store.SaveAsync(duplicate, TestContext.Current.CancellationToken);

		await using var context = new AuditingDbContext(_options);
		var records = await context.Records.Include(x => x.Actions).ToListAsync(TestContext.Current.CancellationToken);

		var saved = Assert.Single(records);
		Assert.Equal(2, saved.Actions.Count);
		Assert.Contains(saved.Actions, x => x.IdempotencyKey == "k1");
		Assert.Contains(saved.Actions, x => x.IdempotencyKey == "k2");
	}

	/// <summary>
	/// 批量保存使用单次 SaveChanges。
	/// </summary>
	[Fact]
	public async Task SaveManyAsync_ShouldPersistAll()
	{
		var store = new EfCoreAuditingStore<AuditingDbContext>(new TestFactory(_options));

		await store.SaveManyAsync(
		[
			CreateRecord("t1:req:a", Guid.CreateVersion7(), "a"),
			CreateRecord("t1:req:b", Guid.CreateVersion7(), "b"),
		], TestContext.Current.CancellationToken);

		await using var context = new AuditingDbContext(_options);
		Assert.Equal(2, await context.Records.CountAsync(TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 查询按租户与结果过滤，清理按时间删除。
	/// </summary>
	[Fact]
	public async Task QueryAndDelete_ShouldWork()
	{
		var store = new EfCoreAuditingStore<AuditingDbContext>(new TestFactory(_options));
		var now = DateTime.UtcNow;

		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:old",
			TenantId = "t1",
			ExecutionTime = now.AddHours(-2),
			RecordedAt = now,
			IsSuccess = true,
		}, TestContext.Current.CancellationToken);

		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:new",
			TenantId = "t1",
			ExecutionTime = now,
			RecordedAt = now,
			IsSuccess = false,
		}, TestContext.Current.CancellationToken);

		var failures = await store.QueryAsync(new AuditingQuery { TenantId = "t1", IsSuccess = false }, TestContext.Current.CancellationToken);
		Assert.Single(failures);
		Assert.Equal("t1:new", failures[0].ScopeId);

		Assert.NotNull(await store.FindByScopeAsync("t1:old", TestContext.Current.CancellationToken));

		var deleted = await store.DeleteBeforeAsync(now.AddHours(-1), TestContext.Current.CancellationToken);
		Assert.Equal(1, deleted);

		await using var context = new AuditingDbContext(_options);
		Assert.Equal(1, await context.Records.CountAsync(TestContext.Current.CancellationToken));
	}

	private static AuditingRecord CreateRecord(string scopeId, Guid actionId, string idempotencyKey)
	{
		return new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = scopeId,
			TenantId = "t1",
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
			HttpStatusCode = 200,
			IsSuccess = true,
			Actions =
			[
				new AuditingAction
				{
					ActionId = actionId,
					IdempotencyKey = idempotencyKey,
					ServiceName = "Svc",
					MethodName = "Do",
					ExecutionTime = DateTime.UtcNow,
					IsSuccess = true,
				},
			],
		};
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_connection.Dispose();
	}

	private sealed class TestFactory : IDbContextFactory<AuditingDbContext>
	{
		private readonly DbContextOptions<AuditingDbContext> _options;

		public TestFactory(DbContextOptions<AuditingDbContext> options)
		{
			_options = options;
		}

		public AuditingDbContext CreateDbContext()
		{
			return new AuditingDbContext(_options);
		}
	}
}
