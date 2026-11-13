# Euonia.Sample.Webapi API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

可运行的 Euonia 全栈示例：模块化宿主、消息总线、EF Core 仓储、权限、审计、租户与遥测在一次启动里全部接好。

| 项 | 值 |
|---|---|
| 项目 | `Samples/Euonia.Sample.Webapi`（`IsPackable=false`，不进 NuGet） |
| 目标框架 | `net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Sample` |
| 入口 | `Program.Main` → `HostUtility.Run<Startup>(args, ...)` |
| 完整参考 | [`Samples/Euonia.Sample.Webapi/README.md`](../../../Samples/Euonia.Sample.Webapi/README.md) |

## API 参考

| 类型 / 端点 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `Program.Main(string[])` | 配置 Serilog 与宿主选项后启动 | 调用 `HostUtility.Run<Startup>` |
| `Startup.ConfigureServices(IServiceCollection)` | 注册模块与平台能力 | `AddModularityApplication<HostModuleContext>` |
| `Startup.Configure(IApplicationBuilder, IWebHostEnvironment)` | 管道装配 | 异常中间件最外层 |
| `HostModuleContext` | `[DependsOn(FacadeServiceModule, HostingModule)]` | 注册控制器/JWT/Swagger |
| `FacadeServiceModule` | 总线约定与传输策略 | 绑定 `Euonia:Bus` |
| `DomainServiceModule` / `BusinessServiceModule` | 领域服务、业务对象、权限 | `AddPermission` 扫描程序集 |
| `PersistServiceModule` | 数据上下文工厂与种子 | 连接串前缀选提供程序 |
| `POST /api/auth/login` | 用户名密码登录 | `{"username","password"}` |
| `GET /api/user/me` | 当前用户 | 需 Bearer |
| `POST /api/team`、`GET /api/team` | 团队创建/分页列表 | `page`、`pageSize` |
| `POST /api/project`、`POST /api/repository` | 项目/仓库 | 需对应权限码 |
| `POST /api/bus/publish/eto` | 发布 `OnetimeCodeCreatedEto` | 固定通道 `nerosoft.chalky.eto:OnetimeCodeCreated` |
| `GET /health` | 健康检查 | 默认排除租户解析 |

### 配置项

示例自用节（不在平台配置白名单内，改 `appsettings.json` 即可）：

| 键 | 说明 | 默认值 |
|---|---|---|
| `ConnectionStrings:Default` | `{provider}://{connection}` | `sqlite://Data Source=euonia-sample.db` |
| `Euonia:Bus:DefaultTransporter` | 默认传输器 | `InMemory` |
| `Euonia:Bus:RabbitMq:Enabled` / `Connection` | RabbitMQ 开关与连接串 | `true` / `amqp://guest:guest@127.0.0.1:5672/` |
| `Bootstrap:DefaultAdminPassword` | 种子管理员密码 | 缺省回退 `admin123`（`PersistServiceModule.cs:89-93`） |
| `JwtAuthenticationOptions` | JWT 签发/校验参数 | 示例自带演示密钥 |

```bash
# .NET 原生：双下划线表示层级
Euonia__Bus__RabbitMq__Enabled=false
ConnectionStrings__Default="Data Source=euonia-sample.db"

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_BUS_RABBITMQ_ENABLED=false
```

## 相关文档

- [教程](TUTORIAL.md)
- [`Samples/Euonia.Sample.Webapi` README](../../../Samples/Euonia.Sample.Webapi/README.md) —— 外部依赖、完整端点与限制
- [Tenancy 手册](../Euonia.Tenancy/API.md) —— 示例里的 `AddTenancyAspNetCore` 用法
- [Telemetry 手册](../Euonia.Telemetry/API.md) —— 示例里的 `AddEuoniaTelemetry` 用法
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `HostUtility` / `AddJwtAuthentication`
- [`Source/Euonia.Bus/README.md`](../../../Source/Euonia.Bus/README.md) —— 通道与传输策略
- [根 README](../../../README.md)
