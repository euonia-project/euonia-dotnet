using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching.Tests;

/// <summary>
/// 针对字典缓存句柄的测试，重点验证句柄释放后后台过期扫描是否停止。
/// </summary>
public class DictionaryCacheHandleTest
{
	/// <summary>
	/// 验证缓存管理器释放后过期扫描不再运行，不会再触发条目移除事件。
	/// </summary>
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