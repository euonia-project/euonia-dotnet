using BenchmarkDotNet.Running;
using Euonia.Bus.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(BusDispatchBenchmarks).Assembly).Run(args);
