using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 租户缓存键转换的热路径开销。
/// </summary>
[MemoryDiagnoser]
public class TenantKeyTransformerBenchmarks
{
    private readonly TenantKeyTransformer _transformer = new();
    private IDisposable? _scope;

    /// <summary>
    /// 安装租户作用域。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _scope = new TenantScopeFactory().Begin(new TenantInfo { Id = "t1", Name = "Tenant One" });
    }

    /// <summary>
    /// 释放租户作用域。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
    }

    /// <summary>
    /// 带租户前缀的键转换。
    /// </summary>
    /// <returns>转换后的键。</returns>
    [Benchmark]
    public string Transform()
    {
        return _transformer.Transform("order:1");
    }
}
