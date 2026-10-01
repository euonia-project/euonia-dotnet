using System.Collections.Concurrent;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 属性注册缓存的并发护栏：
/// <list type="bullet">
/// <item>类型属性列表只能创建一次，所有线程拿到的是同一条实例（无分裂副本）；</item>
/// <item>快路径读必须是并发安全的——普通 <see cref="Dictionary{TKey,TValue}"/> 在另一个线程 <c>Add</c>（尤其扩容替换
/// <c>_buckets</c>/<c>_entries</c> 两次赋值之间）被读取，会读到「新桶配旧项」并抛出越界；</item>
/// <item>列表对外可见时，该类型的静态初始化器必须已经跑完（属性注册齐全）。</item>
/// </list>
/// </summary>
public class PropertyInfoManagerConcurrencyTests
{
	private const int ReaderCount = 6;
	private const int WriterCount = 6;

	/// <summary>
	/// 8 × 8 × 8 = 512 个互不相同的闭合类型，用于制造足够多的缓存写入与扩容。
	/// </summary>
	private static readonly Type[] MarkerTypes =
	[
		.. from a in Primitives()
		   from b in Primitives()
		   from c in Primitives()
		   select typeof(Marker<,,>).MakeGenericType(a, b, c)
	];

	private static Type[] Primitives() =>
	[
		typeof(int), typeof(string), typeof(long), typeof(bool),
		typeof(byte), typeof(short), typeof(double), typeof(decimal)
	];

	private sealed class Marker<T1, T2, T3>
	{
	}

	[Fact]
	public async Task Reading_The_Cache_While_Other_Threads_Publish_New_Types_Should_Not_Fault()
	{
		// 先预热一小批类型，读线程才有稳定的键可以持续读。
		var warm = MarkerTypes.Take(WriterCount).ToArray();
		foreach (var type in warm)
		{
			PropertyInfoManager.GetPropertyListCache(type);
		}

		var failures = new ConcurrentBag<Exception>();
		var published = new ConcurrentBag<(Type Type, PropertyInfoList List)>();
		var observed = new ConcurrentBag<PropertyInfoList[]>();
		using var start = new ManualResetEventSlim(false);

		var readers = Enumerable.Range(0, ReaderCount).Select(_ => Task.Run(() =>
		{
			start.Wait(TestContext.Current.CancellationToken);
			var deadline = Environment.TickCount64 + 400;
			var seen = new PropertyInfoList[warm.Length];

			try
			{
				while (Environment.TickCount64 < deadline)
				{
					for (var i = 0; i < warm.Length; i++)
					{
						seen[i] = PropertyInfoManager.GetPropertyListCache(warm[i]);
					}
				}
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
			finally
			{
				observed.Add(seen);
			}
		}, TestContext.Current.CancellationToken)).ToArray();

		var writers = Enumerable.Range(0, WriterCount).Select(writer => Task.Run(() =>
		{
			start.Set();

			try
			{
				foreach (var type in MarkerTypes.Where((_, index) => index % WriterCount == writer))
				{
					published.Add((type, PropertyInfoManager.GetPropertyListCache(type)));
					Thread.Yield();
				}
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
		}, TestContext.Current.CancellationToken)).ToArray();

		await Task.WhenAll(readers.Concat(writers));

		Assert.Empty(failures);
		Assert.Equal(ReaderCount, observed.Count);

		// 同一类型必须始终返回同一条列表实例（否则属性会写进分裂的副本里）。
		var perType = published
			.GroupBy(p => p.Type)
			.Select(g => g.Select(p => p.List).Distinct().Count());
		Assert.All(perType, count => Assert.Equal(1, count));

		for (var i = 0; i < warm.Length; i++)
		{
			var reference = observed.First()[i];
			Assert.NotNull(reference);
			Assert.All(observed, seen => Assert.Same(reference, seen[i]));
		}
	}
}
