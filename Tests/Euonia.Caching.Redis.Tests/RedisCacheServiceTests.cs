using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis.Tests;

/// <summary>
/// 针对 Redis 缓存后端的测试。
/// </summary>
/// <remarks>
/// 该后端此前**完全不可用且没有任何测试覆盖**：<c>WithRedisCacheHandle</c> 未把连接字符串
/// 作为句柄构造参数传入，导致 <c>RedisCacheHandle</c> 的构造函数永远匹配不上，
/// 任何一次读写都会在句柄创建阶段抛 <c>InvalidOperationException</c>。
/// <para>
/// 需要可访问的 Redis。不可访问时显式跳过（<see cref="Assert.Skip"/>），
/// 而不是让测试静默通过——正是「没有测试」让上述缺陷长期存在。
/// </para>
/// </remarks>
public class RedisCacheServiceTests
{
	private const string DefaultConnectionString = "127.0.0.1:6399";

	/// <summary>
	/// 核心冒烟测试：句柄能被构造，且写入后可读回（回归「后端完全不可用」）。
	/// </summary>
	[Fact]
	public async Task AddOrUpdate_And_Get_RoundTrips()
	{
		var (provider, connection) = await BuildProviderAsync();
		await using var scope = provider;

		var cache = provider.GetRequiredService<ICacheService>();
		var key = $"redis-roundtrip-{Guid.NewGuid():N}";

		try
		{
			cache.AddOrUpdate(key, new SampleValue { Name = "cached" });

			var value = cache.Get<SampleValue>(key);
			Assert.Equal("cached", value.Name);
		}
		finally
		{
			await CleanupAsync(connection, key);
		}
	}

	/// <summary>
	/// 移除是类型无关的：失效方通常不知道写入时的泛型实参。
	/// </summary>
	[Fact]
	public async Task Remove_WithDifferentValueType_RemovesEntry()
	{
		var (provider, connection) = await BuildProviderAsync();
		await using var scope = provider;

		var cache = provider.GetRequiredService<ICacheService>();
		var key = $"redis-cross-type-{Guid.NewGuid():N}";

		try
		{
			cache.AddOrUpdate(key, new SampleValue { Name = "cached" });

			Assert.True(cache.Remove<object>(key));
			Assert.False(cache.TryGet<SampleValue>(key, out _));
		}
		finally
		{
			await CleanupAsync(connection, key);
		}
	}

	/// <summary>
	/// <c>Clear()</c> 的实现是 <c>FLUSHDB</c>，需要连接启用 <c>allowAdmin</c>；
	/// 未启用时必须给出**可操作**的说明，而不是让驱动的原始错误直接冒泡
	/// （此前只捕获 <c>NotSupportedException</c>，而驱动抛的是 <c>RedisCommandException</c>，
	/// 导致提示分支成为死代码）。
	/// </summary>
	[Fact]
	public async Task Clear_WithoutAdminMode_ThrowsActionableNotSupported()
	{
		var (provider, connection) = await BuildProviderAsync();
		await using var scope = provider;

		var configured = ConfigurationBuilder.BuildConfiguration(settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
			        .WithRedisConfiguration("clear-test", ConnectionString, 0)
			        .WithRedisCacheHandle("clear-test");
		});

		var manager = CacheFactory.FromConfiguration<string>(configured);

		var exception = Assert.Throws<NotSupportedException>(() => manager.Clear());

		Assert.Contains("allowAdmin", exception.Message);
		Assert.Contains("FLUSHDB", exception.Message);

		await Task.CompletedTask;
	}

	private static string ConnectionString =>
		Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? DefaultConnectionString;

	private static async Task<(ServiceProvider Provider, IConnectionMultiplexer Connection)> BuildProviderAsync()
	{
		var connection = await TryConnectAsync();
		if (connection == null)
		{
			Assert.Skip($"Requires a reachable Redis at '{ConnectionString}'. Set REDIS_CONNECTION to override.");
		}

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<RedisCacheOptions>(options =>
		{
			options.ConnectionString = ConnectionString;
			options.Database = 0;
		});
		services.AddSingleton<ICacheService, RedisCacheService>();

		return (services.BuildServiceProvider(), connection);
	}

	private static async Task<IConnectionMultiplexer> TryConnectAsync()
	{
		try
		{
			var options = ConfigurationOptions.Parse(ConnectionString);
			options.ConnectTimeout = 1000;
			options.ConnectRetry = 1;
			options.AbortOnConnectFail = false;

			var connection = await ConnectionMultiplexer.ConnectAsync(options);
			return connection.IsConnected ? connection : null;
		}
		catch
		{
			return null;
		}
	}

	private static async Task CleanupAsync(IConnectionMultiplexer connection, string key)
	{
		try
		{
			await connection.GetDatabase().KeyDeleteAsync(key);
		}
		catch
		{
			// 清理失败不应影响测试结论。
		}
	}

	public sealed class SampleValue
	{
		public string Name { get; set; }
	}
}
