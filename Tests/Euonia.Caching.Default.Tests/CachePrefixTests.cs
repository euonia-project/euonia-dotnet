using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Default.Tests;

/// <summary>
/// 缓存前缀删除：带租户前缀的键可被整批清理。
/// </summary>
public class CachePrefixTests
{
	/// <summary>
	/// 只删除匹配前缀的键，其他租户与普通键不受影响。
	/// </summary>
	[Fact]
	public async Task RemoveByPrefixAsync_ShouldRemoveOnlyMatchingKeys()
	{
		var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
			        .WithMemoryCacheHandle("prefix-tests");
		});

		using var cache = CacheFactory.FromConfiguration<object>(configuration);
		cache.Put("t:a:order:1", "1");
		cache.Put("t:a:order:2", "2");
		cache.Put("t:b:order:1", "3");
		cache.Put("global:1", "4");

		var removed = await cache.RemoveByPrefixAsync("t:a:", TestContext.Current.CancellationToken);

		Assert.Equal(2, removed);
		Assert.False(cache.Exists("t:a:order:1"));
		Assert.True(cache.Exists("t:b:order:1"));
		Assert.True(cache.Exists("global:1"));
	}
}
