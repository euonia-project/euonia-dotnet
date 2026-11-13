using BenchmarkDotNet.Running;

namespace Nerosoft.Euonia.Platform.Benchmarks;

/// <summary>
/// 平台能力基准入口。
/// </summary>
public static class Program
{
    /// <summary>
    /// 运行全部基准。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
