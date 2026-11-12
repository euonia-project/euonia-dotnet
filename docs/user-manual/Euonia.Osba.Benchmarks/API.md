# Euonia.Osba.Benchmarks API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

业务对象生命周期、规则求值、保存路径与领域控制的性能门禁：6 个方法、20% 回归阈值、分配翻倍判据。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Osba.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`、`Microsoft.Extensions.DependencyInjection`；被测 `Euonia.Osba` |
| 完整参考 | [`Benchmarks/Euonia.Osba.Benchmarks/README.md`](../../../Benchmarks/Euonia.Osba.Benchmarks/README.md) |

## API 参考

### 基准类与方法

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `ObjectLifecycleBenchmarks.Create()` | 对象创建基线 | `[Benchmark(Baseline = true)]` |
| `ObjectLifecycleBenchmarks.CreateAndFirstRead()` | 创建后首次读属性 | 含惰性初始化 |
| `ObjectLifecycleBenchmarks.ReadExisting()` | 稳态读取 | — |
| `ObjectLifecycleBenchmarks.WriteExisting()` | 写入不同值（门禁） | 交替写 `a`/`b` |
| `ObjectLifecycleBenchmarks.WriteSameValue()` | 写入相同值 | 验证相等性短路 |
| `RuleAndFactoryBenchmarks.WritePlain()` | 无规则写入基线 | 该组 `Baseline` |
| `RuleAndFactoryBenchmarks.WriteWithPropertyRule()` | 带必填规则写入（门禁） | 规则求值开销 |
| `RuleAndFactoryBenchmarks.ValidatePlain()` | 完整校验 | 返回 `bool` |
| `RuleAndFactoryBenchmarks.SavePlain()` | 标记变更后保存（门禁） | 走更新钩子 |
| `RuleAndFactoryBenchmarks.FetchFactory()` | `IObjectFactory.FetchAsync`（门禁） | 工厂拉取 |
| `RuleAndFactoryBenchmarks.FetchActuator()` | `IActuator.For<T>().Fetch()`（门禁） | 执行器拉取 |
| `DomainControlBenchmarks.Write()` | 属性赋值基线 | 该组 `Baseline` |
| `DomainControlBenchmarks.GetOriginalValue()` | 读原始值 | 按键查 `PropertyInfo` |
| `DomainControlBenchmarks.RejectChanges()` | 改后回滚（门禁） | 写入 + 回滚整体 |
| `DomainControlBenchmarks.MarkAsChangedThenAccept()` | 状态迁移 | — |

### 命令行参数

| 参数 | 说明 | 示例 |
|---|---|---|
| `--filter` | 可传多个通配 | `--filter '*DomainControl*'` |
| `--job` | `short` 仅冒烟 | `--job short` |
| `--exporters` | 生成 JSON 供脚本比较 | `--exporters json` |
| `--artifacts` | 结果目录 | `--artifacts ./artifacts/current` |

### 配置项

本项目**没有** `appsettings` 配置；阈值（20% / 分配翻倍）来自架构评审 `4.3，需在 CI 脚本中实现，代码里没有对应常量。

## 相关文档

- [教程](TUTORIAL.md)
- [`Benchmarks/Euonia.Osba.Benchmarks` README](../../../Benchmarks/Euonia.Osba.Benchmarks/README.md) —— 门禁清单与运行方式
- [`Source/Euonia.Osba/ARCHITECTURE-REVIEW.md`](../../../Source/Euonia.Osba/ARCHITECTURE-REVIEW.md) —— `4.3 性能目标
- [`Source/Euonia.Osba/README.md`](../../../Source/Euonia.Osba/README.md) —— 业务对象与规则
- [Bus 基准手册](../Euonia.Bus.Benchmarks/API.md) / [Platform 基准手册](../Euonia.Platform.Benchmarks/API.md) —— 其他基准入口
- [根 README](../../../README.md)
