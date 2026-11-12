# 修复计划：Tenancy / Auditing / Telemetry 四个缺陷

## 目标

修复 Tenancy、Auditing、Telemetry 中的 4 个缺陷；不改公共 API 形状、不新增依赖、net10/net11 双目标编译通过、既有测试不回退。

## 缺陷与修复方案

### P1 Blocker — Tenancy 四参数 Begin 漏掉跨租户授权

- 位置：`Source/Euonia.Tenancy/Accessors/TenantScopeFactory.cs:62-90`（四参数 `Begin`），对照 `BeginCrossTenant`（93-118）。
- 问题：四参数 `Begin` 在 `actor != target` 时未校验 `CrossTenantScopeAuthorizer`，而 `BeginCrossTenant` 有校验；`TenantMessageScopeParticipant.cs:104` 正是走四参数入口，导致跨租户授权被绕过（越权）。
- 修复：
  1. 抽取私有方法 `EnsureCrossTenantAuthorized(target)`，承载 `CrossTenantScopeAuthorizer` 校验逻辑。
  2. `BeginCrossTenant` 与四参数 `Begin` 两处复用该方法。
  3. 四参数 `Begin` 仅在跨租户（`actor != target`）时调用；同租户路径保持不变、免授权。
  4. 更新 `ITenantScopeFactory` 的 XML 注释，明确四参数 `Begin` 的跨租户语义与未授权时的异常。

### P2 Major — Auditing 的 IsSuccess 被 HTTP 状态无条件覆盖

- 位置：`Source/Euonia.Hosting/Auditing/AuditingMiddleware.cs:83-96`（`FillRecord`）。
- 问题：`FillRecord` 无条件用 HTTP 状态覆盖 `IsSuccess`，从而覆盖了 `AbortAsync`、失败 Action、显式 `false` 的结论，造成审计假成功。
- 修复（判定优先级）：
  1. 记录上显式 `false`，或 `Record.Actions` 中任一失败 → `false`。
  2. 否则按 HTTP 状态判定（HTTP 失败仍为 `false`）。
  3. `tenant.failure` 分支继续为 `false`（保持现状）。

### P3 Major — AuditingWriter 因 GroupBy 键选择器丢整批

- 位置：`Source/Euonia.Auditing/AuditingWriter.cs:399-472`。
- 问题：使用 `GroupBy(router.GetStore)` 的键选择器解析 store，单条路由抛异常会让整批丢失。
- 修复：
  1. 先逐条解析 store，每条独立 try/catch：单条异常只把该条标记为失败并保留其 spool，不影响其余条目。
  2. 再按"成功解析出的 store"分组写入。
  3. 保持既有 immediate / batch / 逐条回退三种路径的行为与顺序语义不变。

### P4 Major — Telemetry 指标缓存只按 name 匹配

- 位置：`Source/Euonia.Telemetry/TelemetrySource.cs:161-186`。
- 问题：仅按 `name` 缓存指标，同名但 `unit` / `description` 不同的定义被静默忽略，埋点元数据失真。
- 修复：缓存条目记录完整元数据（name + unit + description）。元数据一致时复用已有指标；不一致时抛 `InvalidOperationException`，显式暴露冲突。

## 文件清单

### 业务源码（7 个：5 个原始修复 + 2 个复审修复引入）

| 文件 | 对应缺陷 |
| --- | --- |
| `Source/Euonia.Tenancy/Accessors/TenantScopeFactory.cs` | P1 |
| `Source/Euonia.Tenancy/Abstractions/ITenantScopeFactory.cs`（仅 XML 注释） | P1 |
| `Source/Euonia.Hosting/Auditing/AuditingMiddleware.cs` | P2 |
| `Source/Euonia.Auditing/AuditingWriter.cs` | P3 |
| `Source/Euonia.Telemetry/TelemetrySource.cs` | P4 |
| `Source/Euonia.Auditing/Internal/AuditingScope.cs`（复审修复引入：新增 `HasFailedActions()`） | P2 |
| `Source/Euonia.Auditing/Properties/AssemblyInfo.cs`（复审修复引入：`InternalsVisibleTo("Euonia.Hosting")`） | P2 |

调用点 `Source/Euonia.Tenancy/.../TenantMessageScopeParticipant.cs:104` 仅作验证，随 P1 修复自动受保护，原则上无需改动。

### 测试（4 个，Tester 负责）

- `Tests/Euonia.Core.Tests/Tenancy/TenancyCoreTests.cs`
- `Tests/Euonia.Hosting.Tests/TenancyAuditingPipelineTests.cs`
- `Tests/Euonia.Application.Tests/Auditing/AuditingCoreTests.cs`
- `Tests/Euonia.Core.Tests/Telemetry/TelemetryCoreTests.cs`

## 实施步骤

- **S1**：P1 — 抽取 `EnsureCrossTenantAuthorized`，两处复用，补 XML 注释。
- **S2**：P2 — 修正 `FillRecord` 的 `IsSuccess` 判定优先级。
- **S3**：P3 — 逐条解析 store、单条失败隔离，再按成功 store 分组写入。
- **S4**：P4 — 指标缓存记录元数据，一致复用、冲突抛 `InvalidOperationException`。
- **S5**：编译（net10/net11 双目标）+ 既有测试回归，确认不回退。

## 验收命令

```bash
dotnet build Tests/Euonia.Core.Tests/Euonia.Core.Tests.csproj -c Debug
dotnet build Tests/Euonia.Application.Tests/Euonia.Application.Tests.csproj -c Debug
dotnet build Tests/Euonia.Hosting.Tests/Euonia.Hosting.Tests.csproj -c Debug

dotnet test --project Tests/Euonia.Core.Tests/Euonia.Core.Tests.csproj -c Debug --no-build --no-restore
dotnet test --project Tests/Euonia.Application.Tests/Euonia.Application.Tests.csproj -c Debug --no-build --no-restore
dotnet test --project Tests/Euonia.Hosting.Tests/Euonia.Hosting.Tests.csproj -c Debug --no-build --no-restore

dotnet test --solution Euonia.Test.slnx -c Debug --no-restore --verbosity minimal
```

## 行为验收

- **P1**：未授权跨租户抛 `UnauthorizedAccessException`；授权通过时成功，且 actor/target/cross 参数正确；同租户调用无需授权。
- **P2**：端点内调用 `AbortAsync` 或 `BeginAction().Fail()` 后即使返回 200，`IsSuccess` 仍为 `false`；普通 200 为 `true`；500 为 `false`。
- **P3**：路由对 `bad` 记录抛异常时，`good` 记录仍正常落库，且 `Flush` 不挂起/不抛。
- **P4**：同名同元数据返回 Same 实例；同名但 unit 或 description 不同抛异常。

## Tester 策略

- 仅在上述 4 个测试文件中新增用例，禁止修改业务源码。
- 覆盖每个缺陷的正向与反向路径，并保留既有用例断言不变（不回退）。

## 约束

- 不改公共 API 形状：不增删/不改公共签名、可见性与类型形状；仅内部实现 + XML 文档注释。
- 不新增任何依赖。
- net10 / net11 双目标编译均需通过。
- 既有测试不得回退。

## 复审修复补充（第 1 轮）

- [Blocker] AuditingMiddleware 对 Record.Actions 的无锁枚举改为 AuditingScope.HasFailedActions()（lock(_sync) 内快照）；FillRecord 与 CompleteAsync 拆为独立 try/catch，回填失败不阻断提交。
- 新增业务文件（复审修复引入）：
  - Source/Euonia.Auditing/Internal/AuditingScope.cs（internal HasFailedActions）
  - Source/Euonia.Auditing/Properties/AssemblyInfo.cs（InternalsVisibleTo Euonia.Hosting）
- [Major] 回退 AuditingWriter 的 OCE 重抛，恢复 fail-open；路由解析失败日志按批聚合；TelemetrySource remarks 明确元数据冲突例外。
- 测试补充：普通 200 正向断言、GatedSnapshotStore 消除 P2 时序依赖、P3 bad 未落库/spool 保留/后续写入健康断言；TEST_REPORT 区分 5 回归 + 3 守卫。
- 验收命令与 P1-P4 行为标准不变；全量 1260/0/13 skipped。
