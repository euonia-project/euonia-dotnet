# Euonia.Application API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Application` 提供用例、应用服务基类与 Castle 拦截器，把日志、校验、授权、缓存、锁、重试、熔断等横切逻辑从业务代码中剥离。

- **包名**：`Euonia.Application`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Application`（`Register<TService>()` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Application/README.md](../../../Source/Euonia.Application/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `ApplicationModule` | 注册全部拦截器、管道行为、presenter 与用例执行器 | 通过 `[DependsOn]` 引入 |
| `ServiceContextBase` | 应用服务上下文基类，默认开启自动注册 | `Assembly` 默认上下文类型程序集 |
| `Register<TContext>()` | 注册上下文并扫描应用服务/管道行为 | 不注册用例 |
| `IApplicationService` | 应用服务标记接口 | 继承 `IHasLazyServiceProvider` |
| `BaseApplicationService` | 应用服务基类，懒加载总线、用户、日志、执行器 | `Bus`、`User`、`RequestContext`、`Logger`、`Executor` |
| `IUseCase<TInput, TOutput>` | 类型化用例契约 | 另有 `INonInputUseCase<T>`、`INonOutputUseCase<T>`、`IParameterlessUseCase` |
| `IUseCaseExecutor` | 执行用例并分发到 presenter | 容器重载每次创建新作用域 |
| `DefaultUseCasePresenter<TOutput>` | 默认结果呈现器 | `OnSucceed`、`OnFailed`、`OnCanceled`、`Output` |
| `[Cache(Key, TimeoutSeconds, Groups)]` | 缓存有返回值的方法结果 | 无 `ICacheService` 时放行 |
| `[CacheEvict(params string[] groups)]` | 按组失效缓存 | `Mode` 默认 `After` |
| `[Retry(MaxRetries, DelayMs, Backoff)]` | 失败重试 | `MaxRetries` 默认 3；`Backoff` 默认 `Fixed` |
| `[CircuitBreaker(MaxFailures, ResetTimeoutSeconds, SuccessThreshold)]` | 连续失败后短路 | 默认 5 次 / 30 秒 / 1 次成功 |
| `[DistributedLock(token)]` / `[SemaphoreLock(token, maximumCount)]` | 分布式互斥 / 进程内信号量 | 令牌支持 `{parameter}` 占位符 |
| `[Idempotent(Key, TimeoutSeconds)]` | 幂等窗口内去重 | `TimeoutSeconds` 默认 60 |
| `[Timing(ThresholdMs, WarningThresholdMs)]` | 记录慢调用 | 默认 1000 / 5000 毫秒 |
| `IOrderedInterceptor` | 控制拦截器顺序 | `Order` 数字小的在外层 |
| `ServiceContextBase.AutoRegisterApplicationService` | 是否扫描应用服务 | 默认 `true` |
| `ServiceContextBase.ApplicationServiceLifetime` | 应用服务代理生命周期 | 默认 `Scoped` |
| `ServiceContextBase.ApplicationServiceTypeFilter` | 自动注册类型筛选器 | 默认 `null` |

### 配置说明

`Euonia.Application` 没有配置节或 `*Options`；拦截器参数由特性属性配置，自动注册由 `ServiceContextBase` 的虚拟属性控制，因此本页不提供 appsettings 示例。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整拦截器、用例与代理边界](../../../Source/Euonia.Application/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 模块与自动注册
- [Euonia.Pipeline 用户手册](../Euonia.Pipeline/API.md) —— 管道行为
- [Euonia.Validation 用户手册](../Euonia.Validation/API.md) —— 校验拦截器与异常
- [Euonia.Hosting 用户手册](../Euonia.Hosting/API.md) —— Web 宿主中的代理
- [仓库根 README](../../../README.md)
