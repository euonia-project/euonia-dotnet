using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 无监听者时安全遥测的空操作开销。
/// </summary>
[MemoryDiagnoser]
public class SecurityTelemetryBenchmarks
{
    /// <summary>
    /// 启动并结束一次授权活动。
    /// </summary>
    [Benchmark]
    public void Authorize()
    {
        var activity = SecurityTelemetry.Start("orders.read");
        SecurityTelemetry.Complete(activity, true);
    }
}
