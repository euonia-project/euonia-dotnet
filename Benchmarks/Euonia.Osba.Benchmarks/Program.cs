using BenchmarkDotNet.Running;
using Euonia.Osba.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(ObjectLifecycleBenchmarks).Assembly).Run(args);
