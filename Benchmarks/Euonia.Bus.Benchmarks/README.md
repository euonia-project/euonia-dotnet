# Euonia.Bus Benchmarks

基于 [BenchmarkDotNet](https://benchmarkdotnet.org/) 的性能基准，覆盖：

- `BusDispatchBenchmarks`：`PublishAsync`、`SendAsync`、`CallAsync` 的分发开销与分配；
- `SerializationBenchmarks`：System.Text.Json 与 Newtonsoft.Json 的序列化/反序列化开销。

## 运行

```bash
# 全部基准
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj

# 只跑分发
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*BusDispatchBenchmarks*'

# 只跑序列化
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*SerializationBenchmarks*'

# 快速冒烟（不作为正式结果）
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*BusDispatchBenchmarks*' --job Dry
```

> 正式基准请使用 `Release` 且不要在共享 CI 容器中运行；`ShortRunJob` 只用于开发阶段快速对比。
