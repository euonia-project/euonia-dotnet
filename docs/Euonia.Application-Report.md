# Euonia.Application 报告（修复补全 + 实用功能增强 一/二/三）

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
- **`UseCaseExecutor` 的容器解析重载**：`ExecuteAsync<TUseCase,...>` 直接以类型参数从 `IServiceProvider` `GetRequiredService`，注册备用类型；若需要更细粒度的作用域/生命周期控制，可经 `IServiceScopeFactory` 每次新建作用域，留待后续。
- **`TracingInterceptor` 依赖 `IRequestContextAccessor` 判定是否启用**；`CorrelationIdBehavior` 已补上链路标识透传，若需将 CorrelationId 一并写入日志条目或 `TraceInfo`，可作为后续增强。

---

# 第三部分 实用功能增强（二）

对 Euonia.Application 追加第二批实用功能：日志脱敏、用户上下文重建、UseCase 容器解析、关联 ID 透传。全程先补测试后实现，全仓无回归。

## 十、日志脱敏：`SensitiveDataAttribute` + `LoggingInterceptor` 掩码

新增 `Attributes/SensitiveDataAttribute.cs`（`AttributeTargets.Parameter`，自带可配置 `Mask`，默认 `***`）。`LoggingInterceptor` 在记录参数前按以下规则掩码：

1. 参数名命中内置敏感关键字（`password`/`passwd`/`pwd`/`secret`/`token`/`accessToken`/`refreshToken`/`apikey`/`apiKey`/`key`/`authorization`/`credential`/`cookie`/`connectionString`，`OrdinalIgnoreCase`）；
2. 参数标注 `[SensitiveData]`。

每个方法的敏感性数组经 `ConcurrentDictionary<MethodInfo,bool[]>` 缓存，避免每次调用反射枚举；掩码替换为 `***` 后仍以 Debug 级 JSON 序列化记录。异常分支行为不变（Error 级日志后原样重抛）。

测试（`InterceptorTests.cs`，3 新增）：关键字命中掩码、`[SensitiveData]` 特性掩码、普通参数原值记录。

## 十一、用户上下文重建：`UserContextExtensions`

与 `UserContextBehavior` 写入互为镜像，新增 `Extensions/UserContextExtensions.cs`：

- `GetAuthorizationToken()`：读取 `Authorization` 元数据键；
- `GetUserPrincipal()`：读取 `UserName`/`UserId`/`UserCode`/`UserTenant` 四键，任一存在即以 `Bearer` 身份类型构造 `ClaimsPrincipal` 重建 `UserPrincipal`；全部缺失返回 `null`。

测试（`UserContextBehaviorTests.cs`，5 新增）：写入→读取全字段往返一致、无用户键返回 null、部分键仅重建可用声明、令牌读取、无令牌返回 null。

## 十二、UseCase 容器解析：`IUseCaseExecutor` 重载

`UseCaseExecutor` 注入 `IServiceProvider`，新增 4 个按类型参数的解析重载（类型参数约束与四类用例家族一一对应）：

```csharp
ExecuteAsync<TUseCase, TInput, TOutput>(input, presenter, ...)     // where TUseCase : IUseCase<,>
ExecuteAsync<TUseCase, TInput>(input, presenter, ...)              // where TUseCase : INonOutputUseCase<>
ExecuteAsync<TUseCase, TOutput>(presenter, ...)                    // where TUseCase : INonInputUseCase<>
ExecuteAsync<TUseCase>(presenter, ...)                             // where TUseCase : IParameterlessUseCase
```

用例经 `GetRequiredService<TUseCase>()` 解析后走既有 `ExecuteCoreAsync` 分发路径。测试改造：既有用例测试改用 `CreateExecutor` 辅助（注入 `IServiceProvider`）。

测试（`UseCaseTests.cs`，5 新增）：四个解析重载正常执行、未注册用例分发错误。

## 十三、关联 ID 透传：`CorrelationIdBehavior`

新增 `Behaviors/CorrelationIdBehavior.cs`，在管道中按优先级回填 `MessageHeaders.CorrelationId` / `RequestTraceId` 到消息元数据：

1. 请求上下文 `TraceIdentifier` / `Request-Id` 请求头；
2. 消息元数据既有 `RequestTraceId` / `CorrelationId`；
3. 信封自身 `CorrelationId`；
4. 全部缺失时生成 `GuidType.SequentialAsString`。

已注册到 `ApplicationModule.cs`（顺序在 `ValidationBehavior`、`UserContextBehavior` 之后，链接口标识与用户上下文一并透传）。

测试（`CorrelationIdBehaviorTests.cs`，6 用例）：TraceIdentifier 回填两键、Request-Id 请求头、无请求上下文时沿信封既有值、既有元数据复用、全缺失时生成新值、委托回调。

## 十四、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误 0 警告 |
| `Euonia.Application.Tests`（`dotnet exec`） | **86/86**（67 → 86，新增 19） |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Osba 131 / Core 78 / Linq 38 / Domain 22 / Bus 22 / Bus.InMemory 10 / Bus.RabbitMq 10 / Pipeline 10 / Caching.Memory 9 / Caching.Runtime 9 / Caching.Default 4 / Mapping.Automapper 3 / Mapping.Mapster 3） |

## 十五、遗留观察（未改动，供后续决策）

- **脱敏覆盖范围**：现仅按参数名/特性掩码，未对复杂对象内部敏感属性（如输入 DTO 里的 `Password` 属性）逐字段脱敏；如需可加「类型级敏感属性声明」支持。
- **`UseCaseExecutor` 生命周期**：容器解析重载使用注入的根 `IServiceProvider`，scoped 用例的解析范围取决于执行器自身的作用域；若需每个请求独立作用域可注入 `IServiceScopeFactory`。
- **`CorrelationIdBehavior` 与 Web 层联动**：`RequestContext.RequestId` 读取 `Request-Id` 请求头；如需遵循 ASP.NET Core 的 `X-Correlation-ID` 惯例，可在 Web 中间件完成头名映射后接入。

---

# 第四部分 实用功能增强（三）

对 Euonia.Application 追加第三批实用功能：对象图脱敏、UseCase 容器 scoped 解析、`BaseApplicationService` 执行器便捷属性。全程先补测试后实现，全仓无回归。

## 十六、对象图脱敏：`SensitiveDataMasker` 落地遗留项

上批遗留的「DTO 内部敏感属性逐字段脱敏」在本批实现。`SensitiveDataAttribute` 目标扩展为 参数/属性/字段/类（`Attributes/SensitiveDataAttribute.cs`）；新增 `Interceptors/SensitiveDataMasker.cs` 提供递归脱敏：

- **成员级**：属性/字段标注 `[SensitiveData]` 时替换为掩码（尊重成员自定义 `Mask`）；
- **类型级**：类型自身标注时整体掩码；`System`/`Microsoft`/`Newtonsoft`/`Castle` 等命名空间视为不透明，不递归展开（避免运行时结构炸日志）；
- **集合/字典**：枚举成员逐项脱敏；循环引用经 `ReferenceEqualityComparer` + 深度上限（8）防护；返回脱敏副本（字典/列表），不修改原对象。

`LoggingInterceptor.GetArguments` 现对每个非关键字命中的参数调用 `SensitiveDataMasker.Mask`，即复杂 DTO 里的敏感成员在序列化前即被替换（`Interceptors/LoggingInterceptor.cs:113`）。

测试：`SensitiveDataMaskerTests.cs`（6 用例：null / 标量透传 / 敏感属性掩码 / 嵌套递归 / 集合逐项 / 循环引用不爆栈）+ `InterceptorTests.cs`（2 用例：DTO 内部掩码、自定义掩码文本）。

## 十七、UseCase 容器 scoped 解析：`IServiceScopeFactory`

上批遗留的「scoped 用例作用域」复核发现：默认 MS DI 容器解析 scoped 服务需经 `IServiceScopeFactory` 每次新建作用域，否则从根容器解析会抛异常或得到错误实例。`UseCaseExecutor` 改为持有 `IServiceScopeFactory`，容器解析重载在**每次执行新建作用域**内 `GetRequiredService<TUseCase>()`，scoped 注册的用例及其依赖每次执行获得独立实例（`UseCase/UseCaseExecutor.cs:23`）。

测试（`UseCaseTests.cs`，2 新增）：scoped 用例每次执行新建实例（`InstancesCreated == 2`）、scoped 依赖正确注入（`ScopedCounter` 非空）。

## 十八、`BaseApplicationService.Executor` 便捷属性

`BaseApplicationService` 新增 `Executor`（懒加载 `IUseCaseExecutor`），派生应用服务可直接调用用例执行（`Services/BaseApplicationService.cs:56`）。

测试（`ServiceRegistrationFeatureTests.cs`，1 新增）：`IExecutorProbeService` 解析到非空执行器；`CreateLoggingProvider` 补注册 `IUseCaseExecutor`。

## 十九、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误 0 警告 |
| `Euonia.Application.Tests`（`dotnet exec`） | **97/97**（86 → 97，新增 11） |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Osba 131 / Core 78 / Linq 38 / Domain 22 / Bus 22 / Bus.InMemory 10 / Bus.RabbitMq 10 / Pipeline 10 / Caching.Memory 9 / Caching.Runtime 9 / Caching.Default 4 / Mapping.Automapper 3 / Mapping.Mapster 3） |

## 二十、遗留观察（未改动，供后续决策）

- **掩码性能**：`SensitiveDataMasker` 每次调用全量反射读取属性值（示例对象图较小）；若用于高吞吐入参日志，可缓存成员反射信息或以表达式树编译属性读取器。
- **不透明命名空间策略**：`System`/`Microsoft` 等整类掩码可能吞掉有诊断价值的框架类型；如需细粒度可从掩码改为「类型名 + 掩码」占位。
- **`TracingInterceptor` 链路 ID 集成**：`CorrelationIdBehavior` 已把标识写入元数据，但追踪日志（`TraceInfo`）尚未携带 CorrelationId；如需按链路聚合可让 `TracingInterceptor` 读取当前请求/元数据后一并输出。

---

# 第五部分 实用功能增强（四）

对 Euonia.Application 追加第四批实用功能：方法结果缓存拦截器、追踪日志链路 ID 集成、脱敏器不透明类型占位 + 反射缓存。全程先补测试后实现，全仓无回归。

## 二十一、方法结果缓存：`[Cache]` + `CacheInterceptor`（落地遗留项）

新增 `Attributes/CacheAttribute.cs`（`AttributeTargets.Method`）与 `Interceptors/CacheInterceptor.cs`，基于 `Euonia.Caching` 的 `ICacheService` 实现方法级结果缓存（`Euonia.Application.csproj` 新增对 `Euonia.Caching` 的 `ProjectReference`；Caching 仅依赖 `Euonia.Core`，无循环依赖）：

- **缓存键**：`Key` 模板支持 `{service}`（服务类型全名）、`{method}`（方法名）与 `{0}`、`{1}`…（按序参数；复杂参数经 `JsonSerializer.Serialize`）；未设置时默认 `{service}.{method}:arg1|arg2`；
- **过期**：`TimeoutSeconds` 大于 0 时写 TTL，否则不设有效期；`IsUtc` 保留以兼容扩展；
- **命中路径**：`TryServeFromCache<T>` 命中直接把缓存值写回 `invocation.ReturnValue`，且**不再执行方法体**：`Task<T>` 返回 `Task.FromResult`、`ValueTask<T>` 返回已完成 `ValueTask`、同步返回原值；
- **写回**：同步方法直接 `AddOrUpdate`；异步方法经 `WriteBackAsync<T>` 在任务 `ContinueWith`（`TaskScheduler.Default`）成功完成后回写，避免阻塞调用线程；
- **退化**：未注册 `ICacheService`、`void`/非泛型 `Task`/`ValueTask` 方法、`null` 结果一律跳过（`null` 不缓存，防止缓存击穿占位）。

测试（新增 `CacheInterceptorTests.cs`，7 用例）：同步/异步命中第 2 次不再执行方法体、不同实参产生不同键、自定义 Key 模板、`{service}.{method}` 占位替换、未注册缓存服务退化为直接执行、`void` 方法不缓存不抛（写回为异步 continuation，测试经轮询等待缓存落盘后再断言）。`FakeCacheService` 为测试项目内最小内存实现，仅覆盖同步成员。

## 二十二、追踪日志链路 ID 集成（落地遗留项）

`TracingInterceptor`（`Interceptors/TracingInterceptor.cs`）在输出 `TraceInfo` 时先读取 `IRequestContextAccessor.Context` 中的 `RequestTraceId`（`X-Request-Trace-Id` 头，缺省回退 `TraceIdentifier`）与 `CorrelationId`（`X-Correlation-ID` 头），拼入 Debug 日志，使同一链路的日志可按标识聚合。

测试（`InterceptorTests.cs`，2 新增）：带 `X-Request-Trace-Id` + `X-Correlation-ID` 时同时输出两个标识；仅 `TraceIdentifier` 时只输出 TraceId。

## 二十三、脱敏器增强：不透明类型占位 + 反射缓存（落地遗留项）

`SensitiveDataMasker`（`Interceptors/SensitiveDataMasker.cs`）两项增强：

- **不透明类型占位**：`System`/`Microsoft`/`Newtonsoft`/`Castle` 等命名空间的类型不再整类替换为 `***`，改为 `类型名:掩码`（如 `Uri:***`），既避免展开庞大的运行时结构，又保留可辨识来源（诊断价值不丢失）；
- **反射缓存**：按类型缓存成员描述符（属性/字段名 + getter + 敏感标识），避免每次掩码全量反射（`ConcurrentDictionary<Type, MemberDescriptor[]>`）。

测试（`SensitiveDataMaskerTests.cs`，3 新增）：框架类型（`System.Uri`）输出类型名占位且不含明文、类型级 `[SensitiveData]` 整体掩码、重复调用结果稳定（缓存路径一致）。原掩码语义（敏感属性/字段、循环引用、深度上限）回归不变。

## 二十四、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误；1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Application.Tests`（`dotnet exec`） | **109/109**（97 → 109，新增 12） |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Osba 131 / Core 78 / Linq 38 / Domain 22 / Bus 22 / Bus.InMemory 10 / Bus.RabbitMq 10 / Pipeline 10 / Caching.Memory 9 / Caching.Runtime 9 / Caching.Default 4 / Mapping.Automapper 3 / Mapping.Mapster 3） |

## 二十五、遗留观察（未改动，供后续决策）

- **缓存失效策略**：`CacheInterceptor` 仅支持按 TTL 过期，暂无主动删除/缓存版号失效；若需可在 `ICacheService` 之上提供版本化键或事件驱动失效。
- **`CacheAttribute` 的 `IsUtc` 字段**：当前仅面向相对 TTL 语义保留，绝对时钟过期（如 `DateTime` 形式）尚未接线。
- **`CorrelationIdBehavior` 与 Web 层联动**（沿用）：`RequestContext.RequestId` 读取 `Request-Id` 请求头；如需遵循 ASP.NET Core 的 `X-Correlation-ID` 惯例，可在 Web 中间件完成头名映射后接入。