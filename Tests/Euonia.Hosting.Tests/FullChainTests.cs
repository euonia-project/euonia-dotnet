using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 租户 HTTP 请求 → EF 写入 → 审计 → 缓存键的全链路。
/// </summary>
public class FullChainTests
{
	/// <summary>
	/// 请求内的写库落到 <c>tenant_id=t1</c>，审计记 t1，缓存键带 <c>t:t1:</c> 前缀。
	/// </summary>
	[Fact]
	public async Task Request_ShouldWriteTenantAuditAndCacheKey()
	{
		using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync(TestContext.Current.CancellationToken);

		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddOptions();
		builder.Services.AddLogging();
		builder.Services.AddScoped<DefaultRequestContextAccessor>();
		builder.Services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		builder.Services.AddTenancyAspNetCore(options =>
		{
			options.Enabled = true;
			options.RequireTenant = true;
			options.TrustHeaderForAnonymous = true;
		});
		builder.Services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "T1", Status = TenantStatus.Active }]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddInMemoryAuditingStore();
		builder.Services.AddDbContext<OrdersContext>(options => options.UseSqlite(connection)
		                                                               .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyFactory>());

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();

		app.MapPost("/orders", async (OrdersContext db, ITenantAccessor accessor, IKeyTransformer keys, CancellationToken cancellationToken) =>
		{
			db.Orders.Add(new Order { Name = "o1" });
			await db.SaveChangesAsync(cancellationToken);
			return Results.Ok(new { tenant = accessor.TenantId, key = keys.Transform("orders:1") });
		});

		await app.StartAsync(TestContext.Current.CancellationToken);

		using (var scope = app.Services.CreateScope())
		{
			await scope.ServiceProvider.GetRequiredService<OrdersContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
		}

		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
		request.Headers.Add("X-Tenant-Id", "t1");
		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.True(response.IsSuccessStatusCode, body);
		Assert.Contains("t:t1:orders:1", body);

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<OrdersContext>();
			var tenants = new List<string>();
			await using var command = db.Database.GetDbConnection().CreateCommand();
			command.CommandText = "SELECT * FROM orders";
			await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
			var tenantOrdinal = -1;
			for (var index = 0; index < reader.FieldCount; index++)
			{
				if (reader.GetName(index).Contains("tenant", StringComparison.OrdinalIgnoreCase))
				{
					tenantOrdinal = index;
					break;
				}
			}

			Assert.True(tenantOrdinal >= 0, "orders 表缺少租户列");
			while (await reader.ReadAsync(TestContext.Current.CancellationToken))
			{
				tenants.Add(reader.GetString(tenantOrdinal));
			}

			Assert.Equal(["t1"], tenants);
		}

		var recorder = app.Services.GetRequiredService<IAuditingRecorder>();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);
		var record = Assert.Single(app.Services.GetRequiredService<InMemoryAuditingStore>().Records);
		Assert.Equal("t1", record.TenantId);
		Assert.Equal("/orders", record.Url);

		await app.StopAsync(TestContext.Current.CancellationToken);
	}

	private sealed class OrdersContext : MultiTenantDataContextBase<OrdersContext>
	{
		public OrdersContext(DbContextOptions<OrdersContext> options, ITenantAccessor tenantAccessor, IOptions<MultiTenancyOptions> tenancyOptions)
			: base(options, tenantAccessor, tenancyOptions)
		{
		}

		public DbSet<Order> Orders => Set<Order>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<Order>().ToTable("orders");
		}
	}

	private sealed class Order : IMultiTenant
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
	}
}
