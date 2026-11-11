using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 租户写入守卫的热路径。
/// </summary>
[MemoryDiagnoser]
public class TenantEntryGuardBenchmarks
{
    private readonly TenantEntryContext _context = new(
        TenantEntryOperation.Added,
        null,
        null,
        "t1",
        null,
        TenantStatus.Active,
        false,
        false);

    /// <summary>
    /// 新增实体的租户校验。
    /// </summary>
    /// <returns>应写入的租户标识。</returns>
    [Benchmark]
    public string? Validate()
    {
        return TenantEntryGuard.Validate(_context);
    }
}
