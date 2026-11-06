namespace Nerosoft.Euonia.Caching.Tests;

/// <summary>
/// 针对默认缓存管理器的测试，覆盖 <c>GetOrAdd</c> 的缓存命中、过期窗口与超时移除行为。
/// </summary>
public class DefaultCacheManagerTest
{
    private readonly ICacheManager _manager;
    private readonly ICacheClock _clock;
    private readonly ICacheSignal _signal;

    /// <summary>
    /// 注入待测缓存管理器及其时钟与失效信号，供各用例共享。
    /// </summary>
    public DefaultCacheManagerTest(ICacheManager manager, ICacheClock clock, ICacheSignal signal)
    {
        _manager = manager;
        _clock = clock;
        _signal = signal;
    }

    /// <summary>
    /// 验证通过 <c>GetOrAdd</c> 写入的值可以按键读回。
    /// </summary>
    [Fact]
    public void TestGetOrAdd()
    {
        const string key = nameof(TestGetOrAdd);
        _manager.GetOrAdd(key, context =>
        {
            context.Monitor(_signal.When(key));
            return "test";
        });

        var result = _manager.Get<string, string>(key);

        Assert.Equal("test", result);
    }

    /// <summary>
    /// 验证在五秒过期窗口内读取缓存项仍然命中并返回原值。
    /// </summary>
    [Fact]
    public async Task TestGetOrAdd_WithTimeout()
    {
        const string key = nameof(TestGetOrAdd_WithTimeout);
        _manager.GetOrAdd(key, context =>
        {
            context.Monitor(_signal.When(key));
            context.Monitor(_clock.When(TimeSpan.FromSeconds(5)));
            return "test";
        });

        await Task.Delay(3000, TestContext.Current.CancellationToken);

        var result = _manager.Get<string, string>(key);

        Assert.Equal("test", result);
    }

    /// <summary>
    /// 验证超过五秒过期窗口后缓存项被移除，读取返回 <c>null</c>。
    /// </summary>
    [Fact]
    public async Task TestGetOrAdd_OverTimeout()
    {
        const string key = nameof(TestGetOrAdd_OverTimeout);

        _manager.GetOrAdd(key, context =>
        {
            context.Monitor(_signal.When(key));
            context.Monitor(_clock.When(TimeSpan.FromSeconds(5)));
            return "test";
        });

        await Task.Delay(8000, TestContext.Current.CancellationToken);

        var result = _manager.Get<string, string>(key);

        Assert.Null(result);
    }
}