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

## 常见问题与故障排查

### `Setup` 阶段抛 `MessageTransportException`（传输器未注册）

**原因**：基准把默认传输器固定为 `bench` 并注册同名 keyed 传输器（`BusDispatchBenchmarks.cs:35,38`），改常量或删掉注册后总线解析不到它。
**解决**：保持 `MessageBusOptions.DefaultTransporter` 与 `AddKeyedSingleton<ITransporter>("bench", ...)` 名称一致；改名前同步改两处。

### 报告里的数字比上次小/大一大截

**原因**：两个基准类都带 `[ShortRunJob]`（`BusDispatchBenchmarks.cs:14`、`SerializationBenchmarks.cs:11`），默认迭代少、噪声大；用 `--job medium` 改变迭代规模后数字必然不同。
**解决**：对比时锁定同一个 job；门禁场景显式传 `--job medium` 或 `--job long`，并把 job 写进基线记录。

### `--filter` 匹配不到任何基准

**原因**：过滤的是完整名称，`*BusDispatchBenchmarks*` 能匹配；写成 `BusDispatch` 或方法名片段可能匹配不到。
**解决**：先用 `--job Dry` 加宽通配（如 `--filter '*Bus*'`）确认能选中，再收窄。

### 序列化基准的结果为 0 或抛空引用

**原因**：`SerializationBenchmarks` 依赖 `[GlobalSetup]` 里创建的序列化器与预生成载荷（`SerializationBenchmarks.cs:30-38`），跳过 Setup（例如手工改 Main）会拿到 `null`。
**解决**：通过 `BenchmarkSwitcher` 运行，不要直接调基准方法；改了序列化器构造参数要同步改 Setup。

## 最佳实践

- **结论只用于对比**：`NoopTransporter` 把传输成本归零，得到的是“总线框架开销”，不要当端到端吞吐。
- **保留 `DirectTransportPublish` 与 `ResolvePipeline` 两个探针**：分别定位“传输调用”和“DI 装配”的回归。
- **升级总线后先跑 Dry**：`--job Dry` 能快速暴露 `IConfigurator` / `ITransporter` 签名变化。
- **序列化基准只比同格式**：System.Text.Json 与 Newtonsoft.Json 的 `Ratio` 以 System.Text.Json 为基线，跨格式看绝对值。
- **告警阈值按比值设**：例如 `PublishAsync` 相对 `DirectTransportPublish` 的差值变化超过 20% 才提示。

## 版本与兼容

- **目标框架**：`net10.0`（`Euonia.Bus.Benchmarks.csproj:5`），不打包（`IsPackable=false`）。
- **关键依赖**：BenchmarkDotNet `0.15.8`（`Directory.Packages.props:141`）；被测包 `Euonia.Bus` / `Euonia.Bus.Abstract` / `Euonia.Modularity`。
- **升级注意**：基准依赖 keyed DI（`AddKeyedSingleton`）与 `MessageBusOptions.DefaultTransporter`，总线重命名传输器或改配置键时基准会先失败。
- **已知边界**：不加载任何真实传输；RabbitMQ / InMemory 的网络与确认开销不在覆盖范围内。

## 相关包

- [`Euonia.Bus`](../../Source/Euonia.Bus/README.md) —— 总线抽象、`IConfigurator`、序列化器
- [`Euonia.Bus.Abstract`](../../Source/Euonia.Bus.Abstract/README.md) —— 消息与信封契约
- [`Euonia.Modularity`](../../Source/Euonia.Modularity/README.md) —— 基准中的服务装配依赖
- [`Euonia.Bus.InMemory`](../../Source/Euonia.Bus.InMemory/README.md) —— 真实传输实现（非基准使用）
- [`Euonia.Osba.Benchmarks`](../Euonia.Osba.Benchmarks/README.md) —— 业务对象基准
- [`Euonia.Platform.Benchmarks`](../Euonia.Platform.Benchmarks/README.md) —— 平台能力基准

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
