namespace Nerosoft.Euonia.Caching.Tests;

public class DefaultCacheManagerTest
{
    private readonly ICacheManager _manager;
    private readonly ICacheClock _clock;
    private readonly ICacheSignal _signal;

    public DefaultCacheManagerTest(ICacheManager manager, ICacheClock clock, ICacheSignal signal)
    {
        _manager = manager;
        _clock = clock;
        _signal = signal;
    }

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