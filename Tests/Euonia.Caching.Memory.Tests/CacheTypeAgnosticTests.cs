namespace Nerosoft.Euonia.Caching.Tests;

/// <summary>
/// 验证缓存是「按键索引」的，而不是「按值类型分区」的。
/// </summary>
/// <remarks>
/// <see cref="ICacheService"/> 以 <c>TValue</c> 为泛型参数，但类型参数只应影响类型化的 API 形态，
/// 不应改变键所在的存储。此前 <c>MemoryCacheHandle</c> 的构造函数会为每个
/// <c>TCacheValue</c> 各建一个 <c>MemoryCache</c> 实例，于是任何跨类型的读/删操作
/// （例如框架自身 <c>CacheGroupManager.Evict</c> 使用的 <c>Remove&lt;object&gt;</c>）
/// 都会落到另一个存储上，变成静默空操作。
/// </remarks>
public class CacheTypeAgnosticTests
{
	private readonly ICacheService _service;

	public CacheTypeAgnosticTests(ICacheService service)
	{
		_service = service;
	}

	[Fact]
	public void Remove_WithDifferentValueType_ShouldStillRemoveEntry()
	{
		var key = nameof(Remove_WithDifferentValueType_ShouldStillRemoveEntry);

		_service.AddOrUpdate(key, new SampleValue { Name = "cached" });

		// 失效方通常并不知道写入时的泛型实参：框架自身的 CacheGroupManager 就是用 Remove<object>。
		var removed = _service.Remove<object>(key);

		Assert.True(removed, "Removing with a different TValue must still remove the entry.");
		Assert.False(_service.TryGet<SampleValue>(key, out _));
	}

	/// <summary>
	/// 锁定契约：**读取**必须使用与写入相同的 <c>TValue</c>；跨类型读取不返回条目。
	/// </summary>
	/// <remarks>
	/// 条目以 <c>CacheItem&lt;TValue&gt;</c> 存储，而该类型不协变，
	/// 因此 <c>TryGet&lt;object&gt;</c> 无法读出以具体类型写入的条目。
	/// 要支持跨类型读取需要把存储类型擦除（改为存放非泛型的条目基类），
	/// 这会波及背板序列化与 Redis 值转换，属于独立的设计变更，此处仅锁定现状。
	/// 与之相对，<c>Remove</c>/<c>Exists</c> 不涉及值类型转换，应保持类型无关（见上一条测试）。
	/// </remarks>
	[Fact]
	public void TryGet_WithDifferentValueType_DoesNotFindEntry()
	{
		var key = nameof(TryGet_WithDifferentValueType_DoesNotFindEntry);

		_service.AddOrUpdate(key, new SampleValue { Name = "cached" });

		Assert.False(_service.TryGet<object>(key, out _));

		// 但用写入时的类型读取仍然正常。
		Assert.True(_service.TryGet<SampleValue>(key, out var value));
		Assert.Equal("cached", value.Name);
	}

	/// <summary>
	/// 同一类型下的正常读写仍应工作（确保上述改动没有把存储变成「全类型共享同一份误命中」）。
	/// </summary>
	[Fact]
	public void AddOrUpdate_AndGet_WithinSameValueType_StillWorks()
	{
		var key = nameof(AddOrUpdate_AndGet_WithinSameValueType_StillWorks);

		_service.AddOrUpdate(key, new SampleValue { Name = "value" });
		var value = _service.Get<SampleValue>(key);

		Assert.Equal("value", value.Name);
	}

	private sealed class SampleValue
	{
		public string Name { get; set; }
	}
}
