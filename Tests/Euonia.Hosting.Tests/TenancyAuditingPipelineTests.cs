using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Telemetry;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// HTTP 管道跨能力顺序：执行上下文 → 审计 → 租户。
/// </summary>
public class TenancyAuditingPipelineTests
{
	/// <summary>
	/// 停用租户：暴露存在性时返回 403，审计记录失败码。
	/// </summary>
	[Fact]
	public async Task SuspendedTenant_ShouldReturn403()
	{
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
			options.ExposeTenantExistence = true;
		});
		builder.Services.AddInMemoryTenantStore([new TenantInfo { Id = "t2", Name = "Suspended", Status = TenantStatus.Suspended }]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddInMemoryAuditingStore();

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();
		app.MapGet("/orders", () => Results.Ok());

		await app.StartAsync(TestContext.Current.CancellationToken);
		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/orders");
		request.Headers.Add("X-Tenant-Id", "t2");
		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

		var recorder = app.Services.GetRequiredService<IAuditingRecorder>();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);
		var record = Assert.Single(app.Services.GetRequiredService<InMemoryAuditingStore>().Records);
		Assert.Contains("tenant.failure", record.ExtraProperties);

		await app.StopAsync(TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// 审计存储不可用时业务仍然成功（失败开放）。
	/// </summary>
	[Fact]
	public async Task AuditStoreFailure_ShouldNotAffectBusiness()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddOptions();
		builder.Services.AddLogging();
		builder.Services.AddScoped<DefaultRequestContextAccessor>();
		builder.Services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		builder.Services.AddTenancyAspNetCore(options =>
		{
			options.Enabled = true;
			options.RequireTenant = false;
		});
		builder.Services.AddInMemoryTenantStore([]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddSingleton<IAuditingStore>(new ThrowingStore());

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();
		app.MapGet("/orders", () => Results.Ok("ok"));

		await app.StartAsync(TestContext.Current.CancellationToken);
		using var client = app.GetTestClient();
		using var response = await client.GetAsync("/orders", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("ok", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

		await app.StopAsync(TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// 遥测关闭时业务成功，审计仍然落库。
	/// </summary>
	[Fact]
	public async Task TelemetryDisabled_ShouldStillAudit()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddOptions();
		builder.Services.AddLogging();
		builder.Services.AddScoped<DefaultRequestContextAccessor>();
		builder.Services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		builder.Services.AddTenancyAspNetCore(options =>
		{
			options.Enabled = true;
			options.RequireTenant = false;
		});
		builder.Services.AddInMemoryTenantStore([]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddInMemoryAuditingStore();
		builder.Services.AddEuoniaTelemetry(options => options.Enabled = false);

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();
		app.MapGet("/orders", () => Results.Ok("ok"));

		await app.StartAsync(TestContext.Current.CancellationToken);
		using var client = app.GetTestClient();
		using var response = await client.GetAsync("/orders", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var recorder = app.Services.GetRequiredService<IAuditingRecorder>();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);
		var store = app.Services.GetRequiredService<InMemoryAuditingStore>();
		Assert.Single(store.Records);

		await app.StopAsync(TestContext.Current.CancellationToken);
	}

	private sealed class ThrowingStore : IAuditingStore
	{
		public Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default)
		{
			throw new InvalidOperationException("audit store unavailable");
		}
	}

	/// <summary>
	/// 未知租户：返回 404，审计记录带 tenant.failure。
	/// </summary>
	[Fact]
	public async Task UnknownTenant_ShouldReturn404AndAuditFailure()
	{
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
		builder.Services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "Tenant One", Status = TenantStatus.Active }]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddInMemoryAuditingStore();

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();
		app.MapGet("/orders", () => Results.Ok());

		await app.StartAsync(TestContext.Current.CancellationToken);

		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/orders");
		request.Headers.Add("X-Tenant-Id", "missing");

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		var recorder = app.Services.GetRequiredService<IAuditingRecorder>();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);

		var store = app.Services.GetRequiredService<InMemoryAuditingStore>();
		var record = Assert.Single(store.Records);
		Assert.Null(record.TenantId);
		Assert.Equal(404, record.HttpStatusCode);
		Assert.False(record.IsSuccess);
		Assert.Contains("TenantNotFound", record.ExtraProperties);

		await app.StopAsync(TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// 租户头解析出作用域，处理器可读取租户；请求结束后审计记录带租户与状态码。
	/// </summary>
	[Fact]
	public async Task Request_ShouldResolveTenantAndWriteAudit()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();

		builder.Services.AddOptions();
		builder.Services.AddLogging();
		builder.Services.AddScoped<DefaultRequestContextAccessor>();
		builder.Services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();

		builder.Services.AddTenancyAspNetCore(options =>
		{
			options.Enabled = true;
			options.TrustHeaderForAnonymous = true;
		});
		builder.Services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "Tenant One", Status = TenantStatus.Active }]);
		builder.Services.AddAuditing(options => options.Enabled = true);
		builder.Services.AddInMemoryAuditingStore();

		var app = builder.Build();
		app.UseExecutionContext();
		app.UseAuditing();
		app.UseTenancy();

		app.MapGet("/orders", (ITenantAccessor accessor, HttpContext context) =>
			Results.Ok(new { tenant = accessor.TenantId, path = context.Request.Path.Value }));

		await app.StartAsync(TestContext.Current.CancellationToken);

		using var client = app.GetTestClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/orders");
		request.Headers.Add("X-Tenant-Id", "t1");

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.True(response.StatusCode == HttpStatusCode.OK, body);
		Assert.Contains("\"tenant\":\"t1\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

		var recorder = app.Services.GetRequiredService<IAuditingRecorder>();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);

		var store = app.Services.GetRequiredService<InMemoryAuditingStore>();
		var record = Assert.Single(store.Records);
		Assert.Equal("t1", record.TenantId);
		Assert.Equal(200, record.HttpStatusCode);
		Assert.Equal("/orders", record.Url);

		await app.StopAsync(TestContext.Current.CancellationToken);
	}
}
