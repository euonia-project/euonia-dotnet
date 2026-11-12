# Euonia.Osba.Benchmarks

`Euonia.Osba` 的对象生命周期、规则、保存、领域控制与读取执行器基线，用于守住 4.3 节的性能预算。

- **目标框架**：`net10.0`
- **入口**：`Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)`
- **运行方式**：`dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks/Euonia.Osba.Benchmarks.csproj`
- **NuGet 包**：无（`IsPackable=false`）

## 前置条件

- .NET SDK（支持 `net10.0`）与 BenchmarkDotNet（随项目引用还原）。
- 不需要外部服务：基准只用内存对象与最小 DI 容器。
- 正式门禁请用 `Release`、独占机器，并保留 `MemoryDiagnoser` 的分配列。

## 基准类

| 基准类 | 方法（`Baseline` 标注者为对比基线） | 测量内容 |
|--------|------------------------------|----------|
| `ObjectLifecycleBenchmarks` | `Create`（Baseline）、`CreateAndFirstRead`、`ReadExisting`、`WriteExisting`、`WriteSameValue` | 对象创建、首次读写与稳态属性访问 |
| `RuleAndFactoryBenchmarks` | `WritePlain`（Baseline）、`WriteWithPropertyRule`、`ValidatePlain`、`SavePlain`、`FetchFactory`、`FetchActuator` | 属性规则求值、完整校验、工厂保存、工厂与执行器的拉取路径 |
| `DomainControlBenchmarks` | `Write`（Baseline）、`GetOriginalValue`、`RejectChanges`、`MarkAsChangedThenAccept` | 原始值读取、回滚与变更状态迁移 |

各基准类在 `[GlobalSetup]` 装配最小 DI，`[GlobalCleanup]` 释放；`RuleAndFactoryBenchmarks` 的 `FetchFactory` / `FetchActuator` 衡量的是工厂与执行器的解析成本。

## 运行

```bash
# 全部
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks

# 只跑某一组
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*ObjectLifecycle*'
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*RuleAndFactory*'
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*DomainControl*'

# 快速冒烟（ShortRun，不作为正式门禁）
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*DomainControl*' --job short
```

以上命令以仓库根目录为工作目录；也可把 `--project` 换成显式的 `Benchmarks/Euonia.Osba.Benchmarks/Euonia.Osba.Benchmarks.csproj`。

## CI 门禁

建议在 CI 中把以下指标作为门禁：

- `ObjectLifecycleBenchmarks.WriteExisting`
- `RuleAndFactoryBenchmarks.WriteWithPropertyRule`
- `RuleAndFactoryBenchmarks.SavePlain`
- `RuleAndFactoryBenchmarks.FetchFactory`
- `RuleAndFactoryBenchmarks.FetchActuator`
- `DomainControlBenchmarks.RejectChanges`

回归超过 20% 或分配翻倍即失败。具体预算见
[`Source/Euonia.Osba/ARCHITECTURE-REVIEW.md`](../../Source/Euonia.Osba/ARCHITECTURE-REVIEW.md) §4.3。

## 注意事项 / 限制

- 门禁阈值（20% 回归 / 分配翻倍）来自架构评审 §4.3，不是代码常量；调整前先改文档再改 CI。
- `--job short` 只做快速冒烟，其数字不能用于门禁判定；正式采集使用默认 job 或更长的 job。
- 基准跑在内存对象与最小 DI 上，不覆盖数据库往返、事务提交与序列化；生产环境的热点可能不在这里。
- 基准类共享 `BenchmarkDotNet.Artifacts/` 输出目录，多次并行运行会互相覆盖报告。
- 同一机器前后两次 `Release` 结果才可比；跨机器对比需固定 CPU 与 GC 配置。
- `RuleAndFactoryBenchmarks` 的工厂 / 执行器缓存若被改动，`FetchFactory`、`FetchActuator` 会先于业务基准报警。

## 相关包

- [`Euonia.Osba`](../../Source/Euonia.Osba/README.md) —— 被测业务对象框架
- [`Euonia.Domain`](../../Source/Euonia.Domain/README.md) —— 领域模型与领域事件
- [`Euonia.Pipeline`](../../Source/Euonia.Pipeline/README.md) —— 基准中的管道装配
- [`Euonia.Bus.Benchmarks`](../Euonia.Bus.Benchmarks/README.md) —— 总线分发与序列化基准
- [`Euonia.Platform.Benchmarks`](../Euonia.Platform.Benchmarks/README.md) —— 平台能力基准

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
