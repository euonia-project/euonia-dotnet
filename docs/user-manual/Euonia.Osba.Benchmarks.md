# Euonia.Osba.Benchmarks 用户手册

业务对象生命周期、规则求值、保存路径与领域控制的性能门禁：6 个方法、20% 回归阈值、分配翻倍判据。

| 项 | 值 |
|---|---|
| 项目 | `Benchmarks/Euonia.Osba.Benchmarks`（`IsPackable=false`） |
| 目标框架 | `net10.0` |
| 入口 | `Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)` |
| 依赖 | BenchmarkDotNet `0.15.8`、`Microsoft.Extensions.DependencyInjection`；被测 `Euonia.Osba` |
| 完整参考 | [`Benchmarks/Euonia.Osba.Benchmarks/README.md`](../../Benchmarks/Euonia.Osba.Benchmarks/README.md) |

## 教程

### 场景

把 6 个门禁方法接进 CI：先采集并提交基线，再在改动后自动比较 `Mean` 与分配量，超过 20% 或分配翻倍即失败。

### 前置条件

- .NET SDK 支持 `net10.0`，仓库根目录执行。
- 固定 runner（同一机型、同一 SDK）；基线必须来自同一环境。
- 不需要数据库：基准用内存对象与最小 DI 容器。

### 1. 冒烟：确认装配没坏

```bash
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks/Euonia.Osba.Benchmarks.csproj \
  -- --filter '*RuleAndFactoryBenchmarks*' --job short
```

`RuleAndFactoryBenchmarks` 在 `[GlobalSetup]` 里建 DI、调用 `BusinessContextAccessor.SetCurrent(...)`（`RuleAndFactoryBenchmarks.cs:28-35`）；装配坏掉时这一步就会失败。

### 2. 采集基线并提交

```bash
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks/Euonia.Osba.Benchmarks.csproj \
  -- --filter '*ObjectLifecycleBenchmarks*' '*RuleAndFactoryBenchmarks*' '*DomainControlBenchmarks*' \
  --exporters json --artifacts ./artifacts/baseline
```

### 3. 改动后采集

与基线使用同一组过滤，只改输出目录：

```bash
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks/Euonia.Osba.Benchmarks.csproj \
  -- --filter '*ObjectLifecycleBenchmarks*' '*RuleAndFactoryBenchmarks*' '*DomainControlBenchmarks*' \
  --exporters json --artifacts ./artifacts/current
```

### 4. 门禁脚本（阈值 20% / 分配 2 倍）

```bash
#!/usr/bin/env bash
set -euo pipefail

base=$(ls artifacts/baseline/results/*-report-full-compressed.json)
curr=$(ls artifacts/current/results/*-report-full-compressed.json)
fail=0

pick() { jq -r --arg m "$2" '.Benchmarks[] | select(.Method == $m) |
          "(.Statistics.Mean) (.Memory.BytesAllocatedPerOperation // 0)"' "$1"; }

for method in WriteExisting WriteWithPropertyRule SavePlain FetchFactory FetchActuator RejectChanges; do
  read -r bMean bAlloc < <(pick "$base" "$method")
  read -r cMean cAlloc < <(pick "$curr" "$method")

  awk -v b="$bMean" -v c="$cMean" -v m="$method" 'BEGIN {
    if (b <= 0) next;
    r = c / b;
    if (r > 1.2) { printf "REGRESSION %-24s mean x%.2f\n", m, r; exit 1 }
  }' || fail=1

  awk -v b="$bAlloc" -v c="$cAlloc" -v m="$method" 'BEGIN {
    if (b <= 0) next;
    r = c / b;
    if (r > 2.0) { printf "ALLOCATION  %-24s alloc x%.2f\n", m, r; exit 1 }
  }' || fail=1
done

exit $fail
```

`set -e` 与 `awk` 非零退出配合：任一方法越界即以非零码结束，CI 直接红。

### 5. 读结果

- 6 个方法中任意一个 `Mean` 比值 > 1.2 或分配比值 > 2.0 判失败。
- `FetchFactory` / `FetchActuator` 变慢通常指向工厂/执行器注册或缓存改动，而不是业务写入逻辑。
- `WriteSameValue`、`ValidatePlain`、`GetOriginalValue` 等非门禁方法只做参考，不参与判定。

### 常见失败点

- **`jq` 取不到值**：方法名写错或该基准本次没被过滤到；先用 `jq -r '.Benchmarks[].Method'` 列一遍。
- **门禁忽红忽绿**：基线来自另一台机器或 `--job short`；统一 job 与机器后重采。
- **`BusinessContextAccessor` 未清理导致后续基准异常**：不要手工调基准方法，始终走 `BenchmarkSwitcher`，保证 `[GlobalCleanup]` 执行。
- **报告目录串了**：每次运行前清空 `artifacts/current`，否则 `ls` 会匹配到多份 JSON。

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

- [`Benchmarks/Euonia.Osba.Benchmarks` README](../../Benchmarks/Euonia.Osba.Benchmarks/README.md) —— 门禁清单与运行方式
- [`Source/Euonia.Osba/ARCHITECTURE-REVIEW.md`](../../Source/Euonia.Osba/ARCHITECTURE-REVIEW.md) —— `4.3 性能目标
- [`Source/Euonia.Osba/README.md`](../../Source/Euonia.Osba/README.md) —— 业务对象与规则
- [Bus 基准手册](Euonia.Bus.Benchmarks.md) / [Platform 基准手册](Euonia.Platform.Benchmarks.md) —— 其他基准入口
- [根 README](../../README.md)
