# REVIEW_FEEDBACK — 第 2 轮复审（Tenancy / Auditing / Telemetry 四缺陷修复）

[PASS]

> 复审方式：只读审查 `PLAN.md`、`TEST_REPORT.md`、完整 `git diff`（10 个修改文件 + 未跟踪 `Source/Euonia.Auditing/Properties/AssemblyInfo.cs`），未运行 build/test、未修改任何业务代码/测试/配置（本文件除外）。
> 静态核对：`git diff --check` 干净；测试 diff 删除行 = 0（纯新增 + 1 条追加断言）；Hosting 用例数 1+1+7=9 与报告一致。

**结论：通过。** 第 1 轮 1 个 Blocker + 3 个 Major 均已实质关闭，修复方式与 PLAN 一致、未扩大公共 API 形状；剩余 6 条均为不阻断的 [Minor]。

## 一、第 1 轮问题关闭核验

| # | 第 1 轮问题 | 关闭证据 | 结论 |
| --- | --- | --- | --- |
| Blocker 1 | `AuditingMiddleware` 无锁枚举 `Record.Actions`，异常会跳过 `CompleteAsync` 导致整条记录丢失 | `AuditingScope.cs:94-104` 新增 `HasFailedActions()`，在 `lock (_sync)` 内取快照（与 `OnActionSealed` 的 `:131` 写入同锁）；`AuditingMiddleware.cs:99-101` 已无任何直接枚举；`:64-88` 把 `FillRecord` 与 `CompleteAsync` 拆成两个独立 try/catch，回填失败不再阻断提交。`AssemblyInfo.cs:3` 的 `InternalsVisibleTo("Euonia.Hosting")` 与仓库既有惯例一致（无强签名） | 已关闭 |
| Major 2 | OCE 无条件重抛杀死消费循环 / `FlushAsync` 挂起 / `StopAsync` 抛异常 | `AuditingWriter.cs` 已无新增 OCE 分支（仅剩 `ProcessAsync:362` 原有取消捕获与 `:164/:570` 的 `when` 过滤）；外层 `:513` 恢复 `catch (Exception)` fail-open；`:412-415` 的路由解析对单条异常（含 OCE）按条隔离 | 已关闭 |
| Major 3 | `TEST_REPORT.md` 谎称 200→true 已覆盖 | `TenancyAuditingPipelineTests.cs:307` 已在既有 200 用例追加 `Assert.True(record.IsSuccess)`；`TEST_REPORT.md:36-38, 93` 如实披露"修改 1 个既有用例（仅追加断言）" | 已关闭 |
| Major 4 | OCE 改动超出 PLAN 范围且无测试 | 已回退；`PLAN.md:105-113` 增补"复审修复补充（第 1 轮）"，`TEST_REPORT.md:45` 说明"按复审指示不再新增 OCE 用例" | 已关闭 |
| Minor(测试) | P3 未验证 bad 未落库 / spool 保留 / 循环健康 | `AuditingCoreTests.cs:439-453` 覆盖 good 恰 1 条、bad 未落库、`spool:bad:1` 保留且 `spool:good:1` 已删、后续写入仍落库 | 已关闭 |
| Minor(时序) | Abort 用例依赖 2s 批窗口、无法断言 HttpStatusCode | `GatedSnapshotStore`（`TenancyAuditingPipelineTests.cs:153-203`）在 `SaveAsync/SaveManyAsync` 等待 gate，`:352` 响应返回后放行，快照必然晚于 `FillRecord`；新增 `Assert.False(record.IsSuccess)` / `Assert.Equal(200, record.HttpStatusCode)`（`:358-359`），修复前必失败、修复后确定通过 | 已关闭 |
| Minor(日志) | 路由解析失败按条刷日志 | `AuditingWriter.cs:403, 429-441` 改为按批计数 + 首个异常聚合，仅一条 Error/Warning | 已关闭 |
| Minor(文档) | `TelemetrySource` remarks 与 P4 抛异常矛盾 | `TelemetrySource.cs:10-13` 明确"除 Counter/Histogram 同名元数据冲突抛出 InvalidOperationException 外，其余异常一律吞掉" | 已关闭 |

补充核对：P1/P4 业务改动与第 1 轮一致（`TenantScopeFactory.cs:80-84, 124-133`；`TelemetrySource.cs:169-172, 187-192, 212-249`），未见行为回退；新增 `AssemblyInfo.cs` 仅一行 IVT，未引入依赖、未改变公共 API 形状。

## 二、剩余问题（均 [Minor]，不阻断）

1. `Source/Euonia.Hosting/Auditing/AuditingMiddleware.cs:99-101` — 失败动作判定只对内置 `AuditingScope` 生效（`scope is AuditingScope`）。`IAuditingRecorder`/`IAuditingScope` 是公开扩展点，宿主自定义实现时 `HasFailedActions` 不会被调用，失败动作重新变成"200 即成功"（旧代码会读取 `record.Actions`）。建议：非内置作用域回退为防御式快照（`try { record.Actions.ToArray(); } catch (InvalidOperationException)`），或在 `IAuditingScope` XML 注释中声明"HTTP 失败动作判定依赖内置作用域"。
2. `Source/Euonia.Hosting/Auditing/AuditingMiddleware.cs:95-101` — `record.IsSuccess` 的"读 → 判定 → 写"未与 `AbortAsync`/`CompleteAsync` 的 `lock(_sync)` 处于同一临界区（`AuditingScope.cs:173-195`）。后台线程在回填读取之后、提交快照之前 Abort，仍可能被覆盖为 true。非本轮回归（旧实现更差），但建议把最终判定下沉为作用域内受锁保护的单一方法（如 `CompleteWithHttpResult(bool transportFailed, int status)`），让显式失败与 HTTP 结论原子化。
3. `Source/Euonia.Telemetry/TelemetrySource.cs:231` — `Resolve` 每次调用都 `new List<string>(2)`，指标获取是热路径（无冲突时也分配）。建议先做两次 `string.Equals` 比较，仅在冲突分支拼装消息。
4. `Tests/Euonia.Application.Tests/Auditing/AuditingCoreTests.cs:436-437, 452` — 整个用例共用一个 10s `CancellationTokenSource`，且沿用默认 2s `BatchInterval`（两次 Flush 合计约 4s，共享预算）。建议显式 `BatchInterval = TimeSpan.FromMilliseconds(10)` 并为两次 Flush 各自设置超时，进一步缩短耗时、降低 CI 抖动。
5. `Tests/Euonia.Application.Tests/Auditing/AuditingCoreTests.cs:816-830` — `RecordingStore.Records` 是无锁 `List<>`，当前依赖 `FlushAsync` 完成后的先后关系（可用），但该 helper 若被并行用例复用会变成数据竞争；建议返回快照或加锁复制。
6. `PLAN.md:45-53` — 文件清单仍写"业务源码（5 个）"，新增的 `Internal/AuditingScope.cs`、`Properties/AssemblyInfo.cs` 只在 `:105-113` 补充段落提及。建议同步表格，避免清单与 `git status` 不一致。

## 三、结论与归属

- **Blocker 0 / Major 0 / Minor 6** → 判定 **[PASS]**，无必须修复项，不阻塞合并。
- 上述 Minor 归属：1、2 为 Coder（业务，建议下轮处理）；3 为 Coder（性能，可选）；4、5 为 Tester（测试稳健性，可选）；6 为文档（PLAN.md 维护者）。
- 验证限制：本轮未执行 build/test（只读复审），`TEST_REPORT.md` 中的 113/216/9 与全量 1260/0/13 计数按报告采信；已独立核对测试文件 diff 为纯新增、`git diff --check` 干净、Hosting 用例总数 9 与报告一致。
