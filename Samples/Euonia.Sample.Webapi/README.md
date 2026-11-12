# Euonia.Sample.Webapi

Euonia 全栈示例服务：在一个可运行的 ASP.NET Core 应用中串起模块化宿主、消息总线、EF Core 仓储、权限、审计与多租户适配。

- **目标框架**：`net11.0`（`Samples/common.props`）
- **根命名空间**：`Nerosoft.Euonia.Sample`
- **入口**：`Program.Main` → `HostUtility.Run<Startup>(args, HostBuilderOptionsAction)`
- **运行方式**：`dotnet run --project Samples/Euonia.Sample.Webapi`
- **默认地址**：`http://localhost:5240`，Swagger 为 `http://localhost:5240/swagger`
- **NuGet 包**：无（`IsPackable=false`，只作为源码示例）

## 快速开始

```bash
# 仓库根目录执行
dotnet run --project Samples/Euonia.Sample.Webapi

# 打开 Swagger
open http://localhost:5240/swagger
```

首次启动会创建 SQLite 库文件 `Samples/Euonia.Sample.Webapi/euonia-sample.db`，并写入内置管理员（用户名 `admin`）。

## 外部依赖

| 依赖 | 是否必需 | 说明 |
|------|----------|------|
| .NET SDK（支持 `net11.0`） | 必需 | 构建与运行 |
| SQLite | 内置 | 数据库文件随应用创建工作目录，无需安装服务 |
| RabbitMQ Broker | 默认必需 | `appsettings.json` 默认 `Euonia:Bus:RabbitMq:Enabled=true`，连接 `amqp://guest:guest@127.0.0.1:5672/` |

本地没有 RabbitMQ 时，把开关关掉再启动（两种写法等价）：

```bash
Euonia__Bus__RabbitMq__Enabled=false dotnet run --project Samples/Euonia.Sample.Webapi
# 或
EUONIA_BUS_RABBITMQ_ENABLED=false dotnet run --project Samples/Euonia.Sample.Webapi
```

## 主要功能

- **模块化宿主**：`HostModuleContext` 以 `[DependsOn]` 组织 Facade / Domain / Persist 三层模块，`Startup` 只做 `AddModularityApplication<HostModuleContext>(Configuration)` 与管道装配。
- **消息总线**：`FacadeServiceModule` 绑定 `Euonia:Bus` 节，注册 `InMemory` 与 `RabbitMq` 两套传输策略，并通过 `RegisterChannel` / `SetConvention` / `SetStrategy` 声明通道约定。
- **EF Core 仓储**：`PersistServiceModule` 按连接串前缀选择数据库提供程序（`sqlite` / `sqlserver` / `inmemory`），首次初始化时 `EnsureCreated` 并写入种子数据。
- **权限体系**：`BusinessServiceModule` 调用 `AddBusinessObject` 与 `AddPermission`，扫描 `ProjectScopeModel` / `RepositoryScopeModel` / `TeamScopeModel` 等数据权限模型。
- **平台能力**：`Startup` 中接入 `AddTenancyAspNetCore` + `AddInMemoryTenantStore`、`AddAuditing` + `AddInMemoryAuditingStore`、`AddEuoniaTelemetry`。
- **统一错误输出**：`ApiExceptionMiddleware` 位于管道最外层，输出 `{ code, error, details }`。

示例控制器：`Auth`、`User`、`Team`、`Project`、`Repository`、`Permission`、`Bus`、`Home`，另有 `/health` 健康检查。

## 核心类型

| 类型 | 作用 |
|------|------|
| `Program` | 配置 Serilog 与 `HostBuilderOptions`，调用 `HostUtility.Run<Startup>` |
| `Startup` | `AddModularityApplication`、平台能力注册、HTTP 管道 |
| `HostModuleContext` | `[DependsOn(FacadeServiceModule, HostingModule)]`；注册控制器、JWT、Swagger、`UserPrincipal` |
| `FacadeServiceModule` | 应用服务、消息处理器、总线约定与传输策略 |
| `DomainServiceModule` / `BusinessServiceModule` | 领域服务、AutoMapper/校验、业务对象与权限 |
| `PersistServiceModule` | 数据上下文工厂、连接串提供程序选择、种子数据 |
| `BootstrapUsers` | 内置管理员标识 `admin` 与默认团队 `T-1` |

## 配置项

示例使用 `appsettings.json`（`appsettings.Development.json` 只覆盖日志级别）。以下为**节选**，仅展示与本示例运行相关的平台节；完整的 `JwtAuthenticationOptions`、`Bootstrap` 等请直接查看仓库中的 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "Default": "sqlite://Data Source=euonia-sample.db"
  },
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "InMemory",
      "InMemory": {
        "MultipleSubscriberInstance": false
      },
      "RabbitMq": {
        "Enabled": true,
        "Connection": "amqp://guest:guest@127.0.0.1:5672/"
      }
    }
  }
}
```

| 键 | 说明 |
|------|------|
| `ConnectionStrings:Default` | `{provider}://{connection}` 形式；前缀决定 keyed `ConnectionConfigurator`（`sqlite` / `sqlserver` / `inmemory`），其余部分原样作为提供程序连接串 |
| `Euonia:Bus:DefaultTransporter` | 默认传输器名称；示例为 `InMemory` |
| `Euonia:Bus:InMemory:*` | 内存传输选项，详见 [`Euonia.Bus.InMemory`](../../Source/Euonia.Bus.InMemory/README.md) |
| `Euonia:Bus:RabbitMq:Enabled` / `Connection` | RabbitMQ 传输开关与连接串 |

```bash
# .NET 原生：双下划线表示层级
Euonia__Bus__RabbitMq__Enabled=false
Euonia__Bus__DefaultTransporter=InMemory
ConnectionStrings__Default="sqlite://Data Source=euonia-sample.db"

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_BUS_RABBITMQ_ENABLED=false
EUONIA_BUS_DEFAULTTRANSPORTER=InMemory
```

示例自有的两个节不在平台配置白名单内，仅由示例代码读取，请在 `appsettings.json` 中按需修改：

- `Bootstrap:DefaultAdminPassword` —— 种子管理员的初始密码；缺失时回退为 `admin123`（`PersistServiceModule.cs:89`）。
- `JwtAuthenticationOptions` —— JWT 签发与校验参数，由 `HostModuleContext` 的 `AddJwtAuthentication("JwtAuthenticationOptions")` 读取（`HostModuleContext.cs:22`）。

## 使用示例

### 登录并调用受保护端点

```bash
TOKEN=$(curl -s -X POST http://localhost:5240/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"admin123"}' | jq -r .token)

curl -s http://localhost:5240/api/user/me -H "Authorization: Bearer $TOKEN"
curl -s 'http://localhost:5240/api/team?page=1&pageSize=10' -H "Authorization: Bearer $TOKEN"
```

登录响应包含 `token`、`userId`、`name`、`roles`；令牌只带身份与角色，权限码由 `ScopeSubjectResolver` 在请求时从授权数据解析。

### 触发一次消息发布

```bash
curl -s -X POST http://localhost:5240/api/bus/publish/eto -H "Authorization: Bearer $TOKEN"
```

该端点向通道 `nerosoft.chalky.eto:OnetimeCodeCreated` 发布 `OnetimeCodeCreatedEto`，`MessageSendSubscriber` 收到后在控制台打印验证码内容。

### 管道顺序（`Startup.Configure`）

```csharp
app.InitializeApplication();          // 模块初始化，必须最先
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseExecutionContext();
app.UseAuditing();
app.UseRouting();
app.UseTenancy();
app.UseEuoniaTelemetry();
app.UseAuthorization();
```

## 注意事项 / 限制

- 示例默认接入 RabbitMQ：本地未启动 broker 时请先关闭 `Euonia:Bus:RabbitMq:Enabled`，否则消息发布路径会因连接失败报错。
- 数据层使用 SQLite 文件与 `EnsureCreated()`，没有迁移；修改实体后需删除 `euonia-sample.db` 重新生成。
- 种子逻辑仅在 `Users` 表为空时执行（`PersistServiceModule.Seed`），重复启动不会重建管理员或覆盖密码。
- `AddInMemoryAuditingStore` 与 `AddInMemoryTenantStore` 只适合示例与测试，生产环境需替换为持久化实现。
- 管道顺序是硬约束：`InitializeApplication` 最外层、`ApiExceptionMiddleware` 紧随其后（否则异常会被开发者异常页吞掉）、`UseTenancy` 必须在 `UseAuthentication` 与 `UseRouting` 之后。
- 示例中的 `JwtAuthenticationOptions` 含演示用签名密钥，**不要**直接用于生产。
- 示例的租户能力以 `RequireTenant=false` 接入单租户数据，不演示 `DatabasePerTenant` 隔离。

## 常见问题与故障排查

### 启动报 `IOException: Failed to bind to address http://localhost:5240: address already in use`

**原因**：`launchSettings.json` 的 `http` 配置固定占用 5240（`Properties/launchSettings.json:17`），上一次的进程或另一个服务仍占用该端口。
**解决**：`dotnet run --project Samples/Euonia.Sample.Webapi --urls http://localhost:5241`，或先 `lsof -i :5240` 结束占用进程。

### 发布消息抛 `MessageTransportException`（传输器未注册）

**原因**：`RabbitMqBusModule` 只在 `Euonia:Bus:RabbitMq:Enabled=true` **且** `Connection` 非空时才注册传输器（`RabbitMqBusModule.cs:18-28`）；只写 `Enabled` 不写 `Connection` 时总线会在发布时找不到名为 `RabbitMq` 的传输器。
**解决**：同时配 `Connection`，或把 `Enabled` 关掉让消息走 `InMemory`；broker 未启动时不会阻断启动，但连接会按 `MaxFailureRetries` 指数退避重试并记录 critical 日志（`DefaultPersistentConnection.cs:91-110`）。

### 用 `admin123` 登录返回 401

**原因**：种子只在 `Users` 表为空时执行（`PersistServiceModule.cs:83-93`）。已有 `euonia-sample.db` 时不会重置密码；连续失败还会累加 `AccessFailedCount` 并临时锁定（`AuthController.cs:51-62`）。
**解决**：删除 `euonia-sample.db` 重新种子，或用已有密码调 `POST /api/user/me/password` 修改；锁定需等待 `LockoutEnd` 过期。

### 换实体后启动报 `SQLite Error 1: no such table` / 缺列

**原因**：示例用 `EnsureCreated()` 建库，没有迁移（`PersistServiceModule.cs:77`），旧库结构不会随实体更新。
**解决**：停掉应用、删除 `euonia-sample.db`（含 `-wal` / `-shm`）后重启，让 `EnsureCreated` 重建。

### Swagger 打开 404

**原因**：`UseSwagger` / `UseSwaggerUI` 只在 `env.IsDevelopment()` 下注册（`Startup.cs:75-79`），`dotnet run` 的默认 profile 设置了 `ASPNETCORE_ENVIRONMENT=Development`，但外部 `dotnet Samples/.../bin/...dll` 启动时可能不是 Development。
**解决**：设置 `ASPNETCORE_ENVIRONMENT=Development`，或直接用 `--launch-profile http` 启动。

## 最佳实践

- **外部依赖用环境变量开关**：本地无 broker 时 `Euonia__Bus__RabbitMq__Enabled=false`，不要改 `appsettings.json` 以免误提交。
- **首跑前清库**：种子与结构都依赖干净数据库，重置顺序是“停进程 → 删 db → 启动”。
- **固定启动 profile**：`dotnet run --project Samples/Euonia.Sample.Webapi --launch-profile http` 保证 5240 与 Swagger 路径稳定，便于照文档验证。
- **排障先看分级别日志**：Serilog 把 Fatal/Error/Warning/Info 分文件写到 `Logs/`（`Program.cs:31-40`），异常细节优先查 `Logs/Error`。
- **只把 InMemory 实现当接线示例**：`AddInMemoryAuditingStore` / `AddInMemoryTenantStore` 与示例签名密钥都不可用于生产。

## 版本与兼容

- **目标框架**：`net11.0` 单目标（`Samples/common.props:5`），不打包（`IsPackable=false`）。
- **关键依赖**：EF Core 走仓库中央版本区间 `[11.0.0,12.0.0)`（`Directory.Packages.props:10,98-107`）；Serilog `4.4.0`、Swashbuckle `11.0.1`、RabbitMQ.Client `7.2.2`（`Directory.Packages.props:42,46,51`）。
- **升级注意**：升级 EF Core 主版本需同步删除本地 `euonia-sample.db` 重新建库；`TargetFramework` 跟随仓库 SDK，不要单独改示例的 TFM。
- **已知边界**：示例不演示多租户隔离（`RequireTenant=false`）、不演示 OTLP 导出，也没有数据库迁移。

## 相关包

- [`Euonia.Hosting`](../../Source/Euonia.Hosting/README.md) —— `HostUtility`、`AddJwtAuthentication`、`UseTenancy`
- [`Euonia.Modularity`](../../Source/Euonia.Modularity/README.md) —— 模块系统与配置
- [`Euonia.Bus`](../../Source/Euonia.Bus/README.md) —— 总线抽象与拦截器
- [`Euonia.Bus.InMemory`](../../Source/Euonia.Bus.InMemory/README.md) / [`Euonia.Bus.RabbitMq`](../../Source/Euonia.Bus.RabbitMq/README.md) —— 两种传输
- [`Euonia.Repository.EfCore`](../../Source/Euonia.Repository.EfCore/README.md) —— EF Core 仓储与连接串提供程序
- [`Euonia.Osba`](../../Source/Euonia.Osba/README.md) —— 业务对象与数据权限
- [`Euonia.Security`](../../Source/Euonia.Security/README.md) —— 权限声明与判定
- [`Euonia.Tenancy`](../../Source/Euonia.Tenancy/README.md) / [`Euonia.Telemetry`](../../Source/Euonia.Telemetry/README.md) —— 平台能力
- [`Euonia.Application`](../../Source/Euonia.Application/README.md) —— 应用服务基类与拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
