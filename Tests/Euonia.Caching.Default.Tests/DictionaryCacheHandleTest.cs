using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching.Tests;

public class DictionaryCacheHandleTest
{
	[Fact]
	public async Task TestExpirationScan_StopsAfterDispose()
	{
		var manager = CacheFactory.Build("test", settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
			        .WithDictionaryHandle();
		});

		var removedAfterDispose = new List<CacheItemRemovedEventArgs>();

		manager.OnRemoveByHandle += (_, args) => removedAfterDispose.Add(args);

		manager.Add(new CacheItem<object>("key", "value", CacheExpirationMode.Absolute, TimeSpan.FromMilliseconds(100)));
		manager.Dispose();

		// 扫描间隔为 5 秒（初始延迟 1~5 秒），等待足够长时间以便暴露释放后仍在运行的定时器
		var deadline = DateTime.UtcNow.AddSeconds(7);
		while (DateTime.UtcNow < deadline)
		{
			if (removedAfterDispose.Count > 0)
			{
				break;
			}

			await Task.Delay(100, TestContext.Current.CancellationToken);
		}

		Assert.Empty(removedAfterDispose);
	}
}