using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.Tests.Tenancy;

/// <summary>
/// 高级隔离：DatabasePerTenant 连接路由、SharedSchema 模型缓存键、非基类上下文的租户过滤器。
/// </summary>
public sealed class TenantAdvancedEfTests
{
	/// <summary>
	/// 每租户连接串解析到独立数据库，数据互不可见。
	/// </summary>
	[Fact]
	public async Task DatabasePerTenant_ShouldRouteConnectionPerTenant()
	{
		var connectionA = new SqliteConnection("DataSource=file:tenant-a?mode=memory&cache=shared");
		var connectionB = new SqliteConnection("DataSource=file:tenant-b?mode=memory&cache=shared");
		connectionA.Open();
		connectionB.Open();

		try
		{
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddTenancy(options => options.Enabled = true);
			services.AddInMemoryTenantStore(
			[
				NewTenant("a", "DataSource=file:tenant-a?mode=memory&cache=shared"),
				NewTenant("b", "DataSource=file:tenant-b?mode=memory&cache=shared"),
			]);
			services.AddTenantEfCore<RoutedContext>();
			services.AddTenantDbContext<RoutedContext>((options, connection) => options.UseSqlite(connection));

			await using var provider = services.BuildServiceProvider();

			using (new TenantScopeFactory().Begin(NewTenant("a", "DataSource=file:tenant-a?mode=memory&cache=shared")))
			{
				await using var scope = provider.CreateAsyncScope();
				var context = scope.ServiceProvider.GetRequiredService<RoutedContext>();
				await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
				context.Orders.Add(new RoutedOrder { Name = "a-order" });
				await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			}

			using (new TenantScopeFactory().Begin(NewTenant("b", "DataSource=file:tenant-b?mode=memory&cache=shared")))
			{
				await using var scope = provider.CreateAsyncScope();
				var context = scope.ServiceProvider.GetRequiredService<RoutedContext>();
				await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
				Assert.Empty(context.Orders);
				context.Orders.Add(new RoutedOrder { Name = "b-order" });
				await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			}

			// 直连两个物理库校验：每个库只有自己的那一行。
			using var commandA = connectionA.CreateCommand();
			commandA.CommandText = "SELECT COUNT(*) FROM orders WHERE name = 'a-order'";
			Assert.Equal(1L, (long)(await commandA.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);

			using var commandB = connectionB.CreateCommand();
			commandB.CommandText = "SELECT COUNT(*) FROM orders WHERE name = 'b-order'";
			Assert.Equal(1L, (long)(await commandB.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
		}
		finally
		{
			AmbientExecutionContext.Reset();
			connectionA.Dispose();
			connectionB.Dispose();
		}
	}

	/// <summary>
	/// SharedSchema 下模型缓存键包含 schema，不同租户不会复用同一模型。
	/// </summary>
	[Fact]
	public void ModelCacheKey_ShouldIncludeSchema()
	{
		AmbientExecutionContext.Reset();

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddTenancy(options => options.Enabled = true);
		services.AddSingleton<ITenantAccessor, TenantAccessor>();

		using var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<ITenantAccessor>();
		var factory = new TenantModelCacheKeyFactory();

		var options = new DbContextOptionsBuilder<RoutedContext>()
		              .UseSqlite("DataSource=:memory:")
		              .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyFactory>()
		              .Options;

		using var context = new RoutedContext(options, accessor, Options.Create(new MultiTenancyOptions { Enabled = true }));

		TenantModelCacheKeyContext.Set(TenantIsolation.SharedSchema, "tenant_a");
		var keyA = factory.Create(context, false);

		TenantModelCacheKeyContext.Set(TenantIsolation.SharedSchema, "tenant_b");
		var keyB = factory.Create(context, false);

		Assert.NotEqual(keyA, keyB);
	}

	/// <summary>
	/// SharedSchema 下模型默认 schema 随租户变化，且模型缓存键不同。
	/// </summary>
	[Fact]
	public void SharedSchema_ShouldSetDefaultSchemaPerTenant()
	{
		AmbientExecutionContext.Reset();

		var options = new DbContextOptionsBuilder<RoutedContext>()
		              .UseSqlite("DataSource=:memory:")
		              .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
		              .Options;

		var accessor = new TenantAccessor();
		var tenancy = Options.Create(new MultiTenancyOptions { Enabled = true });

		TenantModelCacheKeyContext.Set(TenantIsolation.SharedSchema, "tenant_a");
		using (new TenantScopeFactory().Begin(new TenantInfo
		       {
			       Id = "a",
			       Isolation = TenantIsolation.SharedSchema,
			       Schema = "tenant_a",
		       }))
		{
			using var context = new RoutedContext(options, accessor, tenancy);
			Assert.Equal("tenant_a", context.Model.GetDefaultSchema());
		}

		TenantModelCacheKeyContext.Set(TenantIsolation.SharedSchema, "tenant_b");
		using (new TenantScopeFactory().Begin(new TenantInfo
		       {
			       Id = "b",
			       Isolation = TenantIsolation.SharedSchema,
			       Schema = "tenant_b",
		       }))
		{
			using var context = new RoutedContext(options, accessor, tenancy);
			Assert.Equal("tenant_b", context.Model.GetDefaultSchema());
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// SharedSchema 的 DDL 使用租户 schema（SQL Server 提供程序，无需连接）。
	/// </summary>
	[Fact]
	public void SharedSchema_ShouldGenerateTenantSchemaDdl()
	{
		AmbientExecutionContext.Reset();

		var options = new DbContextOptionsBuilder<RoutedContext>()
		              .UseSqlServer("Server=localhost;Database=euonia;Trusted_Connection=True;TrustServerCertificate=True")
		              .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
		              .Options;

		var accessor = new TenantAccessor();
		var tenancy = Options.Create(new MultiTenancyOptions { Enabled = true });

		TenantModelCacheKeyContext.Set(TenantIsolation.SharedSchema, "tenant_a");
		using (new TenantScopeFactory().Begin(new TenantInfo
		       {
			       Id = "a",
			       Isolation = TenantIsolation.SharedSchema,
			       Schema = "tenant_a",
		       }))
		{
			using var context = new RoutedContext(options, accessor, tenancy);
			var script = context.Database.GenerateCreateScript();
			Assert.Contains("tenant_a", script);
			Assert.Contains("CREATE TABLE", script);
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 非 MultiTenantDataContextBase 的上下文也能通过 ApplyTenantModel 获得租户过滤器。
	/// </summary>
	[Fact]
	public void ApplyTenantModel_ShouldFilterPlainContext()
	{
		AmbientExecutionContext.Reset();
		var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();

		try
		{
			var options = new DbContextOptionsBuilder<PlainTenantContext>()
			              .UseSqlite(connection)
			              .Options;

			var accessor = new TenantAccessor();

			using (new TenantScopeFactory().Begin(NewTenant("a", null)))
			{
				using var context = new PlainTenantContext(options, accessor);
				context.Database.EnsureCreated();

				var order = new PlainOrder { Name = "a-order" };
				context.Orders.Add(order);
				context.Entry(order).Property("TenantId").CurrentValue = "a";
				context.SaveChanges();
			}

			using (new TenantScopeFactory().Begin(NewTenant("b", null)))
			{
				using var context = new PlainTenantContext(options, accessor);
				Assert.Empty(context.Orders);
			}

			using (new TenantScopeFactory().Begin(NewTenant("a", null)))
			{
				using var context = new PlainTenantContext(options, accessor);
				Assert.Equal("a-order", Assert.Single(context.Orders).Name);
			}
		}
		finally
		{
			AmbientExecutionContext.Reset();
			connection.Dispose();
		}
	}

	private static TenantInfo NewTenant(string id, string? connectionString)
	{
		return new TenantInfo
		{
			Id = id,
			Name = id,
			Status = TenantStatus.Active,
			ConnectionString = connectionString,
		};
	}

	/// <summary>
	/// DatabasePerTenant 测试上下文。
	/// </summary>
	public sealed class RoutedContext : MultiTenantDataContextBase<RoutedContext>
	{
		/// <summary>
		/// 初始化测试上下文。
		/// </summary>
		public RoutedContext(
			DbContextOptions<RoutedContext> options,
			ITenantAccessor tenantAccessor,
			IOptions<MultiTenancyOptions> tenancyOptions)
			: base(options, tenantAccessor, tenancyOptions)
		{
		}

		/// <summary>
		/// 获取订单集合。
		/// </summary>
		public DbSet<RoutedOrder> Orders => Set<RoutedOrder>();
	}

	/// <summary>
	/// DatabasePerTenant 测试实体。
	/// </summary>
	public sealed class RoutedOrder : IMultiTenant
	{
		/// <summary>
		/// 获取或设置主键。
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// 获取或设置名称。
		/// </summary>
		public string Name { get; set; } = null!;
	}

	/// <summary>
	/// 仅通过 ApplyTenantModel 接入的普通上下文。
	/// </summary>
	public sealed class PlainTenantContext : DbContext, ITenantFilterState
	{
		private readonly ITenantAccessor _accessor;

		/// <summary>
		/// 初始化普通上下文。
		/// </summary>
		public PlainTenantContext(DbContextOptions<PlainTenantContext> options, ITenantAccessor accessor)
			: base(options)
		{
			_accessor = accessor;
		}

		/// <inheritdoc />
		public string? CurrentTenantId => _accessor.TenantId;

		/// <inheritdoc />
		public bool IgnoreTenantFilter => _accessor.Current?.IsCrossTenant == true && _accessor.TenantId == null;

		/// <summary>
		/// 获取订单集合。
		/// </summary>
		public DbSet<PlainOrder> Orders => Set<PlainOrder>();

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyTenantModel(this);
			base.OnModelCreating(modelBuilder);
		}
	}

	/// <summary>
	/// 普通上下文测试实体。
	/// </summary>
	public sealed class PlainOrder : IMultiTenant
	{
		/// <summary>
		/// 获取或设置主键。
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// 获取或设置名称。
		/// </summary>
		public string Name { get; set; } = null!;
	}
}
