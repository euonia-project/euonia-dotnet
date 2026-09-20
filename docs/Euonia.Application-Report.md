# Euonia.Application 报告（修复补全 + 实用功能增强）

## 第一部分 修复与测试补全

### 概述
对 Euonia.Application 做一轮「修复 + 测试补全」：

- 修复 **`[NotNull]` 参数检查死代码**导致空参不抛异常的缺陷（先红灯后修复）；
- 补全此前无测试覆盖的区域：**锁令牌占位符**（`{param}` / `{param.Property}`）语义、**UseCase 接口族**（含非泛型入口适配）、**`DefaultUseCasePresenter`** 事件行为。

验证结果：`Euonia.Build.slnx` 0 错误 0 警告；`Euonia.Test.slnx` 0 错误（Application.Tests 仅剩既有 xUnit1031 警告，非本次引入）；全仓 14 个测试程序集 `dotnet exec` 全绿（Application.Tests **38/38**，由 21 扩至 38）。

---

## 一、缺陷修复：`[NotNull]` 空参不抛异常（`Interceptors/ValidationInterceptor.cs`）

### 现象
`ValidationInterceptor.Intercept` 先按参数类型过滤实参，再检查 `[NotNull]`：

```csharp
if (!parameter.ParameterType.IsInstanceOfType(argument))
{
    continue;                            // ①
}

if (parameter.NotNullAttribute != null && argument == null)
{
    throw new ValidationException(...);  // ② 死代码
}
```

`Type.IsInstanceOfType(null)` 对任意类型恒返回 `false`，因此 **null 实参总在 ① 就被 `continue` 跳过**，② 的 `[NotNull]` 校验永远无法触发：空参静默通过，违背方法的显式契约。

### 修复
将 `[NotNull]` 空参检查**前移**到类型匹配过滤之前，保证 null 实参先命中校验：

```csharp
if (parameter.NotNullAttribute != null && argument == null)
{
    throw new ValidationException($"Parameter '{parameter.Name}' is required in method '{method.Name}'.");
}

if (!parameter.ParameterType.IsInstanceOfType(argument))
{
    continue;
}

if (parameter.ValidationAttribute != null)
{
    Validate(argument, parameter.ParameterType);
}
```

类型检查仅用于过滤「非 null 但类型不匹配」的实参，不再角色混淆地吞掉 null。

### 回归测试（`ValidationInterceptorTests.cs`，新增 4 用例）
| 用例 | 覆盖 |
| --- | --- |
| `NotNullParameter_WithNullArgument_ShouldThrow` | **红灯验证**：修复前 null 实参不抛异常（`Assert.ThrowsAsync` 失败 "No exception was thrown"）；修复后抛 `ValidationException` |
| `NotNullParameter_WithValueArgument_ShouldPass` | 有值实参正常通过并返回结果 |
| `ValidationAttribute_WithInvalidPayload_ShouldThrow` | `[Validation]` 参数内嵌 `IValidatableObject` 校验失败抛 `ValidationException` |
| `ValidationAttribute_WithValidPayload_ShouldPass` | `[Validation]` 参数校验通过并返回结果 |

> 测试服务 `ValidationTestService` 同时实现接口代理路径（特性标注在实现类方法上），并在 `[NotNull]`/`[Validation]` 两种特性下验证。

---

## 二、测试补全：锁令牌占位符语义（`LockTokenTests.cs`，新增 3 用例）

锁令牌支持 `{parameterName}` 与 `{parameterName.PropertyName}` 占位符（`LockInterceptor.ResolveToken`），此前无任何测试。本次以「并发/互斥」反向验证占位符**确实按实参值替换**：

| 用例 | 覆盖 |
| --- | --- |
| `PlaceholderToken_DifferentIds_ShouldRunConcurrently` | token `"item:{id}"`，两个不同 id ⇒ 两个独立信号量 ⇒ 并发到达（MaxConcurrent=2）。若占位符未替换（退化为字面令牌）则 MaxConcurrent 只能为 1 |
| `PlaceholderToken_SameId_ShouldSerialize` | 相同 id ⇒ 同一把锁 ⇒ 10 个任务串行（MaxConcurrent=1） |
| `NestedPropertyPlaceholder_DifferentPayloads_ShouldRunConcurrently` | token `"nested:{payload.Id}"`，嵌套属性占位符按实参属性值替换 ⇒ 并发到达（MaxConcurrent=2） |

> 测试服务 `TokenLockService` 使用延迟临界区 + `MaxConcurrent` 静态计数，可靠区分"分锁并发"与"共锁互斥"。

---

## 三、测试补全：UseCase 接口族与 Presenter（`UseCaseTests.cs`，新增 10 用例）

### `UseCaseTests`（5 用例）
| 用例 | 覆盖 |
| --- | --- |
| `TypedUseCase_ShouldReturnOutput` | `IUseCase<TInput,TOutput>.ExecuteAsync(TInput)` 返回强类型输出 |
| `NonGenericEntry_ShouldRouteToTypedUseCase` | 经非泛型 `IUseCase.ExecuteAsync(object)` 入口正确分派到泛型实现并强转类型 |
| `NonOutputUseCase_ShouldReturnEmptyOutput` | `INonOutputUseCase<TInput>` 执行副作用后返回 `EmptyUseCaseOutput` |
| `NonInputUseCase_ShouldIgnoreEmptyInput` | `INonInputUseCase<TOutput>` 忽略 `EmptyUseCaseInput` 直接产出输出 |
| `ParameterlessUseCase_ShouldReturnEmptyInputAndOutput` | `IParameterlessUseCase` 无参执行，经非泛型入口返回 `EmptyUseCaseOutput` |

### `DefaultUseCasePresenterTests`（5 用例）
| 用例 | 覆盖 |
| --- | --- |
| `Ok_ShouldSetOutputAndRaiseOnSucceed` | `Ok(output)` 设置 `Output` 并触发 `OnSucceed`（携带输出） |
| `Error_OrdinaryException_ShouldRaiseOnFailed` | 普通异常触发 `OnFailed` |
| `Error_CancellationException_ShouldRaiseOnCanceled` | `OperationCanceledException` 触发 `OnCanceled` |
| `Dispose_ShouldDetachAllEventHandlers` | `Dispose` 后清空事件订阅（不再触发） |
| `Ok_WithoutSubscribers_ShouldNotThrow` | 无订阅者时 Ok/Error 均不抛空引用 |

---

## 四、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误；1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Application.Tests`（`dotnet exec`） | **38/38**（21 → 38，新增 17） |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Osba 131 / Core 78 / Linq 38 / Domain 22 / Bus 22 / Bus.InMemory 10 / Bus.RabbitMq 10 / Pipeline 10 / Caching.Memory 9 / Caching.Runtime 9 / Caching.Default 4 / Mapping.Automapper 3 / Mapping.Mapster 3） |

> 注：本机 `dotnet test`（xUnit.v3 MTP 适配器）会报 "Zero tests ran"/exit 5 误报，统一用 `dotnet exec <TestDll>` 跑真实测试。

## 五、遗留观察（未改动，供后续决策）
- **`ValidationInterceptor` 的类型过滤在非 null 情形基本不失效**：接口代理下 `invocation.MethodInvocationTarget` 优先取实现类方法，参数特性标注在实现类或接口方法均可命中——这与 `LockInterceptor`/`AuthorizationInterceptor` 的属性查找策略一致，均已有回归覆盖。本次仅修正 null 被提前跳过的问题。
- **`UserContextBehavior` 仍有未测试的空代码块**（`{ /* prevent code analysis */ }`，`Behaviors/UserContextBehavior.cs:68`）：无副作用，可后续清理；其元数据注入依赖 scoped 的 `UserPrincipal`/`IRequestContextAccessor` 模拟，留待需要时补测。
- **锁令牌解析走 `MethodInvocationTarget ?? Method`**：占位符参数名取自实现类方法；若接口与实现的参数名不一致，占位符匹配以实现类为准，接口上标注占位符时请保持参数名一致。

---

# 第二部分 实用功能增强

## 一、概述
在「修复 + 测试补全」基础上，为 Euonia.Application 增加一批实用功能并配套测试：

1. **UseCase 执行器**（`IUseCaseExecutor` / `UseCaseExecutor`）：统一「执行用例 + 结果分发」入口，串联全部四类 UseCase 与 presenter；
2. **UserContext 强化**：元数据键提取为常量、清理死代码块、补行为注入测试；
3. **Logging/Tracing 拦截器测试**：基于内存日志捕获验证日志行为；
4. **应用服务筛选注册**：`AddApplicationService` 谓词筛选重载 + `IServiceContext` 类型筛选支持；
5. **`BaseApplicationService` 便捷属性**：新增 `RequestContext` / `RequestAborted` / `Logger`；
6. **类级授权**：`AuthorizationInterceptor` 支持类级 `[Authorize]`，并补行为测试。

验证结果：`Euonia.Build.slnx` 0 错误 0 警告；`Euonia.Test.slnx` 0 错误（仅既有 xUnit1031 警告）；Application.Tests **67/67**（38 → 67，新增 29）；全仓 14 程序集全绿。

## 二、UseCase 执行器（新增 `UseCase/IUseCaseExecutor.cs` + `UseCase/UseCaseExecutor.cs`）

此前 UseCase 与 presenter 各自独立，调用方需自行编写 try/catch 分发。执行器收敛该过程：

```csharp
public interface IUseCaseExecutor
{
    Task ExecuteAsync<TInput, TOutput>(IUseCase<TInput, TOutput> useCase, TInput input, IUseCasePresenter<TOutput> presenter, CancellationToken cancellationToken = default);
    Task ExecuteAsync<TInput>(INonOutputUseCase<TInput> useCase, TInput input, IUseCasePresenter<EmptyUseCaseOutput> presenter, ...);
    Task ExecuteAsync<TOutput>(INonInputUseCase<TOutput> useCase, IUseCasePresenter<TOutput> presenter, ...);
    Task ExecuteAsync(IParameterlessUseCase useCase, IUseCasePresenter<EmptyUseCaseOutput> presenter, ...);
}
```

- 成功 → `presenter.Ok(output)`；
- 失败（含 `OperationCanceledException`）→ `presenter.Error(exception)`，由 presenter 自行决定取消语义；
- 无输出/无输入/无参用例经 `EmptyUseCaseOutput.Instance` 与 `EmptyUseCaseInput` 适配到统一路径；
- 已注册到 DI：`ApplicationModule` 中 `IUseCaseExecutor → UseCaseExecutor`（Transient）。

测试（`UseCaseTests.cs` 新增 `UseCaseExecutorTests`，7 用例）：typed 成功分发、异常分发、`NonOutput`/`NonInput`/`Parameterless` 适配、取消分发、DI 可解析。

## 三、UserContext 强化（`Behaviors/UserContextMetadataKeys.cs` + `Behaviors/UserContextBehavior.cs`）

- **键名常量化**：`Authorization` 与 `$nerosoft:user.*` 四键提取为 `UserContextMetadataKeys`，消除实现与消费两端的魔法字符串；
- **清理死代码**：移除上一轮遗留的空代码块 `{ /* prevent code analysis */ }`；
- 逻辑不变：Bearer 令牌（非 `Bearer null`）写入 `Authorization`，已认证用户写入 name/id/code/tenant。

测试（`UserContextBehaviorTests.cs`，6 用例，覆盖常量语义与回归）：有 token 写入、无 token 不写入、`Bearer null` 不写入、已认证用户四键写入、匿名用户不写用户键、`next` 正常调用。

> 注：行为依赖 scoped 的 `UserPrincipal`/`IRequestContextAccessor`，测试通过 `StubRequestContextAccessor`（singleton 注册）+ `AddScoped` 用户实例注入。

## 四、Logging/Tracing 拦截器测试（新增 `InterceptorTests.cs`）

此前五类拦截器中 Logging/Tracing 无任何直接测试。本次新建内存日志提供程序 `InMemoryLoggerProvider` 捕获条目并断言：

| 用例 | 覆盖 |
| --- | --- |
| `LoggingInterceptor_DebugEnabled_ShouldLogMethodAndArguments` | Debug 级别记录方法名与参数 JSON |
| `LoggingInterceptor_DebugDisabled_ShouldNotLog` | Debug 未启用时零日志输出 |
| `LoggingInterceptor_Exception_ShouldLogErrorAndRethrow` | 异常以 Error 记录后原样重抛（`DivideByZeroException`） |
| `TracingInterceptor_WithAccessorAndDebug_ShouldLogTrace` | 注入 `IRequestContextAccessor` 且 Debug 启用时输出 `TraceInfo` |
| `TracingInterceptor_WithoutAccessor_ShouldNotLog` | 未注入访问器时无 `TraceInfo`（避免 StackTrace 开销） |

## 五、服务筛选注册（`Extensions/ServiceCollectionExtensions.cs` + `Seedwork/IServiceContext.cs`）

- **`AddApplicationService(Assembly, lifetime, Func<Type,bool> filter)` 重载**：现有双参重载转发（filter=null 不筛选）；私有 `AddApplicationService(TypeInfo[], lifetime, filter)` 在既有「IsClass + 非抽象 + 实现 IApplicationService」基础上追加谓词；
- **`IServiceContext.ApplicationServiceTypeFilter`**：接口新增属性，`ServiceContextBase` 默认返回 null（不筛选），派生上下文可覆写；`Register<TService>()` 扫描时自动应用。

测试（`ServiceRegistrationFeatureTests.cs`，4 用例）：谓词筛选只注册 Alpha、无谓词全注册、`AlphaOnlyServiceContext`（覆写筛选器）经 `Register<>` 生效、默认上下文全注册。

## 六、BaseApplicationService 便捷属性（`Services/BaseApplicationService.cs`）

新增三个懒解析便捷属性，减少派生服务样板代码：

- `RequestContext`：当前请求上下文（无访问器或请求外为 null）；
- `RequestAborted`：当前请求取消令牌（非请求流内为 `CancellationToken.None`）；
- `Logger`：以当前服务类型为类别的日志记录器（`ILoggerFactory` 缺失时回退 `NullLogger`）。

测试（`ServiceRegistrationFeatureTests.cs`，2 用例）：解析 `ILoggerProbeService`（显式接口实现暴露基类属性）验证 `Logger` 非空、无请求流时 `RequestContext` 为 null 且 `RequestAborted == None`。

## 七、类级 [Authorize]（`Interceptors/AuthorizationInterceptor.cs`）

属性查找从「目标方法 / 接口方法」扩展到「目标声明类型 / 接口声明类型」：

```csharp
key.Target.GetCustomAttribute<AuthorizeAttribute>()
?? key.Interface.GetCustomAttribute<AuthorizeAttribute>()
?? key.Target.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>()
?? key.Interface.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>()
```

实现类标注 `[Authorize]` 时，其全部方法均受保护（无需逐方法标注）。

测试（`AuthorizationInterceptorTests.cs`，5 用例）：类级 `[Authorize(Roles="admin")]` 下「管理员放行 / 非管理员抛 `UnauthorizedAccessException` / 匿名抛 `AuthenticationException`」+ 方法级角色授权通过/拒绝。

## 八、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误；1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Application.Tests`（`dotnet exec`） | **67/67**（38 → 67，新增 29） |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Osba 131 / Core 78 / Linq 38 / Domain 22 / Bus 22 / Bus.InMemory 10 / Bus.RabbitMq 10 / Pipeline 10 / Caching.Memory 9 / Caching.Runtime 9 / Caching.Default 4 / Mapping.Automapper 3 / Mapping.Mapster 3） |

## 九、遗留观察（未改动，供后续决策）

- **类级 `[Authorize]` 与缓存键**：`_attributeCache` 以 `(MethodInfo Target, MethodInfo Interface)` 为键，类级查找按目标方法声明类型进行；若同一实现类被多个接口代理，仍逐方法命中缓存，性能无虞。
- **`UseCaseExecutor` 仅执行实例分发，不含 DI 自动解析**：若需要「按接口类型从容器解析用例并执行」，可作为后续增强（当前保持"应用层无容器依赖"的职责边界）。
- **`TracingInterceptor` 依赖 `IRequestContextAccessor` 判定是否启用**；若未来需要基于链路 ID 关联多个请求，可引入 `CorrelationId` 处理（与 Domain 轮 `Event.CorrelationId` 呼应），留待后续。