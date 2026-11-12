[TEST_PASS]

# TEST_REPORT — Tenancy / Auditing / Telemetry 四缺陷修复验证

## 结论

**通过。** Coder 完成复审修复（动作快照 / CompleteAsync 独立 try、OCE 回退、Telemetry remarks、路由日志聚合）后，P1～P4 的行为与 PLAN 一致。新增 8 个用例（5 个回归检测 + 3 个守卫），并给既有正向 200 用例补充 1 个断言；3 个测试项目与全量 solution 均为 0 失败，既有测试无回退。

Tester 未修改任何业务源码、`PLAN.md`、配置；仅修改 4 个指定测试文件并新增本报告。

## 变更范围

- 4 个测试文件（分属 3 个测试项目）：
  - `Tests/Euonia.Core.Tests/Tenancy/TenancyCoreTests.cs`（P1）
  - `Tests/Euonia.Hosting.Tests/TenancyAuditingPipelineTests.cs`（P2）
  - `Tests/Euonia.Application.Tests/Auditing/AuditingCoreTests.cs`（P3）
  - `Tests/Euonia.Core.Tests/Telemetry/TelemetryCoreTests.cs`（P4）
- 新增本报告 `TEST_REPORT.md`。
- 测试文件 diff 均为新增行（`git diff --numstat` 删除行 = 0），未修改既有断言；仅对既有正向 200 用例追加了一条断言。

## 新增 / 修改用例

### 新增 8 个（5 回归检测 + 3 守卫）

| 优先级 | 文件 | 用例名 | 类别 | 验证点 |
| --- | --- | --- | --- | --- |
| P1 | `TenancyCoreTests.cs` | `TenantScopeFactory_BeginWithActorAndTarget_ShouldRejectUnauthorizedCrossTenant` | 回归检测 | 未配置 `CrossTenantScopeAuthorizer` 时四参数 `Begin(tenant, "a", "b", "support")` 抛 `UnauthorizedAccessException`；修复前不抛，用例失败 |
| P1 | `TenancyCoreTests.cs` | `TenantScopeFactory_BeginWithActorAndTarget_ShouldAuthorizeCrossTenant` | 守卫 | 配置授权回调后成功；Ambient 与 Scope.Context 的 ActorTenantId="a"、TargetTenantId="b"、IsCrossTenant=true、CrossTenantReason="support" |
| P1 | `TenancyCoreTests.cs` | `TenantScopeFactory_BeginWithSameActorAndTarget_ShouldNotRequireAuthorization` | 守卫 | actor==target（"a","a"）无需授权；IsCrossTenant=false 且 CrossTenantReason 为 null，防止过度收紧同租户路径 |
| P2 | `TenancyAuditingPipelineTests.cs` | `AuditScope_AbortInEndpoint_ShouldRemainFailureOnHttp200` | 回归检测 | 端点内 `CurrentScope.AbortAsync(...)` 后返回 200；使用 gated 存储，在响应结束、中间件回填完成后才快照，断言 IsSuccess=false 且 HttpStatusCode=200；修复前会被无条件覆盖为 true，用例失败 |
| P2 | `TenancyAuditingPipelineTests.cs` | `AuditScope_ActionFailureInEndpoint_ShouldRemainFailureOnHttp200` | 回归检测 | 端点内 `BeginAction("Orders","Create").Fail(...)` 后返回 200；Flush 后落库记录 IsSuccess=false；修复前会被覆盖为 true |
| P3 | `AuditingCoreTests.cs` | `Writer_ShouldIsolateRoutingFailure` | 回归检测 | router 对 TenantId="bad" 抛异常时：bad 未落库、good 恰好 1 条、bad spool 保留且 good spool 已删除、后续写入仍落库、Flush 不抛不挂（10s 超时）；修复前整批丢失 |
| P4 | `TelemetryCoreTests.cs` | `Metric_WithSameMetadata_ShouldReturnSameInstance` | 守卫 | 独立 source 下，同名同 unit/description 的 Counter 返回同一实例；Histogram 同样复用 |
| P4 | `TelemetryCoreTests.cs` | `Metric_WithConflictingMetadata_ShouldThrow` | 回归检测 | Counter/Histogram 同名但 unit 或 description 不同时均抛 `InvalidOperationException`；修复前静默复用，用例失败 |

### 修改 1 个既有用例（仅追加断言）

- `Request_ShouldResolveTenantAndWriteAudit`：在落库断言处新增 `Assert.True(record.IsSuccess)`，补齐 PLAN 要求的“普通 200 → IsSuccess=true”HTTP 管线正向路径；其余既有断言未改动。

### 关键测试设计

- **P2 Abort 的确定性回归**：`GatedSnapshotStore` 在 `SaveAsync/SaveManyAsync` 中等待 gate；测试在响应返回后释放 gate，再 Flush 并读取快照。因此快照一定发生在中间件 `FillRecord` 之后：修复前的无条件 HTTP 覆盖会留下 true，修复后保持 false。不再依赖 2s 批窗口或 `FailureDelivery` 时序。
- **P3 边界与 spool**：`RecordingSpool` 以 `spool:{ScopeId}` 记录持久化键；bad 路由失败后其键仍在 pending，good 落库后其键被删除。`RecordingStore` 保留落库记录，用于断言 bad 未落库、good 恰好 1 条；随后再写 good:2 并 Flush，证明单条隔离后消费循环仍健康。
- **P4 隔离**：使用独立 source 名 `test-metric-same-metadata` / `test-metric-conflicting-metadata`，避免跨用例指标缓存冲突。
- **OCE**：Coder 已回退该越界改动，按复审指示不再新增 OCE 用例。

## 构建

```bash
dotnet build Tests/Euonia.Core.Tests/Euonia.Core.Tests.csproj -c Debug --no-restore
dotnet build Tests/Euonia.Application.Tests/Euonia.Application.Tests.csproj -c Debug --no-restore
dotnet build Tests/Euonia.Hosting.Tests/Euonia.Hosting.Tests.csproj -c Debug --no-restore
```

结果：3 个项目均 **Build succeeded，0 Error**。仅有环境类/既有代码警告：`NU1900`（NuGet 漏洞缓存目录不可写）、`NU1510`（Hosting 既有 PackageReference 裁剪提示）、`xUnit1051`（既有测试行，不在本次新增代码）。目标框架 net11.0 / osx-arm64。

## 项目级测试

```bash
dotnet test --project Tests/Euonia.Core.Tests/Euonia.Core.Tests.csproj -c Debug --no-build --no-restore
dotnet test --project Tests/Euonia.Application.Tests/Euonia.Application.Tests.csproj -c Debug --no-build --no-restore
dotnet test --project Tests/Euonia.Hosting.Tests/Euonia.Hosting.Tests.csproj -c Debug --no-build --no-restore
```

| 项目 | total | failed | succeeded | skipped | 结果 |
| --- | ---: | ---: | ---: | ---: | --- |
| Euonia.Core.Tests | 113 | 0 | 113 | 0 | Passed |
| Euonia.Application.Tests | 216 | 0 | 216 | 0 | Passed |
| Euonia.Hosting.Tests | 9 | 0 | 9 | 0 | Passed |

本项目新增用例数为：Core 5（P1×3 + P4×2）、Application 1（P3）、Hosting 2（P2），全部通过。

## 全量测试

```bash
dotnet test --solution Euonia.Test.slnx -c Debug --no-restore --verbosity minimal
```

**全量汇总：**

| total | failed | succeeded | skipped | 结果 |
| ---: | ---: | ---: | ---: | --- |
| 1260 | 0 | 1247 | 13 | Passed |

关键项目在全量运行中的计数与项目级一致：Core 113/0、Application 216/0、Hosting 9/0。13 个 skipped 均为既有环境依赖用例（Redis 3 个、RabbitMQ 10 个），与本次变更无关。

## 失败详情

无失败用例，不存在需要打回 Coder 的业务缺陷。

## 复审问题落实

- [Major] “普通 200 → IsSuccess=true”已由 `Request_ShouldResolveTenantAndWriteAudit` 的 `Assert.True(record.IsSuccess)` 覆盖，报告说明已修正。
- [Major] P3 已补 bad 未落库、good 恰好 1 条、bad spool 保留、good spool 清理、后续写入仍落库。
- [Minor] Abort 已改为 gated 快照存储，去掉批窗口依赖，并同时断言 HttpStatusCode=200。
- [Minor] 报告已区分“回归检测 5 / 守卫 3”，不再统称 8 个回归用例。
- Coder 归属的 Blocker/OCE/Telemetry remarks/日志聚合问题不在 Tester 修改范围内，本轮仅验证其最终行为。

## git status --short

```text
 M Source/Euonia.Auditing/AuditingWriter.cs
 M Source/Euonia.Auditing/Internal/AuditingScope.cs
 M Source/Euonia.Hosting/Auditing/AuditingMiddleware.cs
 M Source/Euonia.Telemetry/TelemetrySource.cs
 M Source/Euonia.Tenancy/Abstractions/ITenantScopeFactory.cs
 M Source/Euonia.Tenancy/Accessors/TenantScopeFactory.cs
 M Tests/Euonia.Application.Tests/Auditing/AuditingCoreTests.cs
 M Tests/Euonia.Core.Tests/Telemetry/TelemetryCoreTests.cs
 M Tests/Euonia.Core.Tests/Tenancy/TenancyCoreTests.cs
 M Tests/Euonia.Hosting.Tests/TenancyAuditingPipelineTests.cs
?? PLAN.md
?? REVIEW_FEEDBACK.md
?? Source/Euonia.Auditing/Properties/
?? TEST_REPORT.md
```

（`Source/*` 与新增 `Source/Euonia.Auditing/Properties/` 来自 Coder；Tester 仅改动 4 个测试文件并新增本报告。）
