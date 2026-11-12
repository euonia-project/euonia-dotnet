using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.Tests.Tenancy;

/// <summary>
/// 验证租户查询过滤器、写入打标与跨租户保护在 SQLite 上的行为。
/// </summary>
public sealed class TenantEfCoreTests : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<TenantTestContext> _options;
	private readonly TenantAccessor _accessor = new();
	private readonly IOptions<MultiTenancyOptions> _tenancyOptions =
		Options.Create(new MultiTenancyOptions { Enabled = true });

	/// <summary>
	/// 初始化测试并创建内存 SQLite 连接。
	/// </summary>
	public TenantEfCoreTests()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<TenantTestContext>()
		           .UseSqlite(_connection)
		           .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyFactory>()
		           .Options;

		using var context = CreateContext();
		context.Database.EnsureCreated();
	}

	/// <summary>
	/// 不同租户写入的数据彼此不可见。
	/// </summary>
	[Fact]
	public void QueryFilter_ShouldIsolateTenants()
	{
		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			using var context = CreateContext();
			context.Orders.Add(new TenantOrder { Name = "a-order" });
			context.SaveChanges();
		}

		using (new TenantScopeFactory().Begin(NewTenant("b")))
		{
			using var context = CreateContext();
			context.Orders.Add(new TenantOrder { Name = "b-order" });
			context.SaveChanges();
		}

		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			using var context = CreateContext();
			Assert.Equal("a-order", Assert.Single(context.Orders).Name);
		}

		using (new TenantScopeFactory().Begin(NewTenant("b")))
		{
			using var context = CreateContext();
			Assert.Equal("b-order", Assert.Single(context.Orders).Name);
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 新增实体自动写入当前租户（影子属性）。
	/// </summary>
	[Fact]
	public void SaveChanges_ShouldStampTenantId()
	{
		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			using var context = CreateContext();
			var order = new TenantOrder { Name = "order" };
			context.Orders.Add(order);
			context.SaveChanges();

			var tenantId = context.Entry(order).Property("TenantId").CurrentValue as string;
			Assert.Equal("a", tenantId);
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 无租户上下文时查询返回空集，写入被拒绝。
	/// </summary>
	[Fact]
	public void WithoutTenant_ShouldFailClosed()
	{
		AmbientExecutionContext.Reset();

		using var context = CreateContext();
		Assert.Empty(context.Orders);

		context.Orders.Add(new TenantOrder { Name = "order" });
		Assert.Throws<TenantRequiredException>(() => context.SaveChanges());
	}

	/// <summary>
	/// 跨租户写入在修改原有实体时应被拒绝。
	/// </summary>
	[Fact]
	public void CrossTenantUpdate_ShouldBeRejected()
	{
		int id;
		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			using var context = CreateContext();
			var order = new TenantOrder { Name = "order" };
			context.Orders.Add(order);
			context.SaveChanges();
			id = order.Id;
		}

		using (new TenantScopeFactory().Begin(NewTenant("b")))
		{
			using var context = CreateContext();
			var order = context.Orders.IgnoreQueryFilters(["Tenant"]).Single(x => x.Id == id);
			order.Name = "changed";
			Assert.Throws<CrossTenantOperationException>(() => context.SaveChanges());
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 软删除过滤器与租户过滤器同时生效。
	/// </summary>
	[Fact]
	public void TombstoneAndTenantFilters_ShouldCoexist()
	{
		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			using var context = CreateContext();
			context.Orders.Add(new TenantOrder { Name = "active" });
			context.Orders.Add(new TenantOrder { Name = "deleted", IsDeleted = true });
			context.SaveChanges();

			Assert.Equal("active", Assert.Single(context.Orders).Name);
		}

		AmbientExecutionContext.Reset();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_connection.Dispose();
		AmbientExecutionContext.Reset();
	}

	private TenantTestContext CreateContext()
	{
		return new TenantTestContext(_options, _accessor, _tenancyOptions);
	}

	private static TenantInfo NewTenant(string id)
	{
		return new TenantInfo { Id = id, Name = id, Status = TenantStatus.Active };
	}

	/// <summary>
	/// 测试用租户上下文。
	/// </summary>
	public sealed class TenantTestContext : MultiTenantDataContextBase<TenantTestContext>
	{
		/// <summary>
		/// 初始化测试上下文。
		/// </summary>
		/// <param name="options">上下文选项。</param>
		/// <param name="tenantAccessor">租户访问器。</param>
		/// <param name="tenancyOptions">多租户选项。</param>
		public TenantTestContext(
			DbContextOptions<TenantTestContext> options,
			ITenantAccessor tenantAccessor,
			IOptions<MultiTenancyOptions> tenancyOptions)
			: base(options, tenantAccessor, tenancyOptions)
		{
		}

		/// <summary>
		/// 获取订单集合。
		/// </summary>
		public DbSet<TenantOrder> Orders => Set<TenantOrder>();
	}

	/// <summary>
	/// 测试用订单实体；同时实现租户标记与软删除。
	/// </summary>
	public sealed class TenantOrder : IMultiTenant, ITombstone
	{
		/// <summary>
		/// 获取或设置主键。
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// 获取或设置名称。
		/// </summary>
		public string Name { get; set; } = null!;

		/// <inheritdoc />
		public bool IsDeleted { get; set; }
	}
}
