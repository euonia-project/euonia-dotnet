using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Telemetry;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 无监听者时遥测的空操作开销。
/// </summary>
[MemoryDiagnoser]
public class TelemetryBenchmarks
{
    private readonly TelemetrySource _source = new("Nerosoft.Euonia.Benchmarks");

    /// <summary>
    /// 启动并释放一个活动；无监听者时应接近零开销。
    /// </summary>
    [Benchmark]
    public void StartActivity()
    {
        using var activity = _source.StartActivity("benchmark");
    }
}
