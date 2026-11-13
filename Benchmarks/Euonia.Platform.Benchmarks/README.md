# Euonia.Platform.Benchmarks

`Euonia.Tenancy`、`Euonia.Telemetry`、`Euonia.Security` 与执行上下文热路径的基准基线：租户写入守卫、租户键转换、遥测空操作开销。

- **目标框架**：`net10.0`
- **入口**：`Program.Main` → `BenchmarkSwitcher.FromAssembly(...).Run(args)`
- **运行方式**：`dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj`
- **NuGet 包**：无（`IsPackable=false`）

## 前置条件

- .NET SDK（支持 `net10.0`）与 BenchmarkDotNet（随项目引用还原）。
- 不需要任何外部服务：基准全部在进程内运行，不连接数据库、Redis、ZooKeeper 或 OTLP 端点。
- 正式测量请用 `Release` 且独占机器；`Debug` 数字无参考价值。

## 基准类

| 基准类 | 方法 | 测量内容 |
|--------|------|----------|
| `AmbientExecutionContextBenchmarks` | `PushPop` | `AmbientExecutionContext.Push` 安装并释放上下文作用域的每消息开销 |
| `TenantEntryGuardBenchmarks` | `Validate` | `TenantEntryGuard.Validate` 对新增实体的租户校验（`Added` + 活跃租户） |
| `TenantKeyTransformerBenchmarks` | `Transform` | `TenantKeyTransformer.Transform` 在已安装租户作用域下生成 `t:{tenantId}:{key}` 前缀键 |
| `SecurityTelemetryBenchmarks` | `Authorize` | `SecurityTelemetry.Start` + `Complete` 在无监听者时的空操作开销 |
| `TelemetryBenchmarks` | `StartActivity` | `TelemetrySource.StartActivity` 在无监听者时的空操作开销 |

所有基准类标注 `[MemoryDiagnoser]`，输出包含分配量与 Gen0 回收次数。`TenantKeyTransformerBenchmarks` 在 `[GlobalSetup]` 中通过 `new TenantScopeFactory().Begin(...)` 安装租户作用域，因此测的是**命中租户前缀**的路径。

## 运行

```bash
# 全部基准（仓库根目录执行）
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj

# 只跑某一组
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantEntryGuard*'

dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TelemetryBenchmarks*'

# 快速冒烟（不作正式结果）
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantKeyTransformer*' --job Dry
```

`--filter` 使用 BenchmarkDotNet 的通配语法，匹配的是 `命名空间.类名.方法名`；不带 `--filter` 时交互式选择或全量运行。

## 使用示例

把平台基准与业务基准一起做回归对比：

```bash
# 先取基线
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*' --exporters json

# 改动后再跑一次，对比 Baseline 列的 Ratio 与 Allocated
dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks/Euonia.Platform.Benchmarks.csproj \
  -- --filter '*TenantEntryGuard*' --exporters json
```

结果默认写入 `BenchmarkDotNet.Artifacts/`（在运行目录下），包含 Markdown 报告与 JSON 数据。

## 注意事项 / 限制

- `TelemetryBenchmarks` / `SecurityTelemetryBenchmarks` 测的是**无监听者**路径：接入 `Euonia.Telemetry.Otel` 或自建监听器后的数字不适用。
- `TenantEntryGuardBenchmarks` 固定使用 `Added` 且作用域租户为 `t1` 的上下文，不覆盖跨租户、停用租户与修改/删除分支。
- `TenantKeyTransformerBenchmarks` 的作用域在 `[GlobalSetup]` 安装、`[GlobalCleanup]` 释放；若改动 `AmbientExecutionContext` 的安装语义，需要同步检查这两个钩子。
- 基准跑在进程内，不覆盖网络、序列化边界或真实存储；与 `Euonia.Bus.Benchmarks` / `Euonia.Osba.Benchmarks` 的结论不能相互替代。
- 不要在共享 CI 容器中采集正式基线：CPU 争用与 GC 模式会让 `Ratio` 失真。`--job Dry` / `--job short` 只用于验证基准能跑通。
- 跨机器、跨 SDK 版本的数字不可直接比较；回归判断应使用同一机器上的前后两次 `Release` 结果。

## 常见问题与故障排查

### 命令跑完什么都没输出就退出

**原因**：`BenchmarkSwitcher` 按 `--filter` 的**完整名称**（`命名空间.类名.方法名`）匹配，写错类名时没有任何基准被选中（`Program.cs:16`）。
**解决**：用通配包住类名，例如 `-- --filter '*TenantEntryGuard*'`；先跑 `--job Dry` 确认能匹配到，再采集正式数据。

### 不加 `--filter` 时 CI 卡住不动

**原因**：`BenchmarkSwitcher.Run(args)` 在无参数、无过滤器时进入交互式选择列表，等待标准输入。
**解决**：CI 中始终显式传 `--filter`；本地想全量跑用 `--filter '*'`。

### 报告里出现 `Dry` / 数字明显偏小

**原因**：`--job Dry` 只做一次预热与少量迭代（`--job short` 同理），用于验证基准能跑通，不代表真实开销。
**解决**：正式数据去掉 `--job`；跨次对比必须保持 job、机器与 SDK 版本一致。

### `TelemetryBenchmarks` 的分配量全是 0

**原因**：基准没有挂任何监听器，`StartActivity` 直接返回空句柄（`TelemetrySource.IsTraceEnabled` 依赖 `HasListeners()`），分配为 0 是预期结果。
**解决**：不要把它当“遥测零成本”的绝对结论；需要测有监听器路径时另建基准并显式 `AddSource` 一个空的 `ActivityListener`。

### `TenantKeyTransformerBenchmarks` 的结果明显低于预期或跨机器差异巨大

**原因**：该基准在 `[GlobalSetup]` 用 `new TenantScopeFactory().Begin(...)` 安装租户作用域，测的是**命中前缀**的路径；没有租户上下文时 `Transform` 直接返回原键，开销完全不同。
**解决**：确认 `GlobalSetup` / `GlobalCleanup` 未被裁剪；比较时确保两边都在有租户作用域的前提下运行。

## 最佳实践

- **固定 job 与机器**：门禁只用同一台机器、同一个 `--job`，并在报告里记下 `net10.0` SDK 版本。
- **结果落盘对比**：`--exporters json` 输出到 `BenchmarkDotNet.Artifacts/`，用 `Ratio` 与 `Allocated` 两列做回归判断。
- **先冒烟再采集**：每次改基准代码先 `--job Dry`，确认 `[GlobalSetup]` 不抛异常。
- **只钉关键方法**：CI 只跑 `TenantEntryGuard`、`TenantKeyTransformer`、`Telemetry` 这几条热路径，全量留给本地。
- **平台基准与业务基准一起看**：改动 `AmbientExecutionContext` 会同时影响 Tenancy、Telemetry 与 Bus，回归时至少各跑一组。

## 版本与兼容

- **目标框架**：`net10.0`（`Euonia.Platform.Benchmarks.csproj:5`），不打包（`IsPackable=false`）。
- **关键依赖**：BenchmarkDotNet `0.15.8`（`Directory.Packages.props:141`）；被测包 `Euonia.Tenancy` / `Euonia.Telemetry` / `Euonia.Security` 支持 `net10.0` 与 `net11.0`。
- **升级注意**：升级 BenchmarkDotNet 会改变默认 job 与报告列，历史基线需重新采集。
- **已知边界**：基准全在进程内，不覆盖网络、存储与序列化；结论不能外推到 Bus / Osba 场景。

## 相关包

- [`Euonia.Tenancy`](../../Source/Euonia.Tenancy/README.md) —— `TenantEntryGuard`、`TenantKeyTransformer`
- [`Euonia.Telemetry`](../../Source/Euonia.Telemetry/README.md) —— `TelemetrySource`
- [`Euonia.Security`](../../Source/Euonia.Security/README.md) —— `SecurityTelemetry`
- [`Euonia.Core`](../../Source/Euonia.Core/README.md) —— `AmbientExecutionContext`
- [`Euonia.Bus.Benchmarks`](../Euonia.Bus.Benchmarks/README.md) —— 总线分发与序列化基准
- [`Euonia.Osba.Benchmarks`](../Euonia.Osba.Benchmarks/README.md) —— 业务对象生命周期基准

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
