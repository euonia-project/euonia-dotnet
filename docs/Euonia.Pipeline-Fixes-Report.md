# Euonia.Pipeline 修复报告

## 概述
本次针对 `euonia-net` 仓库中 Euonia.Pipeline 项目与测试展开分析（延续 Euonia.Bus 修复轮次），全部修改集中在管道行为解析、同步门面与 DI 注册三类，并补全回归测试覆盖此前从未被测试触达的路径。

- **方法级行为解析修复**（2 处，同根问题）：
  `DefaultPipelineProvider` / `DefaultPipelineProvider<TRequest, TResponse>` 中 `Compile<T>` 的 `GetServiceInfo` 反射为 null 的问题，以及 contextArg / 首参数类型不匹配问题；
- **同步门面修复**（2 处）：
  `Pipeline.Run<TRequest, TResponse>` 与 `Pipeline.Run<TRequest>` 的聚合实现（`TargetParameterCountException`、值类型 NRE、Task 永不等待）；
- **DI 注册清理**（1 处）：
  `AddPipeline` 中无效的 `PipelineDelegate` no-op 注册；
- **回归测试补全**（5 个新测试）。

验证结果：`Euonia.Build.slnx` 与 `Euonia.Test.slnx` 各 **0 错误 0 警告**；Pipeline.Tests **11/11**（原 6 + 新增 5）、Bus.Tests **22/22**、InMemory.Tests **10/10**、Application.Tests **21/21** 全部通过。

---

## 一、方法级行为解析（`DefaultPipelineProvider` / 泛型 `Task<TResponse>` 版）

涉及文件：`Source/Euonia.Pipeline/DefaultPipelineProvider.cs`

### 问题 1：`GetServiceInfo` 反射到 null
**具体问题**：两个 `Compile<T>` 方法编译表达式树时，用 `Expression.Call(GetServiceInfo, providerArg, Constant(type))` 从服务提供程序解析“方法的其余参数”。但：

```csharp
// 非泛型版（原 179 行）——但 GetService 定义在 DefaultPipelineProvider 上，不在 PipelineBase 上
private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

// 泛型版（原 365 行）——GetService 定义在 DefaultPipelineProvider<TRequest, TResponse> 上
private static readonly MethodInfo GetServiceInfo = typeof(PipelineBase<,>).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
```

`PipelineBase` / `PipelineBase<,>` 上并没有 `GetService`（它声明在两个 `DefaultPipelineProvider` 里），因此 `GetServiceInfo == null`。只要行为走“方法级 Handle/HandleAsync + 多参数”路径，`Expression.Call(GetServiceInfo, ...)` 立即抛 **`ArgumentNullException`（Parameter 'method'）**——回归测试在修复前精确复现（`DefaultPipelineProvider.cs:139`）。

**修改依据**：改为从实际的声明类型反射：

```csharp
private static readonly MethodInfo GetServiceInfo =
    typeof(DefaultPipelineProvider).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

private static readonly MethodInfo GetServiceInfo =
    typeof(DefaultPipelineProvider<TRequest, TResponse>).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);
```

### 问题 2：contextArg 类型与首参数不匹配
**具体问题**：
- **泛型版**：`contextArg` 声明为 `Expression.Parameter(typeof(object))`，但最终 `Expression.Lambda<Func<T, TRequest, IServiceProvider, Task<TResponse>>>` 的第二个形参是 `TRequest`。只要 `TRequest != object`（Bus 里是 `IMessageEnvelope<TMessage>`，Osba 里是实体类型），`Expression.Lambda` 立即抛 `ArgumentException`（参数类型不匹配）。这单独就否决了“多参数方法级行为”在类型化管道中的可用性。
- **两个版本**：`methodArguments[0] = contextArg` 直塞给 `Expression.Call`，要求 Handle/HandleAsync 的**首参数恰好等于** contextArg 类型（`object`/`TRequest`）。方法首参声明为更强类型时（如 `HandleAsync(MyRequest request, ISvc svc)`），表达式树无法把一个 `object` 表达式绑定到 `MyRequest` 形参。

**修改依据**：泛型版让 `contextArg` 的类型跟随 `TRequest`，两版都对首参数按需加 `Expression.Convert`：

```csharp
// 泛型版
var contextArg = Expression.Parameter(typeof(TRequest), "context");
var firstParameterType = parameters[0].ParameterType;
methodArguments[0] = firstParameterType == typeof(TRequest) ? contextArg : Expression.Convert(contextArg, firstParameterType);

// 非泛型版（TRequest 即 object），首参数非 object 时同样补 Convert
var contextArg = Expression.Parameter(typeof(object), "context");
var firstParameterType = parameters[0].ParameterType;
methodArguments[0] = firstParameterType == typeof(object) ? contextArg : Expression.Convert(contextArg, firstParameterType);
```

修复后方法级多参行为（“其余方法参数在调用时从服务提供程序解析”，见 `GetNext` 的 XML 文档承诺）真正可用。**说明**：方法首参仍须是 `TRequest` 的基类/相等类型（`Convert` 方向固定），派生类型或无关类型属用法错误，会照常在编译期抛异常。

### 其余保持现状的限制（如实记录，不做改动）
- **单参数方法级路径**使用 `methodInfo.CreateDelegate(...)`，要求方法与委托签名恰好兼容（首参为 `TRequest` 或其基类）。非泛型 `PipelineDelegate(object)` 要求首参恰为 `object`；泛型 `PipelineDelegate<in TRequest, TResponse>` 支持逆变。此路径已在作用域内，未改动。
- 泛型 `Compile<T>` 保留对 `CancellationToken` 参数显式拒绝的语义（`NotSupportedException`）。

---

## 二、同步门面 `Pipeline.Run`
涉及文件：`Source/Euonia.Pipeline/Pipeline.cs`

### 问题 3：`Run<TRequest, TResponse>` 聚合完全失效
**具体问题**（原实现，`Aggregate` 折叠把一切变成“零参委托”再做 `DynamicInvoke`）：

```csharp
var response = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => () => behavior.HandleAsync(context, @delegate, default));
return (TResponse)response.DynamicInvoke(context);
```

- 种子 `accumulate` 是 `Action<TRequest>`，折叠体产出的是**无参** `() => Task` 委托；
- `response.DynamicInvoke(context)` 向无参委托传入 `context` → **≥1 个行为时必然抛 `TargetParameterCountException`**（回归测试修复前精确复现，栈顶 `Pipeline.cs:72`）；
- 无常行为时 `response` 就是 `Action<TRequest>`（void），`.DynamicInvoke(context)` 返回 null → `(TResponse)null`：值类型 TResponse 直接 `NullReferenceException`（拆箱 null）；
- 行为返回的 `Task` 从未被等待。
- 且 `accumulate` 声明为 `Action<TRequest>`（void），一个有 `TResponse` 的同步 API 根本无法产生响应值——签名本身就是错的（新测试在该签名下连编译都无法通过：`CS8030`）。

另一个附带问题：`Run<TRequest>`（void 版）共享同一个折叠缺陷，≥1 个行为同样抛 `TargetParameterCountException`。

**修改依据**：仓库内（经全库检索）没有任何调用方，静态 `Run`/`RunAsync`、`IDelegateBehavior` 均属未被使用但有公开契约的遗留 API。为“能用且可预期”，修复为链式同步折叠：

```csharp
public static TResponse Run<TRequest, TResponse>(
    TRequest context, Func<TRequest, TResponse> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
{
    var @delegate = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => (Func<TRequest, TResponse>)(request =>
    {
        behavior.HandleAsync(request, @delegate, default).GetAwaiter().GetResult();
        return ((Func<TRequest, TResponse>)@delegate)(request);
    }));

    return ((Func<TRequest, TResponse>)@delegate)(context);
}

public static void Run<TRequest>(
    TRequest context, Action<TRequest> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
{
    var @delegate = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => (Action<TRequest>)(request =>
    {
        behavior.HandleAsync(request, @delegate, default).GetAwaiter().GetResult();
        ((Action<TRequest>)@delegate)(request);
    }));

    ((Action<TRequest>)@delegate)(context);
}
```

要点：
- `accumulate`（`Run<TRequest, TResponse>` 版）由 `Action<TRequest>` 改为 **`Func<TRequest, TResponse>`**——这是让该重载“返回响应”的唯一自洽签名（无调用方，改签名安全）；
- 行为按注册顺序链式执行，各自 `Task` 在调用线程同步等待（`GetAwaiter().GetResult()`），终结点 `accumulate` 产出响应值；
- 行为收到的 `next` 是“下方委托”（继续传递语义），修复后行为先于 accumulate 执行（语义与异步静态 `RunAsync` 一致）；
- **已知限制（如实记录）**：`IDelegateBehavior.HandleAsync` 只返回 `Task`（void），响应无法经行为链回传，因此语义定义为“行为在前、accumulate 终结取值”。若某行为在自身内部再次 `DynamicInvoke(next)`，内层链会执行两次——这是该接口契约本身的先天矛盾，非本次引入。

---

## 三、DI 注册清理
涉及文件：`Source/Euonia.Pipeline/ServiceCollectionExtensions.cs`

### 问题 4：`AddPipeline` 注册了一个永远 no-op 的 `PipelineDelegate`
**具体问题**：

```csharp
services.AddTransient(provider =>
{
    var pipeline = provider.GetService<IPipeline>();
    ...
    var @delegate = pipeline.Build();
    return @delegate;
});
```

每次解析都得到**全新** Transient 的 `DefaultPipelineProvider`（无任何组件），`Build()` 恒产出空管道委托 `_ => Task.CompletedTask`——一个注册了但什么也不做的服务；且 `IPipeline` 注册本身是 Transient，跨解析不可能累积组件，注册在结构上是无意义的。全库（含测试）检索不到任何消费方。

**修改依据**：移除这段注册与 `NuGet` 相关 XML 注释。若某天需要全局 `PipelineDelegate`，应改为对单例 `IPipeline` 实例做配置后缓存构建结果，而不是 Transient 实例上裸 `Build()`。移除后 `AddPipeline` 仍注册 `IPipeline` 与泛型 `IPipeline<,>`，业务行为不受影响。

---

## 四、回归测试补全

新增 `Tests/Euonia.Pipeline.Tests/PipelineRegressionTests.cs`（5 个测试，均为修复前“红灯”验证过）：
- `TypedPipeline_Use_MethodBasedMultiParamHandler_BuildsAndRuns`——泛型管道 `Use(Type)` 多参数方法级行为：修复前 Build 期 `ArgumentNullException`，修复后正常执行并返回响应；
- `UntypedPipeline_Use_MethodBasedMultiParamHandler_WithTypedFirstParameter_BuildsAndRuns`——非泛型管道、首参数为强类型（同问题 1/2）；
- `StaticRun_returns_accumulate_result_and_runs_behaviors_in_order`——同步门面带行为：修复前 `TargetParameterCountException`；
- `StaticRun_without_behaviors_returns_accumulate_result`——无常行为时正确取 accumulate 值（修复前值类型拆箱 null）；
- `StaticRun_void_runs_behaviors_then_accumulate`——void 同步门面（修复前同样 `TargetParameterCountException`）。

原有 `PipelinePriorityTests.cs`（优先级/注册顺序/attribute 路径，6 个）保持不动、全绿。

---

## 五、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误 0 警告 |
| `Euonia.Pipeline.Tests`（`dotnet exec`） | 11/11 |
| `Euonia.Bus.Tests` | 22/22 |
| `Euonia.Bus.InMemory.Tests` | 10/10 |
| `Euonia.Application.Tests` | 21/21 |

> 注：本机 `dotnet test`（xUnit.v3 MTP 适配器）会报 “Zero tests ran”/exit 5 的误报，故统一用 `dotnet exec <TestDll>` 跑真实测试，与 Euonia.Bus 轮次一致。

## 六、遗留观察（未改动，供后续决策）
- **`IDelegateBehavior` + 静态 `Pipeline`** 整体是几乎未使用、且行为与返回值契约先天矛盾（见问题 3 的已知限制）：若团队不打算继续用，可考虑整体删除或标注 `[Obsolete]`；
- **`PipelineBase.Build` 全部路径都经由排序 + 聚合**，语义清晰无需动；`RunAsync(context, accumulate)` 内 `Task.Run` 二次调度属无害设计，保留；
- **`AddPipeline` 无单例管道**：若需要“进程级全局管道委托”，需按上文重新设计，本次未引入。