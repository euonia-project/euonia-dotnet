# Euonia.Bus.Benchmarks

`Euonia.Bus` 的分发开销与消息序列化性能基准：用空传输器把总线框架成本与真实传输成本隔离开。

- **目标框架**：`net10.0`
- **入口**：`Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)`
- **运行方式**：`dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj`
- **NuGet 包**：无（`IsPackable=false`）

## 前置条件

- .NET SDK（支持 `net10.0`）与 BenchmarkDotNet（随项目引用还原）。
- 不需要 RabbitMQ、Redis 等外部服务：基准使用进程内的 `NoopTransporter`（名称 `bench`）。
- 正式测量请用 `Release` 且独占机器。

## 基准类

| 基准类 | 方法 | 测量内容 |
|--------|------|----------|
| `BusDispatchBenchmarks` | `PublishAsync` | `IBus.PublishAsync` 发布多播事件 `BenchEvent` 的端到端开销 |
| | `SendAsync` | `IBus.SendAsync` 发送单播命令 `BenchCommand` |
| | `CallAsync` | `IBus.CallAsync<BenchRequest, int>` 请求-响应往返 |
| | `DirectTransportPublish` | 绕过 pipeline/路由直接调用传输器，得出总线框架的净开销 |
| | `ResolvePipeline` | 每次分发从 DI 解析 `IPipeline<IMessageEnvelope<BenchEvent>, Unit>` 的成本 |
| | `CreateEnvelope` | `RoutedMessage<T>` 信封构造（含默认 ID / 元数据初始化） |
| | `ParallelOne` | 单元素 `Parallel.ForEachAsync` 的固定开销 |
| `SerializationBenchmarks` | `SystemTextJson_Serialize` / `SystemTextJson_Deserialize` | 内置 `SystemTextJsonSerializer` 的往返（前者为对比基线） |
| | `NewtonsoftJson_Serialize` / `NewtonsoftJson_Deserialize` | 内置 `NewtonsoftJsonSerializer` 的往返 |

两个基准类都标注 `[MemoryDiagnoser]` 与 `[ShortRunJob]`：默认迭代次数较少，适合本地快速对比；需要更稳的结论时显式指定 `--job medium` 或 `--job long`。

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

> 正式基准请使用 `Release` 且不要在共享 CI 容器中运行；`ShortRunJob` / `--job Dry` 只用于开发阶段快速对比。

## 使用示例

定位“总线框架开销 vs 传输开销”：

```bash
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*PublishAsync*' '*DirectTransportPublish*' --exporters json
```

两者的差值近似为 pipeline 解析、路由与信封处理的总成本；若 `ResolvePipeline` 同步上升，通常是 DI 生命周期或约定注册发生了变化。

## 注意事项 / 限制

- `NoopTransporter` 立即返回，基准**不覆盖**网络、序列化边界与 broker 确认等真实传输成本。
- `SerializationBenchmarks` 使用固定的 `BenchMessage`（`string` / `decimal` / `string[]`），不覆盖多态、循环引用或大对象。
- 两个基准类都带 `[ShortRunJob]`；去掉它或改 `--job` 后数字会变化，跨次对比必须保持 job 一致。
- 基准在 `[GlobalSetup]` 中手工装配 DI（`AddEuoniaBus()` + `SetConvention` + keyed `ITransporter`），总线 API 变更时需同步更新这里。
- 跨机器、跨 SDK 版本的数字不可直接比较；回归判断应使用同一机器上的前后两次 `Release` 结果。
- 结论不能外推到 `Euonia.Osba.Benchmarks` / `Euonia.Platform.Benchmarks` 的场景。

## 相关包

- [`Euonia.Bus`](../../Source/Euonia.Bus/README.md) —— 总线抽象、`IConfigurator`、序列化器
- [`Euonia.Bus.Abstract`](../../Source/Euonia.Bus.Abstract/README.md) —— 消息与信封契约
- [`Euonia.Modularity`](../../Source/Euonia.Modularity/README.md) —— 基准中的服务装配依赖
- [`Euonia.Bus.InMemory`](../../Source/Euonia.Bus.InMemory/README.md) —— 真实传输实现（非基准使用）
- [`Euonia.Osba.Benchmarks`](../Euonia.Osba.Benchmarks/README.md) —— 业务对象基准
- [`Euonia.Platform.Benchmarks`](../Euonia.Platform.Benchmarks/README.md) —— 平台能力基准

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
