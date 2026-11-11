using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 执行上下文安装与恢复（并发请求/消息的每消息开销）。
/// </summary>
[MemoryDiagnoser]
public class AmbientExecutionContextBenchmarks
{
    /// <summary>
    /// 推入并释放一个上下文作用域。
    /// </summary>
    [Benchmark]
    public void PushPop()
    {
        using var scope = AmbientExecutionContext.Push(values => values.TenantId = "t1");
    }
}
