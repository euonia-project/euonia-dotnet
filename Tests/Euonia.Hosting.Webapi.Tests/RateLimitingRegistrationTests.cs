using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Webapi.Tests;

/// <summary>
/// 针对 Web API 流控注册的测试。
/// </summary>
public class RateLimitingRegistrationTests
{
	/// <summary>
	/// 注册后应能解析选项并设置拒绝状态码。
	/// </summary>
	[Fact]
	public void AddEuoniaRateLimiting_RegistersOptions()
	{
		var services = new ServiceCollection();

		services.AddEuoniaRateLimiting(options =>
		{
			options.DefaultLimiter.MinutePermitLimit = 1;
			options.PolicyName = "custom";
		});

		using var provider = services.BuildServiceProvider();

		Assert.Equal("custom", provider.GetRequiredService<EuoniaRateLimitingOptions>().PolicyName);

		var limiterOptions = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
		Assert.Equal(429, limiterOptions.RejectionStatusCode);
	}
}
