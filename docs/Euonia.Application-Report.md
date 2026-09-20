# Euonia.Application 修复与测试补全报告

## 概述
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