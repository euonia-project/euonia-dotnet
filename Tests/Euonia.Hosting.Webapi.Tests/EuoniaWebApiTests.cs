using System.Text.Json;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Hosting.Webapi;

namespace Nerosoft.Euonia.Hosting.Webapi.Tests;

/// <summary>
/// 针对 Web API 宿主注册与异常响应的测试。
/// </summary>
public class EuoniaWebApiTests
{
	/// <summary>
	/// 默认选项应启用异常处理、OpenAPI、健康检查与 HTTPS 重定向。
	/// </summary>
	[Fact]
	public void Options_Defaults_AreEnabled()
	{
		var options = new EuoniaWebApiOptions();

		Assert.True(options.EnableExceptionHandling);
		Assert.True(options.EnableOpenApi);
		Assert.True(options.EnableHealthChecks);
		Assert.True(options.EnableHttpsRedirection);
		Assert.Equal("/health", options.HealthCheckPath);
		Assert.Empty(options.AllowedOrigins);
	}

	/// <summary>
	/// 注册后应能解析选项并存在异常处理器。
	/// </summary>
	[Fact]
	public void AddEuoniaWebApi_RegistersOptionsAndExceptionHandler()
	{
		var services = new ServiceCollection();
		services.AddLogging();

		services.AddEuoniaWebApi(options => options.EnableOpenApi = false);

		using var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<EuoniaWebApiOptions>();

		Assert.False(options.EnableOpenApi);
		Assert.NotNull(provider.GetService<IExceptionHandler>());
	}

	/// <summary>
	/// 允许来源为 null 时应归一化为空数组。
	/// </summary>
	[Fact]
	public void AddEuoniaWebApi_NullAllowedOrigins_Normalizes()
	{
		var services = new ServiceCollection();

		services.AddEuoniaWebApi(options => options.AllowedOrigins = null!);

		using var provider = services.BuildServiceProvider();

		Assert.Empty(provider.GetRequiredService<EuoniaWebApiOptions>().AllowedOrigins);
	}

	/// <summary>
	/// 启用健康检查时空白路径应被拒绝。
	/// </summary>
	[Fact]
	public void AddEuoniaWebApi_BlankHealthCheckPath_Throws()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentException>(() =>
			services.AddEuoniaWebApi(options => options.HealthCheckPath = " "));
	}

	/// <summary>
	/// 配置允许来源后应注册默认 CORS 策略。
	/// </summary>
	[Fact]
	public void AddEuoniaWebApi_WithAllowedOrigins_RegistersCorsPolicy()
	{
		var services = new ServiceCollection();

		services.AddEuoniaWebApi(options => options.AllowedOrigins = ["https://example.com"]);

		using var provider = services.BuildServiceProvider();
		var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
		var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

		Assert.NotNull(policy);
		Assert.Contains("https://example.com", policy.Origins);
	}

	/// <summary>
	/// 参数异常应映射为 400，并写出统一 JSON 响应。
	/// </summary>
	[Fact]
	public async Task ExceptionHandler_MapsArgumentExceptionToBadRequest()
	{
		var handler = new EuoniaExceptionHandler(NullLogger<EuoniaExceptionHandler>.Instance);
		var context = new DefaultHttpContext
		{
			Response = { Body = new MemoryStream() }
		};

		var handled = await handler.TryHandleAsync(context, new ArgumentException("bad"), TestContext.Current.CancellationToken);

		Assert.True(handled);
		Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

		context.Response.Body.Position = 0;
		using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
		Assert.Equal("bad", document.RootElement.GetProperty("message").GetString());
	}
}
