using System.Diagnostics;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

namespace Nerosoft.Euonia.Caching.Default.Tests;

/// <summary>
/// 缓存组件遥测：操作 Span 名称与低基数标签。
/// </summary>
public class CacheTelemetryTests
{
	/// <summary>
	/// 写入 / 读取 / 移除各产生一个缓存 Span，且不记录键。
	/// </summary>
	[Fact]
	public async Task Operations_ShouldEmitSpans()
	{
		var activities = new List<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == CacheTelemetry.SourceName,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				lock (activities)
				{
					activities.Add(activity);
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
			        .WithMemoryCacheHandle("telemetry-tests");
		});

		using var cache = CacheFactory.FromConfiguration<object>(configuration);
		cache.Put("order:1", "value");
		_ = cache.Get("order:1");
		cache.Remove("order:1");

		await Task.Yield();

		List<Activity> snapshot;
		lock (activities)
		{
			snapshot = activities.ToList();
		}

		Assert.Contains(snapshot, activity => activity.OperationName == CacheTelemetry.SetActivity);
		Assert.Contains(snapshot, activity => activity.OperationName == CacheTelemetry.GetActivity);
		Assert.Contains(snapshot, activity => activity.OperationName == CacheTelemetry.RemoveActivity);

		foreach (var activity in snapshot)
		{
			Assert.NotNull(activity.GetTagItem("cache.operation"));
			Assert.NotNull(activity.GetTagItem("cache.system"));
			Assert.Null(activity.GetTagItem("cache.key"));
		}
	}
}
