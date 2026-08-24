namespace Nerosoft.Euonia.Caching.Tests;

/// <summary>
/// 针对 <see cref="Nerosoft.Euonia.Caching.Memory.MemoryCacheService"/> 的测试。
/// </summary>
public class MemoryCacheServiceTest
{
	private readonly ICacheService _service;

	public MemoryCacheServiceTest(ICacheService service)
	{
		_service = service;
	}

	[Fact]
	public void TestGetOrAdd_ShouldReturnValueAndCacheIt()
	{
		var key = nameof(TestGetOrAdd_ShouldReturnValueAndCacheIt);
		var factoryCalls = 0;

		var first = _service.GetOrAdd(key, () =>
		{
			Interlocked.Increment(ref factoryCalls);
			return "value-1";
		});

		var second = _service.GetOrAdd(key, () =>
		{
			Interlocked.Increment(ref factoryCalls);
			return "value-2";
		});

		Assert.Equal("value-1", first);
		Assert.Equal("value-1", second);
		Assert.Equal(1, factoryCalls);
	}

	[Fact]
	public void TestAddOrUpdate_ShouldUpdateExistingValue()
	{
		var key = nameof(TestAddOrUpdate_ShouldUpdateExistingValue);

		_service.AddOrUpdate(key, "old-value");
		var updated = _service.AddOrUpdate(key, "new-value");

		Assert.Equal("new-value", updated);
		Assert.Equal("new-value", _service.Get<string>(key));
	}

	[Fact]
	public void TestTryGet_ShouldReturnCachedValue()
	{
		var key = nameof(TestTryGet_ShouldReturnCachedValue);

		_service.AddOrUpdate(key, "cached-value");

		var found = _service.TryGet<string>(key, out var value);

		Assert.True(found);
		Assert.Equal("cached-value", value);
	}

	[Fact]
	public void TestTryGet_ShouldReturnFalse_WhenKeyDoesNotExist()
	{
		var found = _service.TryGet<string>(nameof(TestTryGet_ShouldReturnFalse_WhenKeyDoesNotExist), out var value);

		Assert.False(found);
		Assert.Null(value);
	}

	[Fact]
	public void TestRemove_ShouldRemoveValue()
	{
		var key = nameof(TestRemove_ShouldRemoveValue);

		_service.AddOrUpdate(key, "to-remove");

		var removed = _service.Remove<string>(key);

		Assert.True(removed);
		Assert.False(_service.TryGet<string>(key, out _));
	}

	[Fact]
	public async Task TestTryGetAsync_ShouldReturnValueAndFlag()
	{
		var key = nameof(TestTryGetAsync_ShouldReturnValueAndFlag);

		_service.AddOrUpdate(key, "async-value");

		var (found, value) = await _service.TryGetAsync<string>(key, TestContext.Current.CancellationToken);

		Assert.True(found);
		Assert.Equal("async-value", value);
	}

	[Fact]
	public async Task TestGetOrAddAsync_ShouldCacheValue()
	{
		var key = nameof(TestGetOrAddAsync_ShouldCacheValue);
		var factoryCalls = 0;

		var first = await _service.GetOrAddAsync(key, async () =>
		{
			Interlocked.Increment(ref factoryCalls);
			await Task.Yield();
			return "async-value";
		}, cancellationToken: TestContext.Current.CancellationToken);

		var second = await _service.GetOrAddAsync(key, async () =>
		{
			Interlocked.Increment(ref factoryCalls);
			await Task.Yield();
			return "another-value";
		}, cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal("async-value", first);
		Assert.Equal("async-value", second);
		Assert.Equal(1, factoryCalls);
	}

	[Fact]
	public async Task TestAddOrUpdateAsync_ShouldUpdateValue()
	{
		var key = nameof(TestAddOrUpdateAsync_ShouldUpdateValue);

		var updated = await _service.AddOrUpdateAsync(key, async () =>
		{
			await Task.Yield();
			return "updated-value";
		}, cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal("updated-value", updated);
		Assert.Equal("updated-value", _service.Get<string>(key));
	}

	[Fact]
	public async Task TestGetOrAdd_ShouldExpireAfterTimeout()
	{
		var key = nameof(TestGetOrAdd_ShouldExpireAfterTimeout);

		_service.GetOrAdd(key, () => "expiring-value", TimeSpan.FromMilliseconds(300));

		Assert.True(_service.TryGet<string>(key, out _));

		var expired = false;
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (!_service.TryGet<string>(key, out _))
			{
				expired = true;
				break;
			}

			await Task.Delay(50, TestContext.Current.CancellationToken);
		}

		Assert.True(expired, "The cache item should have expired after the configured timeout.");
	}
}
