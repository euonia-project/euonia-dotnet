# Euonia.Pipeline 统一为纯泛型重构报告

## 概述
延续 Euonia.Pipeline 修复轮次，本次将 Euonia.Pipeline 的公开契约**统一为纯泛型**：删除非泛型管道家族与无消费者的单泛型委托/行为家族，只保留双泛型 `IPipeline<TRequest, TResponse>` 等类型。最终决策（方案 A，PascalCase `DefaultPipelineProvider<TRequest, TResponse>`）：删除冗余类型，不为 Pipeline 包引入 `System.Unit` 依赖；void 场景由消费方用 `System.Unit` 表达（Bus 已是 `IPipeline<..., Unit>`）。

统一后 `Euonia.Pipeline` 公开类型收敛为：

- `IPipeline<TRequest, TResponse>`、`IPipelineBehavior<TRequest, TResponse>`
- `PipelineDelegate<in TRequest, TResponse>`、`PipelineDelegateComponent<TRequest, TResponse>`
- `PipelineBase<TRequest, TResponse>`、`DefaultPipelineProvider<TRequest, TResponse>`
- `PipelineBehaviorAttribute`
- `IDelegateBehavior<TRequest>` + 静态门面 `Pipeline`（保留，见第二节）

验证结果：`Euonia.Build.slnx` 与 `Euonia.Test.slnx` 各 **0 错误**（Test.slnx 有 3 条既有 xUnit 分析器警告，均在无关的 Caching/Application 测试项目，非本次引入）；Pipeline.Tests **10/10**（原 11，删 1 个非泛型用例）、Bus.Tests **22/22**、InMemory.Tests **10/10**、Application.Tests **21/21** 全部通过。

---

## 一、删除清单

### 非泛型家族（整类删除）
| 类型 | 原文件 | 删除数量 |
| --- | --- | --- |
| `IPipeline` | `IPipeline.cs` | 接口及全部成员 |
| `IPipelineBehavior`（object 签名） | `IPipelineBehavior.cs` | 接口 |
| `PipelineDelegate(object)` | `PipelineDelegate.cs` | 委托 |
| `PipelineDelegateComponent(PipelineDelegate)` | `PipelineDelegateComponent.cs` | 委托 |
| `PipelineBase` | `PipelineBase.cs` | 整文件删除 |
| `DefaultPipelineProvider`（非泛型） | `DefaultPipelineProvider.cs` | 类（含 `GetNext`/`Compile<T>`/`GetServiceInfo`） |

删除依据（全库盘点）：仓库内非泛型 `IPipeline` **除定义本身与 `PipelineBase`/DI 注册外无任何消费方**——无 `GetService<IPipeline>` 解析、无非泛型 `IPipelineBehavior` 实现类；`AddPipeline` 中的 `AddTransient<IPipeline, DefaultPipelineProvider>()` 与测试中那一个 `new DefaultPipelineProvider(provider)` 是仅有的三两处引用。运行时类型创建能力（`Use(Type, ...)`、`UseOf(Type, ...)`、`PipelineBehaviorAttribute`）由泛型接口完整保留，删除不损失能力。

### 单泛型家族（仅有一个类型参数，无消费者）
| 类型 | 原文件 | 删除量 |
| --- | --- | --- |
| `IPipelineBehavior<TRequest>` | `IPipelineBehavior.cs` | 接口 |
| `PipelineDelegate<in TRequest>` | `PipelineDelegate.cs` | 委托 |
| `PipelineDelegateComponent<TRequest>` | `PipelineDelegateComponent.cs` | 委托 |

删除依据：这三个单泛型类型在整个仓库中**唯一的消费者**是静态 void 门面 `Pipeline.RunAsync<TRequest>`（`Pipeline.cs` 原 37-45 行）。删除它们的同时删除该门面方法后即成为死代码。经全库检索，除上述自身引用外没有任何实现、声明或调用。

---

## 二、保留与取舍

- **`IDelegateBehavior<TRequest>` + 静态同步门面 `Pipeline.Run<TRequest,TResponse>` / `Pipeline.Run<TRequest>` 保留**：它们是自包含的同步门面，不依赖 `IPipeline` 或上述删除类型，且上一轮已补回归测试（`StaticRun_*` 3 个）。删除徒增 API 破坏面，保留并在报告注明遗留观察。
- **静态 `Pipeline.RunAsync<TRequest>`（void）删除**：它依赖单泛型 `IPipelineBehavior<TRequest>` + `PipelineDelegate<TRequest>`，随家族一起移除；`Pipeline.RunAsync<TRequest,TResponse>`（双泛型）保留。
- **不引入 `System.Unit`**：void 语义由消费方（Bus 已用 `IPipeline<..., Unit>`）自行表达，Pipeline 包无需添加对 Euonia.Core 的引用，`csproj` 保持 `netstandard2.1` 仅依赖 DI.Abstractions。

---

## 三、受影响文件改动明细

| 文件 | 改动 |
| --- | --- |
| `Source/Euonia.Pipeline/IPipeline.cs` | 仅保留 `IPipeline<TRequest, TResponse>`；删非泛型接口（原 1-104 行） |
| `Source/Euonia.Pipeline/IPipelineBehavior.cs` | 仅保留 `IPipelineBehavior<TRequest, TResponse>`；删 object 版与单泛型版 |
| `Source/Euonia.Pipeline/PipelineDelegate.cs` | 仅保留双泛型委托 |
| `Source/Euonia.Pipeline/PipelineDelegateComponent.cs` | 仅保留双泛型组件委托 |
| `Source/Euonia.Pipeline/PipelineBase.cs` | **非泛型文件整文件删除**；`PipelineBase\`2.cs`（泛型基类）保留且文件名不动 |
| `Source/Euonia.Pipeline/DefaultPipelineProvider.cs` | 整文件重写为仅含 `DefaultPipelineProvider<TRequest, TResponse>`；非泛型类、其 `Compile<T>(object)` 与 `GetServiceInfo` 一并移除 |
| `Source/Euonia.Pipeline/Pipeline.cs` | 删 void `RunAsync<TRequest>`；保留 `RunAsync<TRequest,TResponse>` 与同步 `Run` 双门面 |
| `Source/Euonia.Pipeline/ServiceCollectionExtensions.cs` | `AddPipeline` 去掉 `AddTransient<IPipeline, DefaultPipelineProvider>()`，只保留 `AddTransient(typeof(IPipeline<,>), typeof(DefaultPipelineProvider<,>))`；更新 XML 注释 |
| `Tests/Euonia.Pipeline.Tests/PipelineRegressionTests.cs` | 删 `UntypedPipeline_Use_MethodBasedMultiParamHandler_WithTypedFirstParameter_BuildsAndRuns`（依赖被删除的非泛型 `DefaultPipelineProvider`）；其余 4 个回归用例保留并全绿 |

---

## 四、API 影响（公开契约破坏，需下游评估）
上述删除均为 `public` 类型/成员，属公开 API 破坏。仓库内（含测试与 Sample）经全量检索确认零调用方，影响面仅外部下游。若需兼容，可在版本升级上标注 major；本次仅作删除、不做 `[Obsolete]` 过渡（仓库内无任何缓解对象被引用）。

---

## 五、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误（3 条既有 xUnit 分析器警告，非本次引入）|
| `Euonia.Pipeline.Tests`（`dotnet exec`） | 10/10 |
| `Euonia.Bus.Tests` | 22/22 |
| `Euonia.Bus.InMemory.Tests` | 10/10 |
| `Euonia.Application.Tests` | 21/21 |
| 全库残留检查 | `IPipeline`/`PipelineBase`/`DefaultPipelineProvider` 非泛型引用 0；单泛型 `PipelineDelegate<T>`/`IPipelineBehavior<T>`/`PipelineDelegateComponent<T>` 引用 0 |

> 注：本机 `dotnet test`（xUnit.v3 MTP 适配器）会报 “Zero tests ran”/exit 5 的误报，故统一用 `dotnet exec <TestDll>` 跑真实测试，与既往轮次一致。

---

## 六、遗留观察（未改动，供后续决策）
- **`IDelegateBehavior<TRequest>` + 静态同步门面**仍是一块“未被消费、行为/返回值契约先天矛盾”的遗留面（`IDelegateBehavior.HandleAsync` 只返回 `Task`，响应无法经行为链回传，语义定为“行为先执行、accumulate 终结取值”）。本轮按既定方案保留；若团队不打算继续用，可整体删除或以 `IPipelineBehavior<TRequest,TResponse>` 重写。
- **attribute 优先级设计**：`UseOf` 在 `useAheadOfOthers=true` 时把行为统一推为 `int.MinValue`（attribute 声明的优先级被覆盖）属设计使然，保持。
- **无单例全局管道**：`AddPipeline` 现在只注册泛型 Transient 管道；若需要“进程级全局管道委托”，需另行设计。

关联文档：`docs/Euonia.Pipeline-Fixes-Report.md`（上一轮修复报告）、`docs/Euonia.Bus-Fixes-Report.md`（Bus 轮次）。