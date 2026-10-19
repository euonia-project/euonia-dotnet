# 更新日志

本文件记录 Euonia 各版本的更新内容，格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 `年份.主版本.修订号`（如 `2026.4.3`）。

## [未发布]

<!-- 在此累积下一个版本的变更，发布时改写为正式版本号与日期 -->

## [2026.4.5] - 2026-10-19

修复后台消息处理继承已释放的发布方作用域、以及跨进程消息处理丢失请求上下文的缺陷。
变更规模：13 个源文件（含 3 个新增），新增回归测试 8 个。

### 修复

- **消息处理不再继承已释放的发布方作用域**（`Euonia.Bus` / `Euonia.Modularity`）：
  `IServiceAccessor` 以 `AsyncLocal` 保存当前作用域的服务提供程序，由宿主中间件在请求开始时
  写入请求作用域。后台泵、broker 消费者以及发件箱/收件箱重试可能在发布方作用域释放后才执行，
  继承到的提供程序已失效；处理程序内任何经由该访问器的服务解析——包括订阅者发送后续命令时
  `MessageBus`/`RequestContextAccessor` 读取请求上下文——都会抛出 `ObjectDisposedException`。
  现在 `DefaultHandlerContext.HandleAsync`/`ReplayDeadLetterAsync`、`InboxDispatcher.ExecuteAsync`、
  `OutboxDispatcher.RedeliverCoreAsync` 统一将访问器绑定到本次执行使用的作用域，执行结束后恢复原绑定。
- **进程内单播/请求复用调用方作用域**：内存传输的 `SendAsync`/`CallAsync`（含处理器内继续发送的嵌套命令）
  由调用方等待处理完成，发布方作用域在整个处理期间存活，因此处理程序直接复用该作用域，
  不创建新作用域；处理器与调用方共享 scoped 服务（如 `DbContext`/事务）。
  若捕获的作用域已释放（异常的后台调用方），探测到后回退为新作用域并记录警告。
  多播发布（`PublishAsync`）仍是 fire-and-forget，使用消息专用作用域。
- **进程内传输按消息保留发布方上下文**：内存传输器在发布时复制发布方请求的完整信息
  （连接信息、用户、授权、请求头、方法、协议、主机、路径、取消令牌、跟踪标识等；
  `RequestServices` 除外）为 `PublisherContextSnapshot`，随消息一起入队；处理时按消息恢复，
  并把 `RequestServices` 重新绑定到本次处理作用域。修复了后台泵只继承启动时第一条消息的上下文、
  导致同一泵中的后续消息沿用前一条消息身份的问题；发布方没有上下文时也会清除继承值，
  仅在信封携带身份时（如发件箱重投）回退到信封。
- **跨进程消息处理从信封恢复请求上下文**：网络传输（RabbitMQ、ActiveMQ、HTTP、gRPC）的发布与处理
  不属于同一个请求，处理器流中不存在发布方的请求上下文。当处理流尚无上下文时，
  `DefaultHandlerContext.HandleAsync`/`ReplayDeadLetterAsync` 与 `InboxDispatcher.ExecuteAsync`
  使用信封携带的 `User`/`Authorization`/`RequestTraceId` 创建 `RequestContext`，
  使处理器内的 `IRequestContextAccessor`、跟踪行为以及处理器继续发送的消息获得一致的用户身份与跟踪标识；
  HTTP 入站请求或进程内发布方的原有上下文不会被覆盖。死信重放与收件箱重执行则以**原消息身份**
  覆盖触发方（如管理请求）的上下文，避免重放后的嵌套消息沿用管理用户身份。
- **后台调度器不再继承创建时的请求上下文**：`InboxDispatcher`/`OutboxDispatcher` 的定时任务
  在 `ExecutionContext.SuppressFlow()` 下启动，避免解析到创建定时器时所在的请求作用域。
- **`IServiceAccessor.TryGetServiceProvider`**：读取显式绑定而不触发兜底作用域的创建，
  避免后台消息处理隐式创建并丢弃无人释放的兜底作用域。
- **`DefaultRequestContextAccessor` 默认注册**：模块化应用基类以 Scoped 生命周期注册该类型，
  使非 Web 宿主（消息消费者、后台工作者）也能使用默认请求上下文存储。
- **委托访问器回退**：`RequestContextAccessor` 在委托访问器返回 `null` 时回退到默认访问器中
  由消息信封恢复的上下文；`HostingModule` 的委托在请求流之外（无 `HttpContext`）返回 `null`
  而不是抛出空引用异常。

### 行为说明

- 消息处理程序与处理程序内部继续发出的消息，经由 `IServiceAccessor` 的环境解析
  （如 `RequestContextAccessor.Context`）与处理程序的构造注入使用同一个作用域：
  进程内 `SendAsync`/`CallAsync` 使用调用方作用域，多播发布、网络传输与后台重试使用消息专用作用域。
  发布方请求的作用域在后台处理开始时通常已释放，此前继承到的只是失效引用。
  需要发布方请求信息时：网络传输可从 `MessageEnvelope` 携带的 `User`/`RequestTraceId` 获取；
  内存传输则按消息保留发布方请求的完整信息（`RequestServices` 除外，始终指向实际处理作用域）。
- 处理器内启动的 fire-and-forget 任务会捕获本次处理作用域，并在处理器返回后随作用域释放；
  需要延后执行的后台工作应自行创建作用域。

### 测试

- 新增 `MessageScopeLifetimeTests`：复现"请求作用域内发布事件 → 请求结束 → 后台泵处理 →
  订阅者解析服务并发送嵌套命令"的完整链路，验证处理程序不再解析到已释放的发布方作用域。
- 新增 `AmbientServiceProviderBindingTests`：分别覆盖发件箱重投递、收件箱重执行与死信重放
  在继承已释放提供程序时仍使用本次执行作用域的回归场景。
- 新增 `MessageRequestContextTests`：验证内存传输中同一后台泵处理多条消息时各自保留自己的
  发布方上下文（先到的消息阻塞泵、后续消息在泵运行期间入队），以及无发布方上下文时
  从信封恢复身份、授权与跟踪标识；信封恢复路径同时覆盖默认访问器与委托访问器两种模式。

## [2026.4.4] - 2026-10-19

对 32 个源项目完成缺陷扫描，共修复 60 余处可复现缺陷；无新功能主线。
变更规模：21 个提交，180 个文件，+3,100 余 / −500 余行。

### 概要

最值得关注的变化：

- **消息不再静默丢失**：发布与 outbox 重投递失败会正确重试；inbox 死信可真正重放；
  进程崩溃遗留的 `Pending` 发件箱记录会被自动回收并重试。
- **多播处理器真正扇形执行**：同一 `IHandler<T,R>` 的多个实现都会收到消息；
  升级时注意 inbox/死信记录中的处理器名已改为具体实现类型。
- **事务边界生效**：`[UnitOfWork(IsTransactional = true)]` 现在会开启真实事务并在完成后提交、
  异常时回滚。
- **权限判定收紧**：派生类用 `new` 遮蔽基类工厂方法不再绕过 `[Permission]`；
  缺少 `BusinessContext` 时 fail-closed。
- **传输与缓存可靠性**：RabbitMQ 持久化、毒消息 `nack` 与自动恢复；ActiveMQ 断线重连；
  Redis 空值、过期与 region 索引修复。
- **一批影响正确性的基础缺陷**：ULID 时间戳编码、分页整数溢出、嵌套属性表达式、
  `ShortUniqueId` 边界、管道优先级与执行顺序。
- **升级必读**：接口处理器在持久化 inbox/死信中的名称由接口全名改为具体实现全名，旧记录需要迁移。

### 破坏性变更

升级前请重点关注本节。

#### 接口处理器与收件箱记录名

- `ChannelHandler` 新增 `ImplementationType` 参数。同一 `IHandler<T,R>` 的多个实现现在按具体类型分别注册，
  多播通道会依次执行全部实现（此前第二个实现会被去重丢弃，或只能解析到 DI 中最后注册的实现）。
- 接口处理器在收件箱/死信中的名称由**接口全名**改为**具体实现全名**。
  升级后旧持久化 inbox / 死信记录中的处理器名可能无法匹配，需要迁移或重新登记。

#### `BypassRuleChecks` 管理器 API

`BypassRuleChecksObject.GetManager(...)` 与 `RefCount` 已删除。`BypassRuleChecks` 每次访问返回独立的
流级作用域对象，修复并发异步流共享引用计数导致的绕过状态失效；调用方只需保持 `using` 用法不变。

#### 事务性工作单元

`IsTransactional = true` 的 `[UnitOfWork]` 现在会真正开启数据库事务，并在 `CompleteAsync` 时提交、
未完成释放时回滚。此前该标志不生效，每次 `SaveChanges` 都自动提交；升级后中途异常将按事务语义回滚。

#### MongoDB

- `AssignIdOnInsert` 改为 `true`：模型配置中的 Id 生成器恢复生效；手动赋值的 Id 不受影响。
- `ModelProfileContainer` 改为按数据上下文类型缓存，避免第二个上下文的 `OnModelCreating` 被忽略。

#### 授权

`BusinessObjectFactory.SaveAsync` / `ExecuteAsync(target)` 按**工厂使用的静态类型**收集权限要求。
派生类型用 `new` 遮蔽基类工厂方法时，基类方法上的 `[Permission]` 现在会生效（此前可被绕过）。

#### 管道与缓存

- 静态 `Pipeline.RunAsync` 的行为执行顺序与 `PipelineBase` 一致（按枚举顺序，从外到内）；
  同步重载不再重复调用下游委托。
- `MemoryCacheOptions.Expires` / `RedisCacheOptions.Expires` / `RuntimeCacheOptions.Expires`
  改为绝对过期并真正生效（此前因 `CacheExpirationMode.Default` 被忽略）。
- `RuntimeCacheManager` 对默认实例名不再传入 options，修复 `"default"` 实例首次操作抛异常。

#### 传输层

- RabbitMQ 不再在连接关闭时销毁并重建自动恢复连接；连接、通道与消费者交由客户端自动恢复。
- RabbitMQ `user_id` 不再使用终端用户身份（避免 broker 以 `PRECONDITION_FAILED` 关闭信道）。
- ActiveMQ 重试仅覆盖连接级异常（`SocketException` / `IOException` / `NMSConnectionException`），
  超时与取消不再重试，降低重复发送风险。

### 新增

- `IOutboxStore.GetPendingMessages(cutoff)`（默认空实现）：调度器据此回收进程崩溃/取消后遗留的
  `Pending` 发件箱记录，避免消息永远不重试也不清理。
- `IRepositoryContext.BeginTransactionAsync`（默认实现）：EF Core 与 MongoDB 分别提供事务开启实现。
- `InMemoryMessengerProvider`：每个服务容器使用独立的一对信使，避免进程级静态 Messenger
  在多容器之间串消息。
- `EnvelopeTypeResolver`：按信封声明的泛型类型（而非负载运行时类型）执行 outbox 重投递。
- 新增测试项目 `Euonia.Repository.Tests`。

### 修复

#### 消息总线与可靠性

- 发布与 outbox 重投递的异常不再被 `ContinueWith(_ => Unit.Value)` 吞掉，失败会正确标记并重试。
- 收件箱死信重放现在绕过 inbox 去重、只执行目标处理器，并在成功后标记为 `Success`。
- 同一接口的多个处理器实现可正确多播扇出，不再被去重或只解析最后一个实现。
- 同类型 Lambda 处理器的收件箱记录名冲突：重名时追加稳定序号。
- `CallOptions.Delay` / `WithDelay` 对请求-响应调用生效（此前被忽略）。
- 自动装配（`AutoLoadAssemblies`）使用用户配置的通道解析器。
- System.Text.Json 序列化接口声明类型的负载时按运行时类型写入 `payload`，
  修复 `CallAsync(IRequest<T>)` 经 HTTP/gRPC 丢失请求字段。
- 超出 `MaxRetryAttempts` 的死信、Pending 回收与重试状态现在保持一致。

#### 传输层

- RabbitMQ：消息持久化（`DeliveryMode`）与 `Mandatory` 配置生效；反序列化失败的毒消息会 `nack`，
  处理失败且无回复通道时不再错误 `ACK`；请求发布与回复订阅分开重试，避免重复发布。
- ActiveMQ：断线后 `IsConnected` 正确复位并重建连接；取消不再触发重试。
- InMemory：传输器与接收者注册器共享容器级信使，避免跨容器投递和重复执行。

#### 缓存

- Redis FireAndForget 正确识别 `RedisResult.IsNull`，region 索引不再丢失。
- `TryUpdate` / `AddOrUpdate` 在 `FactoryReturnedNull` 且不抛错时返回而非落空异常。
- 反射构造支持带默认值的可选参数，`WithRuntimeCacheHandle` 无 options 重载恢复可用。
- Redis 背板三参重载补齐连接字符串参数。
- 空字符串/空字节数组不再被当作“不存在”。
- `GetOrAdd` / `GetOrAddAsync` 与 `TryGet` 一致地检查 token 失效。
- 过期扫描只删除仍指向快照实例的条目，避免误删同键新值。
- `TypeCache` 在解析时快照解析器列表，避免并发注册导致“集合已修改”。

#### 并发与后台任务

- ZooKeeper 池的簿记续延不再使用调用方取消令牌，失败的连接不会永久滞留。
- `LeaseMonitor` 的监控任务与 CTS 释放续延不再被取消令牌跳过。
- Azure 删除锁 Blob 时忽略 `LeaseIdMissing` 竞争；同步路径改用同步 API，避免同步上下文死锁。
- FileSystem 目录创建同时重试瞬时 `IOException`。
- Quartz 的 `StartDelay` / `WaitForJobsToComplete` / `AwaitApplicationStarted` 配置生效。

#### Core / LINQ / Pipeline / Validation

- ULID 编码按标准 130 位布局重写，时间戳与字典序恢复正确。
- `ShortUniqueId` 修复守卫字符输入的索引越界，以及小字符集编码缓冲区溢出。
- `AsyncManualResetEvent.Wait(pre-canceled)` 正确抛出取消异常。
- 管道累积终结点不再按优先级混入组件表，正优先级组件不会被跳过。
- 嵌套属性表达式（`x => x.Owner.Name`）保留完整访问链。
- 直接实现 `IValidator<T>` 的验证器不再在扫描时抛 `NullReferenceException`。
- `QueryHandler`：`AddCriteria(null)` 立即失败；排序在过滤之后执行；`GetCount` 不再应用排序。
- `RepositoryExtensions.Paginate` 使用 `long` 计算偏移，修复 `page * size` 整数溢出。

#### 应用、仓储与宿主

- `UseCaseExecutor` 的 DI 作用域覆盖用例完整执行过程，修复 scoped 依赖（如 DbContext）在用例执行前被释放。
- Mongo 更新操作从 `$set` 中移除不可变的 `_id`，批量更新用序列化后的 `_id` 匹配。
- `UseCulture` 正确解析带权重的 `Accept-Language`，设置并还原 `CurrentCulture` / `CurrentUICulture`。
- `DefaultAutomaticRegistration` 的可赋值性判断方向修正，显式服务类型不再导致启动失败。
- Hosting 将框架自身的 `ValidationException` 映射为 400 并返回字段明细。
- 幂等键包含服务标识，避免不同服务的同名方法互相误判。
- `[UnitOfWork]` 支持 `ValueTask` / `ValueTask<T>`，不再提前提交。
- `UnitOfWork.Disposed` 单次释放只触发一次，且终结器线程不再触发托管事件。

#### 诊断与本地化

- 处理程序发现、无接收方与传输失败等异常/日志补充处理程序类型、方法名、消息类型、通道与消息 ID 等上下文。
- 验证消息、连接字符串与数据库提供程序错误迁移到 `Resources.*.resx`，补齐 8 个文化的本地化文案。

#### Osba / Security / Mapping

- 无 `BusinessContext` 的手工构造对象仍使用宿主注册的权限来源（无上下文时 fail-closed）。
- `ScopeGuard` 在失效阶段推进代数，避免旧授权快照写回缓存。
- `FieldData.TrimOldest` 不再反转撤销历史。
- 工厂方法的 `params object[]` 回退正确打包实参。
- 授权失败时不再 `FinalizeInstance` 一个从未初始化的对象。
- Mapster 键控工厂改为单例安全的 `ServiceMapper` 构造，`ValidateScopes` 下可正常启动。

### 测试

- 全量回归：18 个测试套件、912 项测试通过；`Euonia.slnx` 与 `Euonia.Test.slnx` 构建 0 errors。
- 新增覆盖：接口处理器扇出、收件箱死信重放、Pending 发件箱回收、基类类型 outbox 重投递、
  ULID、ShortUniqueId 边界、管道优先级与静态门面、ValueTask UoW、隐藏基类权限、并发绕过、
  Mapster 作用域校验、分页溢出等。
- 需要外部服务的 Redis（配置端口 6399）、RabbitMQ / ActiveMQ 集成测试未在本次环境运行。

### 文档

- 补充 `ObjectAuthorization`、`ModelProfileContainer`、`BypassRuleChecksObject` 等新增/变更成员的
  XML 注释，并消除空文档标签。

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

[未发布]: https://github.com/Nerosoft/Euonia/compare/v2026.4.4...HEAD
[2026.4.0]: https://github.com/Nerosoft/Euonia/compare/v2026.3.16...v2026.4.0
[2026.4.1]: https://github.com/Nerosoft/Euonia/compare/v2026.4.0...v2026.4.1
[2026.4.2]: https://github.com/Nerosoft/Euonia/compare/v2026.4.1...v2026.4.2
[2026.4.3]: https://github.com/Nerosoft/Euonia/compare/v2026.4.2...v2026.4.3
[2026.4.4]: https://github.com/Nerosoft/Euonia/compare/v2026.4.3...v2026.4.4
