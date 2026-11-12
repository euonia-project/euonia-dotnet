# Euonia.Bus.Benchmarks 用户手册

用空传输器把总线框架开销与真实传输开销分离，从而定位 `PublishAsync` / `SendAsync` / `CallAsync` 的回归来源。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Bus.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`；被测 `Euonia.Bus` / `Euonia.Bus.Abstract` / `Euonia.Modularity` |
| 完整参考 | [`Benchmarks/Euonia.Bus.Benchmarks/README.md`](../../Benchmarks/Euonia.Bus.Benchmarks/README.md) |

## 教程

### 场景

一次总线改动后 `PublishAsync` 的 Mean 从 1.2 μs 涨到 2.0 μs：判断是“总线框架”变慢，还是“传输调用”变慢，并给出可复现的对比数据。

### 前置条件

- .NET SDK 支持 `net10.0`；仓库根目录执行命令。
- 独占机器（关闭其他构建/容器），否则 `Ratio` 不可信。
- 不需要 RabbitMQ / Redis：基准用进程内 `NoopTransporter`。

### 1. 冒烟：确认基准能跑

```bash
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*BusDispatchBenchmarks*' --job Dry
```

Dry 只验证 `[GlobalSetup]` 不抛异常（DI 装配、`SetConvention`、keyed 传输器注册），几十秒内结束。

### 2. 采集基线（改动前）

```bash
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*BusDispatchBenchmarks*' --exporters json --artifacts ./artifacts/before
```

关键方法：`PublishAsync`、`SendAsync`、`CallAsync`、`DirectTransportPublish`、`ResolvePipeline`、`CreateEnvelope`、`ParallelOne`。

### 3. 采集对照（改动后）

```bash
dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks/Euonia.Bus.Benchmarks.csproj \
  -- --filter '*BusDispatchBenchmarks*' --exporters json --artifacts ./artifacts/after
```

### 4. 解读：先看差值，再看单点

- `PublishAsync` 涨、`DirectTransportPublish` 不涨 → 回归在 pipeline/路由/信封处理。
- 两者同比例上涨 → 更像环境漂移（CPU 降频、GC 模式），先重跑基线。
- `ResolvePipeline` 单独上涨 → 每次分发的 DI 解析变贵（生命周期从单例改成作用域会立刻体现）。
- `CreateEnvelope` 上涨 → 信封构造里新增了默认字段或 GUID 生成。

用 `jq` 把两次结果对齐成一张表：

```bash
for dir in before after; do
  echo "== $dir =="
  jq -r '.Benchmarks[] | [.Method, (.Statistics.Mean | floor), (.Memory.BytesAllocatedPerOperation // 0)] | @tsv' \
    artifacts/$dir/results/*-report-full-compressed.json
done
```

输出三列：方法名、平均耗时（ns）、每操作分配字节。

### 5. 报告列速查

| 列 | 含义 | 判读方式 |
|---|---|---|
| `Mean` | 平均单次耗时 | 与基线同 job 对比，超过 20% 视为回归 |
| `Error` / `StdDev` | 置信区间与标准差 | 抖动接近 Mean 时先排除环境问题 |
| `Ratio` | 相对 `[Benchmark(Baseline = true)]` 的倍数 | 仅同组内有效（序列化组基线是 STJ） |
| `Allocated` | 每操作分配字节 | 分配翻倍通常比耗时回归更值得先修 |
| `Gen0` | 每 1000 次操作的 Gen0 回收次数 | 上升说明短生命周期对象变多 |

`--job Dry` 与 `--job medium` 的数字不可混用：先固定 job，再比较两个目录的 JSON。

### 常见失败点

- **`Setup` 抛 `MessageTransportException`**：默认传输器名与 keyed 注册必须一致，基准里是 `"bench"`（`BusDispatchBenchmarks.cs:35,38`）。
- **两次数字差一倍但代码没动**：多半是没独占机器，或一次用了 `--job Dry`、一次用了正式 job。
- **`--filter` 没匹配**：过滤的是完整名称，先加宽到 `'*Bus*'` 确认命中。
- **序列化基准空引用**：跳过 `[GlobalSetup]`（直接调基准方法）会拿到未初始化的序列化器（`SerializationBenchmarks.cs:30-38`）。

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

- [`Benchmarks/Euonia.Bus.Benchmarks` README](../../Benchmarks/Euonia.Bus.Benchmarks/README.md) —— 环境要求与方法清单
- [`Source/Euonia.Bus/README.md`](../../Source/Euonia.Bus/README.md) —— `IConfigurator` / 传输器 / 序列化器
- [Osba 基准手册](Euonia.Osba.Benchmarks.md) —— 另一套门禁指标
- [Platform 基准手册](Euonia.Platform.Benchmarks.md) —— 租户与遥测热路径
- [根 README](../../README.md)
