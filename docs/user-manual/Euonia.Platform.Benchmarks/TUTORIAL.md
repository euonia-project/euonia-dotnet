# Euonia.Platform.Benchmarks 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

平台热路径基准：租户写入守卫、租户键转换、执行上下文安装与遥测空操作开销，用来守住 Tenancy / Telemetry / Security 的每请求成本。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Platform.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`；被测 `Euonia.Tenancy` / `Euonia.Telemetry` / `Euonia.Security` |
| 完整参考 | [`Benchmarks/Euonia.Platform.Benchmarks/README.md`](../../../Benchmarks/Euonia.Platform.Benchmarks/README.md) |

## 教程

### 场景

改动 `TenantKeyTransformer`（例如调整缓存键前缀逻辑）后，验证三点：键转换没有变慢、租户写入守卫仍是纳秒级、没有引入额外分配。

### 前置条件

- .NET SDK 支持 `net10.0`；仓库根目录执行。
- 无需外部服务：不连数据库、Redis、ZooKeeper 或 OTLP。
- 独占机器；同一轮对比必须同 job。

### 1. 冒烟

```bash
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantKeyTransformer*' --job Dry
```

该基准在 `[GlobalSetup]` 用 `new TenantScopeFactory().Begin(...)` 安装租户作用域（`TenantKeyTransformerBenchmarks.cs:18-22`），Dry 能跑通说明作用域链路正常。

### 2. 采集基线

```bash
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantKeyTransformer*' '*TenantEntryGuard*' '*TelemetryBenchmarks*' \
  --exporters json --artifacts ./artifacts/platform-before
```

### 3. 改动后采集

```bash
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantKeyTransformer*' '*TenantEntryGuard*' '*TelemetryBenchmarks*' \
  --exporters json --artifacts ./artifacts/platform-after
```

### 4. 对比两次结果

```bash
for dir in platform-before platform-after; do
  echo "== $dir =="
  jq -r '.Benchmarks[] | [.Method, (.Statistics.Mean | floor), (.Memory.BytesAllocatedPerOperation // 0)] | @tsv' \
    artifacts/$dir/results/*-report-full-compressed.json
done
```

### 5. 判读

- `Transform` 只测“有租户前缀”的路径：没有租户上下文时它会原样返回，数字完全不同。
- `Validate` 走 `TenantEntryOperation.Added` + 活跃租户，正常应在纳秒级且零分配；出现分配说明守卫里新增了字符串拼接或装箱。
- `StartActivity` 分配为 `0` 是预期：无监听者时空句柄直接返回；它衡量的是“关掉遥测时的成本”，不是开启后的成本。
- `PushPop` 衡量 `AmbientExecutionContext.Push` 的安装/恢复成本，多租户与消息管道都依赖它。

### 6. 组合回归：动了 `AmbientExecutionContext` 之后

执行上下文是租户、遥测、消息三条路径的公共底座，改它要一起看：

| 基准 | 先看什么 | 含义 |
|---|---|---|
| `AmbientExecutionContextBenchmarks.PushPop` | `Mean` | 每请求/每消息的安装成本 |
| `TenantKeyTransformerBenchmarks.Transform` | `Mean` + 分配 | 缓存键前缀生成 |
| `TenantEntryGuardBenchmarks.Validate` | 分配 | 写入守卫是否引入装箱 |
| `TelemetryBenchmarks.StartActivity` | 分配 | 关闭遥测时的底线成本 |

改动后至少跑这四组；只跑其中一组容易漏掉跨组回归。

### 常见失败点

- **`--filter` 没命中**：名称需通配；先 `--filter '*Tenant*'` 确认能选中。
- **不加 `--filter` 时命令挂起**：`BenchmarkSwitcher` 会进入交互式选择（`Program.cs:16`），CI 必须显式传过滤。
- **`Transform` 数字异常低**：租户作用域没装上，基准退化为“无租户直接返回”。
- **两次结果差很多但代码没改**：检查是否一次加了 `--job Dry`，或机器负载不同。
- **误把 `StartActivity` 的 0 分配当结论**：接上 OTel/监听器后分配会上升，需另建带监听器的基准。

## 相关文档

- [API 参考](API.md)
- [`Benchmarks/Euonia.Platform.Benchmarks` README](../../../Benchmarks/Euonia.Platform.Benchmarks/README.md) —— 方法与限制
- [Tenancy 手册](../Euonia.Tenancy/TUTORIAL.md) —— `TenantEntryGuard` / `TenantKeyTransformer` 的用法
- [Telemetry 手册](../Euonia.Telemetry/TUTORIAL.md) —— `TelemetrySource` 与监听者语义
- [`Source/Euonia.Security/README.md`](../../../Source/Euonia.Security/README.md) —— `SecurityTelemetry`
- [Bus 基准手册](../Euonia.Bus.Benchmarks/TUTORIAL.md) / [Osba 基准手册](../Euonia.Osba.Benchmarks/TUTORIAL.md)
- [根 README](../../../README.md)
