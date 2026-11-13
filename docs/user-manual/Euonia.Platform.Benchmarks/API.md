# Euonia.Platform.Benchmarks API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

平台热路径基准：租户写入守卫、租户键转换、执行上下文安装与遥测空操作开销，用来守住 Tenancy / Telemetry / Security 的每请求成本。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Platform.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`；被测 `Euonia.Tenancy` / `Euonia.Telemetry` / `Euonia.Security` |
| 完整参考 | [`Benchmarks/Euonia.Platform.Benchmarks/README.md`](../../../Benchmarks/Euonia.Platform.Benchmarks/README.md) |

## API 参考

### 基准类与方法

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `AmbientExecutionContextBenchmarks.PushPop()` | 安装并释放上下文作用域 | `AmbientExecutionContext.Push(values => values.TenantId = "t1")` |
| `TenantEntryGuardBenchmarks.Validate()` | 新增实体的租户校验 | 固定 `Added` + `t1` + `Active` |
| `TenantKeyTransformerBenchmarks.Transform()` | 生成 `t:{tenantId}:{key}` 前缀键 | `[GlobalSetup]` 装租户作用域，`[GlobalCleanup]` 释放 |
| `SecurityTelemetryBenchmarks.Authorize()` | 授权活动 Start/Complete | 无监听者空操作 |
| `TelemetryBenchmarks.StartActivity()` | `TelemetrySource.StartActivity` | 源名 `Nerosoft.Euonia.Benchmarks` |

### 命令行参数

| 参数 | 说明 | 示例 |
|---|---|---|
| `--filter` | 按完整名称通配 | `--filter '*TenantEntryGuard*'` |
| `--job` | `Dry` 冒烟、`short`/`medium` 正式 | `--job Dry` |
| `--exporters` | 落盘 JSON 用于对比 | `--exporters json` |
| `--artifacts` | 结果目录 | `--artifacts ./artifacts/platform-after` |

### 配置项

本项目**没有** `appsettings` 配置，也不读取 `Euonia:*` 节；行为只由命令行参数与基准类特性决定。

## 相关文档

- [教程](TUTORIAL.md)
- [`Benchmarks/Euonia.Platform.Benchmarks` README](../../../Benchmarks/Euonia.Platform.Benchmarks/README.md) —— 方法与限制
- [Tenancy 手册](../Euonia.Tenancy/API.md) —— `TenantEntryGuard` / `TenantKeyTransformer` 的用法
- [Telemetry 手册](../Euonia.Telemetry/API.md) —— `TelemetrySource` 与监听者语义
- [`Source/Euonia.Security/README.md`](../../../Source/Euonia.Security/README.md) —— `SecurityTelemetry`
- [Bus 基准手册](../Euonia.Bus.Benchmarks/API.md) / [Osba 基准手册](../Euonia.Osba.Benchmarks/API.md)
- [根 README](../../../README.md)
