# 更新日志

本文件记录 Euonia 各版本的更新内容，格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 `年份.主版本.修订号`（如 `2026.4.3`）。

## [未发布]

<!-- 在此累积下一个版本的变更，发布时改写为正式版本号与日期 -->

## [2026.4.3] - 2026-10-01

本版以**权限体系解耦**为主线：策略引擎从 `Euonia.Osba` 剥离为独立的 `Euonia.Security`；同时完成全模块异常消息的
本地化资源迁移，并修复对象模型的一批并发与异步缺陷。

变更规模：79 个提交，696 个文件，+57,005 / −7,663 行。

### 破坏性变更

升级前请重点关注本节。

#### 权限服务注册方式

`AddBusinessObject` 不再注册任何鉴权服务（`IPermissionChecker`、`IScopeGuard`、权限模型注册表均已移出）。
必须显式调用 `AddPermission`，否则行级数据权限会静默失效。

```csharp
// 变更前
services.AddBusinessObject(typeof(Order).Assembly);
services.AddScoped<IScopeSubjectResolver, MySubjectResolver>();
provider.ValidatePermissionSetup();

// 变更后
services.AddBusinessObject(typeof(Order).Assembly);
services.AddPermission(p =>
{
    p.Scan(typeof(Order).Assembly);
    p.Source(ObjectPermissionCodeSource.Instance);
});
```

#### 已删除的 API

| 删除项 | 替代方案 |
|---|---|
| `ServiceProviderExtensions.ValidatePermissionSetup()` | 首次解析 `IScopeGuard` 时自动执行启动校验 |
| `ServiceCollectionExtensions.AddPermissionModels()` | `AddPermission(p => p.Scan(assemblies))` |
| `ServiceCollectionExtensions.AssertNoPermissionModels()` | `AddPermission(p => p.NoModels())` |
| `PermissionOptions.UseOperations()` | 操作词汇一律由入口规则推导 |
| `EmptyCodeSource`（公开可见） | 收敛为 `internal`，不再作为公开 API |
| `IScopeKeyResolver` / 配置节注册载体 | 并入 `ScopeKeys`，仅保留约定名 |
| `PermissionRule` / `ScopePolicyRule` | 权限只由工厂边界裁决，越权一律 `SecurityException` |
| `ClaimPermissionChecker` | 由宿主自建 `IPermissionChecker` 承接 |
| `UserClaimTypes.Permission`（`"perm"` 声明） | 权限码一律从授权数据实时解析，不再固化进令牌 |
| `AssemblyHelper.GetAllTypes(Assembly)` | 重命名为 `LoadTypes`，并新增批量重载 |
| `IPermissionUserAccessor` | 改用 `ClaimsPrincipal` |

#### 命名空间迁移

`Nerosoft.Euonia.Osba.Permission.*` 下的权限契约与引擎类型整体迁至 `Nerosoft.Euonia.Security`。

#### 行为变更

- **`ReadOnlyObject<T>` 拒绝一切属性写入**：改由 `CanWriteProperty` 返回 `false` 表达只读，越权写入抛
  `SecurityException`（此前为静默绕过）。显式 `BypassRuleChecks` 仍可写，语义统一由 `ObservableObject` 定义。
- **权限失败形态统一为 `SecurityException`**：越权（新增 / 更新 / 删除 / 命令）不再以规则阶段的
  `ValidationException` 暴露；规则通道只做数据校验。
- **操作名与权限码比较统一为 `OrdinalIgnoreCase`**，不再静默丢失大小写不同的规则。

### 新增

#### `Euonia.Security` 权限引擎

新增独立程序集 `Euonia.Security`，仅依赖 `Euonia.Core`，承载策略引擎：
`IScopeGuard`、`IScopeModel`、`IScopeModelBuilder`、`IScopeSubjectResolver`、`ScopeModel`、`ScopePolicyCompiler`、
`ScopePolicySet`、`ScopeModelRegistry`、`CompiledScopePolicy`、`ScopeDecision`、`SubjectPermissionChecker` 等。

该引擎与 `Euonia.Osba` **互不引用，也不需要适配包**：Osba 只面对 `Euonia.Core` 中的权限契约并实现其中「对象状态 → 操作、
来源、工厂边界强制」的一半，引擎实现「策略编译、行级判定」的一半，由宿主用 `AddPermission` 接线。
新增测试项目 `Euonia.Osba.Standalone.Tests` 以引用图作为架构护栏——一旦 Osba 被加回引擎引用即编译失败。

#### 统一注册入口 `AddPermission`

```csharp
services.AddPermission(p =>
{
    p.Scan(typeof(Order).Assembly);                              // 扫描范围（可多次调用，按并集累积）
    p.OnAttributeOrName(BusinessOperation.Read, "Order",
        typeof(FactoryFetchAttribute));                          // 入口规则：打特性或按约定名匹配
    p.OnMethod(BusinessOperation.Read, m => …);                  // 入口规则：任意谓词（逃生舱）
    p.Source(ObjectPermissionCodeSource.Instance);                // 显式来源（与入口规则互斥）
    p.NoOperationCodes();                                        // 断言：本应用没有方法级权限码
    p.NoModels();                                                // 断言：本应用没有任何权限模型
});
```

未调用 `Scan()` 且未断言 `NoModels()` 时，启动期校验会拒绝——空扫描范围会让行级数据权限静默失效。
`PermissionSetup` 改为注册期一次创建的可变单例，不再每次 `RemoveAll` + 重建快照。

#### 异步授权链路

- `IScopeGuard` 新增 `EnsureResolvedAsync()`、`GetSubjectsAsync()`、`RefreshAsync()`；
  同步成员只读已解析快照，绝不隐式等待，冷缓存时 `GetSubjects()` 抛 `InvalidOperationException`。
- `IPermissionChecker` 补充 `IsGrantedAsync` 与预热方法。
- `BusinessObjectFactory` 全部 `*Async` 入口改为先 await 预热、再执行同步判定，避免 sync-over-async 造成线程池饥饿。
- 新增 `AuthorizationWarmup`。

#### 行级策略与维度

- `ScopePolicySet<T>` 支持按操作名寻址：新增 `ForOperation(operation, policy, scopeKey)`；
  原有 `For(code, policy)` 按权限码声明（标识与授予键均为该码）。
- `ScopeModelBuilder<T>` 新增 `MapMany(dimension, selector)`，支持集合 / 子表维度。
- `ScopeDimensions` 新增 `Team`、`Member`、`Region`、`Project` 常量与 `Register(name)`。

#### 下沉至 `Euonia.Core` 的权限契约

`IPermissionChecker`、`IPermissionCodeSource`、`IObjectScopeAuthorizer`、`IObjectOperationResolver`、
`PermissionAttribute`、`BusinessOperation`、`OperationConventions`。

#### 示例应用

`Samples/Euonia.Sample.Webapi` 新增 Project / Team / Repository 三个聚合（含 `TeamMember` 子表）的完整 DDD
权限示例：命令与处理器、EF Core 持久化与规格、Scope 模型与 `AuthorizationStore`、`AuthController` + JWT、
`ApiExceptionMiddleware`。

### 变更

#### 全模块异常消息本地化

32 个模块的内联异常消息全部迁移到资源文件，共 **291 个 resx / 35 个 `Resources.Designer.cs`**，
每模块 9 份文化（neutral 英文 + de / es / fr / ja / ko / zh-CN / zh-HK / zh-TW）。

- 覆盖模块：Application、Bus（Abstract / ActiveMq / Grpc / HealthChecks / Http / InMemory / RabbitMq）、
  Caching（Memory / Redis）、Concurrency（Azure / FileSystem / Redis / ZooKeeper）、Core、Hosting、Linq、
  Mapping（Automapper / Mapster）、Modularity、Osba、Pipeline、Quartz、Repository（EfCore / Mongo）、
  Security、Uow、Validation。
- `resource.props` 新增 `ResourcesNamespace`，用于隔离同 `RootNamespace` 项目
  （`Euonia.Bus` 与 `Euonia.Bus.Abstract` 同为 `Nerosoft.Euonia.Bus`）生成的同名 `Resources` 类型，规避 CS0436。
- 新增资源键 `IDS_PERMISSION_*` / `IDS_VALIDATION_*` / `IDS_DECISION_*` 等，各语言文案同步补齐。
- 新增 `ResourceLocalizationRegressionTests` 护栏：断言英文文案不含 CJK、占位符数量中英一致
  （防止 `string.Format` 运行期抛 `FormatException`）。

#### 其他

- `UserPrincipal` 合并 Windows 与 Cookie 身份验证的 claims 优先逻辑，并为 netstandard2.1 补
  `PriorityValueFinder` 降级分支。
- `AssemblyHelper.LoadTypes` 新增批量程序集重载。
- `AddBusinessObject` 统一注册 `IObjectOperationResolver`（对象状态 → 业务操作），
  任何鉴权实现（引擎或宿主自建）均可直接消费。

### 修复

- **规则绕过改为异步流作用域**：修复并发外溢与嵌套还原缺陷，新增属性规则异步出口，并补齐流语义护栏测试。
- **对象模型并发安全加固**：限制字段撤销历史深度，适配 netstandard2.1，并补齐降级诊断。
- **`ReadOnlyObject<T>` / `ObservableObject` 旧值读取逻辑统一**：收敛为 `FieldManager.GetExistingOrInit`。
- **账户与凭据异常补描述性消息**：`AccountLocked`、`AccountNotFound`、`AccountExpired`、
  `CredentialIncorrect`、`CredentialExpired`、`CredentialNotFound` 的默认构造函数不再返回 BCL 默认值、
  不再向异常管道泄漏类型名。
- **`ReflectionTypeLoadException` 不再零痕迹降级**：逐条写入 `Trace`，避免拼错依赖表现为「扫描什么都没找到」。
- 修复 `BusinessOperation.All` 的数组初始化语法；`ExecuteAsync` 移除多余的 `await`。
- `GlobalUsings` 与 ReSharper 抑制指令清理。

### 测试

- 新增 3 个测试项目：`Euonia.Security.Tests`（含 `Fixtures` / `BrokenFixtures` 两个独立程序集）、
  `Euonia.Osba.Standalone.Tests`、`Euonia.Security.Tests.Fixtures`。
- 新增用例覆盖：集合 / 子表维度、大小写忽略、锁序与注入解析护栏、授权预热、异步快照与声明读取、
  快照回写回归、管道生命周期回归、用户主体回归。
- `ValidatePermissionSetup_*` 系列用例重命名为 `GuardResolution_*`。

### 文档

- 新增 `Source/Euonia.Security/README.md`、`Source/Euonia.Security/DESIGN.md`。
- 新增 `Source/Euonia.Osba/PERMISSION.md`、`PERMISSION-SAMPLE.md`、`PERMISSION-DESIGN.md`。
- 新增设计评审文档 `docs/Euonia.Osba-Security-Design-Review-Report.md`、
  `docs/Euonia.Security-Registration-Redesign-Proposal.md`、`docs/REFACTOR-CHECKLIST.md`。
- 删除 12 份历史修复报告（`docs/Euonia.*-Report.md`）。
- `README.md` / `README.en.md` 同步更新权限章节与包清单（新增 `Euonia.Security`）。

## [2026.4.2] - 2026-09-27

Actuator 支持任意业务对象类型，小幅重构。

变更规模：4 个提交，12 个文件，+251 / −41 行。

### 破坏性变更

- `ActuatorBase<TTarget>.WithoutRuleChecks()` 重命名为 **`BypassRuleChecks()`**，与
  `Rules.SuppressRuleChecking()` 的术语统一。
- `CreateActuator<TTarget>` 的类型约束由 `EditableObject<TTarget>` 放宽为 **`BusinessObject<TTarget>`**，
  基类由 `EditableActuator<TTarget>` 改为直接继承 `ActuatorBase<TTarget>`。
- `ActuatorBuilderExtensions.Create<TTarget>` 随之放宽约束：可编辑对象创建后标记新增并保存（插入语义），
  只读对象、命令对象等不可持久化类型仅构造实例、不落库。

### 变更

- Actuator 体系支持任意业务对象类型，`SAMPLE.md` 与 `Permission/{README,DESIGN}.md` 同步更新。

### 测试

- 新增 `CreateActuatorTests`，扩充 `ActuatorRuleTests`。

## [2026.4.1] - 2026-09-24

消息总线补齐延迟派发与投递属性，缓存拦截器异步化，规则检查抽离为独立作用域。

变更规模：7 个提交，73 个文件，+5,435 / −384 行。

### 新增

- **消息投递属性** `MessageProperties`：`GetQueue` / `SetQueue` / `GetPriority` / `SetPriority` 扩展方法，
  并在 `MessageContext` 中落地；`ActiveMqDelivery`、`RabbitMqDelivery` 对应新增。
- **消息总线自动加载程序集**：无需手工登记程序集即可发现处理器。
- **延迟派发**：`DispatchBuilder` 支持延迟投递。
- **规则作用域** `RuleScope`（internal）与 `ObjectRuleGuard`（internal）：把「本次操作跑哪些规则、是否跳过」
  从 `Rules` 中抽出，属性规则触发时机可独立测试。

### 变更

- `CacheInterceptor` / `CacheEvictionInterceptor`：缓存拦截器与失效逻辑适配异步方法的缓存写入。
- `MessageHandlerFinder`、`StrategicDispatcher`、`ServiceActivator`、`DefaultConfigurator` 等重构，
  处理器缓存与约定缓存的失效路径得到覆盖。
- `Bus.HealthChecks` 模块补充 README 文档。

### 测试

- 新增测试项目 `Euonia.Bus.ActiveMq.Tests`。
- 新增 `DelayedDispatchTests`、`DeliveryPropertiesTests`、`ConventionCacheInvalidationTests`、
  `DispatcherCacheInvalidationTests`、`TransportStrategyCacheInvalidationTests`、
  `AutoLoadAssembliesTests`、`ChannelResolverRegistrationTests`、`ActiveMqDeliveryTests`、
  `RabbitMqDeliveryTests`、`RulePathTests`、`PropertyRuleTriggerTests`、`ScopeRuleInjectionTests`。

## [2026.4.0] - 2026-09-22

大版本。引入 **Inbox/Outbox** 与**远程调用**两大能力、完整的**横切治理套件**（缓存 / 重试 / 熔断 / 幂等 / 计时），
以及 `Euonia.Osba` 的**权限体系**（操作权限 + 数据权限）。

变更规模：63 个提交，415 个文件，+32,620 / −2,595 行。

### 破坏性变更

| 变更 | 说明 |
|---|---|
| `Euonia.Grpc` 项目移除 | 协议定义与通用 gRPC 工具并入 `Euonia.Bus.Grpc` |
| `IBus.CallAsync` 扩展 | 新增超时与取消支持，签名变更 |
| `PermissionRequirementAttribute` → `PermissionAttribute` | 权限声明特性重命名 |
| `IMessageStore` / `IMessageTracking` 删除 | 由 `IInboxStore` / `IOutboxStore` 取代 |

### 新增

#### 消息可靠性（Inbox / Outbox）

`Euonia.Bus.Abstract/Consistency/` 新增 `IInboxStore`、`IOutboxStore`、`IDeadLetterService`、`IDeadLetterStore`、
`InboxEntry`、`OutboxEntry`、`DeadLetterEntry`、`InboxHandler`、`OutboxTransport`、`StoreEntryCache`；
`Euonia.Bus/Reliability/` 提供 `InboxDispatcher`、`OutboxDispatcher`、`InboxOptions`、`OutboxOptions`、
`DeadLetterService` 及内存实现。

#### 远程调用

- `Euonia.Bus/Remote/`：`RemoteReply<T>`、`RemoteError`、`RemoteReceiver`。
- 新增 `Euonia.Bus.Http` 与 `Euonia.Bus.Grpc` 传输适配器，支持请求-响应式远程调用。
- **gRPC 泛化调用**：客户端运行时构造 `Method<GrpcRequest, GrpcResponse>` 经 `CallInvoker` 执行，
  服务名 / 方法名由 `GrpcBusOptions` 动态指定，移除客户端桩代码生成。
- `Euonia.Bus.HealthChecks` 新增健康检查模块。

#### 横切治理（`Euonia.Application`）

| 特性 / 行为 | 说明 |
|---|---|
| `[Cache]` / `[CacheEvict]` | 方法结果缓存；缓存组失效、绝对过期 |
| `[Retry]` / `[RetryBackoffMode]` | 重试，支持 `ValueTask` 重试、退避策略与抖动 |
| `[CircuitBreaker]` | 熔断，含半开状态下单飞探测；新增 `CircuitBreakerOpenException` |
| `[Idempotent]` | 幂等控制，重复调用去重，支持分布式锁工厂 |
| `[Timing]` | 方法耗时日志与链路 ID 追踪 |
| `[SensitiveData]` | 敏感数据脱敏（`SensitiveDataMasker`） |
| `CorrelationIdBehavior` | 支持从请求头 `X-Correlation-ID` 取关联标识 |

- 新增 `IUseCaseExecutor` / `UseCaseExecutor`（支持容器解析）。
- 新增 `ICacheGroupManager` / `CacheGroupManager`。

#### 管道

`PipelineBase<TRequest, TResponse>` 新增 `RunAsync(context, accumulate)` 重载，支持直接使用累积委托作为终结点，
减少线程池调度开销；`Pipeline` 统一入口提供 `RunAsync` / `Run` 同步与异步执行。

#### 缓存

- `Euonia.Caching` 抽象收敛，`ICacheService` 扩展。
- 新增 `MemoryCacheStoreRegistry`、`RedisConfigurations`，完善 Redis 缓存后端。
- 缓存句柄补齐 `Dispose`，修复句柄释放不完整导致的资源泄漏。

#### 权限体系（`Euonia.Osba`，全新）

- 操作权限：`PermissionAttribute`、`IPermissionChecker`、`ClaimPermissionChecker`、`SubjectPermissionChecker`、
  `BusinessOperation`、`ObjectAuthorization`。
- 数据权限：`ScopeModel`、`ScopeModelBuilder`、`ScopePolicy` / `ScopePolicyCompiler` / `ScopePolicySet`、
  `IScopeGuard`、`ScopeGuard`、`ScopeSubjectSet`、`ScopeKeyResolver`、`ScopeAuthorization`、`ScopeOperationMap` 等 30+ 类型。
- 启动期校验：检测死策略与多重策略键解析错误；强制要求接入 `BusinessContext`，无法判定时抛
  `InvalidOperationException`。
- `Create` 操作不做数据范围判定。

#### 其他

- 新增 `GuidVersion7ValueGenerator`（EF Core）。
- 新增 `IValueObject` 值对象语义（`Euonia.Domain`）。
- `EnumHelper` 补充枚举本地化描述与显示名获取方法。
- 新增 `IServiceCollection.Extensions`，简化服务注册与配置。
- `BusinessContext` / `IAuditable` 增强；`BusinessObject` / `EditableObject` 新增
  `AcceptChanges`、`MarkAsClean`。
- gRPC 与 HTTP 传输实现补充完整示例。

### 修复

- **`IsValid` 判定方向完全相反**（`ValidatableObject`）：此前写作 `Errors.Count > 0`，
  导致合法对象被判定为无效并抛出空的 `ValidationException`，非法对象反而被放行——校验同时过度触发与失效。
- 重试机制竞态条件：确保仅返回失败的消息记录。
- 在途授权解析不会被陈旧快照回滚。
- `InMemoryRecipientRegistrar` 改为管理收件人实例，避免过早被垃圾回收。
- 逻辑删除处理优化，更新审计信息。
- `[NotNull]` 参数检查：确保空参抛出异常。
- 缓存句柄释放不完整导致的资源泄漏（补 `Dispose`）。
- 测试对象不再跨测试泄漏规则。
- `ObjectId` 改为按数值比较与哈希。

### 文档

- 新增中英文 `README`（仓库根、`Euonia.Core`）。
- 新增 `Source/Euonia.Bus/SAMPLE.md`、`Source/Euonia.Osba/SAMPLE.md`、
  `Source/Euonia.Osba/Permission/{README,DESIGN}.md` 及权限控制完整示例文档。
- 新增 `Samples/Euonia.Sample.Webapi` 的 Osba 综合示例（业务对象、工厂、状态机、规则、执行器、权限体系）。
- 新增缓存 / Core / Pipeline 修复报告。
- 大量 XML 注释完善。

### 测试

- 新增 5 个测试项目：`Euonia.Bus.Grpc.Tests`、`Euonia.Bus.Http.Tests`、`Euonia.Bus.HealthChecks.Tests`、
  `Euonia.Caching.Redis.Tests`、`Euonia.Domain.Tests`。
- 新增 `ScopePolicy` 单元测试、缓存分组失效测试、缓存类型无关性测试、Redis 缓存服务测试、
  重试组合测试、熔断半开探测测试、UoW 作用域回归测试等。

[未发布]: https://github.com/Nerosoft/Euonia/compare/v2026.4.3...HEAD
[2026.4.0]: https://github.com/Nerosoft/Euonia/compare/v2026.3.16...v2026.4.0
[2026.4.1]: https://github.com/Nerosoft/Euonia/compare/v2026.4.0...v2026.4.1
[2026.4.2]: https://github.com/Nerosoft/Euonia/compare/v2026.4.1...v2026.4.2
[2026.4.3]: https://github.com/Nerosoft/Euonia/compare/v2026.4.2...v2026.4.3
