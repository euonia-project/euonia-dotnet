# Euonia.Bus.Benchmarks API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

用空传输器把总线框架开销与真实传输开销分离，从而定位 `PublishAsync` / `SendAsync` / `CallAsync` 的回归来源。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Bus.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`；被测 `Euonia.Bus` / `Euonia.Bus.Abstract` / `Euonia.Modularity` |
| 完整参考 | [`Benchmarks/Euonia.Bus.Benchmarks/README.md`](../../../Benchmarks/Euonia.Bus.Benchmarks/README.md) |

## API 参考

### 基准类与方法

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `BusDispatchBenchmarks.PublishAsync()` | 发布多播事件端到端 | 类级 `[ShortRunJob]`（迭代较少） |
| `BusDispatchBenchmarks.SendAsync()` | 发送单播命令 | 同上 |
| `BusDispatchBenchmarks.CallAsync()` | 请求-响应往返 | 同上 |
| `BusDispatchBenchmarks.DirectTransportPublish()` | 绕过 pipeline 直调传输器 | 框架净开销基线 |
| `BusDispatchBenchmarks.ResolvePipeline()` | DI 解析 `IPipeline<IMessageEnvelope<BenchEvent>, Unit>` | 生命周期回归探针 |
| `BusDispatchBenchmarks.CreateEnvelope()` | `RoutedMessage<T>` 构造 | 信封元数据成本 |
| `BusDispatchBenchmarks.ParallelOne()` | 单元素 `Parallel.ForEachAsync` | 并行固定开销 |
| `SerializationBenchmarks.SystemTextJson_Serialize()` | STJ 序列化 | 该组基线 |
| `SerializationBenchmarks.SystemTextJson_Deserialize()` | STJ 反序列化 | 复用预生成载荷 |
| `SerializationBenchmarks.NewtonsoftJson_Serialize/Deserialize` | Newtonsoft 往返 | 与 STJ 对照 |

### 命令行参数

| 参数 | 说明 | 示例 |
|---|---|---|
| `--filter` | 按完整名称通配匹配 | `--filter '*SerializationBenchmarks*'` |
| `--job` | 迭代规模；`Dry` 只冒烟 | `--job Dry` / `--job medium` |
| `--exporters` | 导出格式 | `--exporters json` |
| `--artifacts` | 报告输出目录 | `--artifacts ./artifacts/after` |

### 配置项

本项目**没有** `appsettings` 配置：全部行为由命令行参数与基准类特性控制，不读取 `Euonia:*` 节。

## 相关文档

- [教程](TUTORIAL.md)
- [`Benchmarks/Euonia.Bus.Benchmarks` README](../../../Benchmarks/Euonia.Bus.Benchmarks/README.md) —— 环境要求与方法清单
- [`Source/Euonia.Bus/README.md`](../../../Source/Euonia.Bus/README.md) —— `IConfigurator` / 传输器 / 序列化器
- [Osba 基准手册](../Euonia.Osba.Benchmarks/API.md) —— 另一套门禁指标
- [Platform 基准手册](../Euonia.Platform.Benchmarks/API.md) —— 租户与遥测热路径
- [根 README](../../../README.md)
