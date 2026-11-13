# Euonia.Sample.Webapi 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

可运行的 Euonia 全栈示例：模块化宿主、消息总线、EF Core 仓储、权限、审计、租户与遥测在一次启动里全部接好。

| 项 | 值 |
|---|---|
| 项目 | `Samples/Euonia.Sample.Webapi`（`IsPackable=false`，不进 NuGet） |
| 目标框架 | `net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Sample` |
| 入口 | `Program.Main` → `HostUtility.Run<Startup>(args, ...)` |
| 完整参考 | [`Samples/Euonia.Sample.Webapi/README.md`](../../../Samples/Euonia.Sample.Webapi/README.md) |

## 教程

### 场景

在本地把示例跑起来，走完“登录 → 建团队 → 查列表 → 发布一条集成事件”的闭环，并确认数据落在 `euonia-sample.db`、日志落在 `Logs/`、消息被订阅者收到。

### 前置条件

| 依赖 | 说明 |
|---|---|
| .NET SDK | 需支持 `net11.0` |
| RabbitMQ | 默认开启（`Euonia:Bus:RabbitMq:Enabled=true`）；没有 broker 时先关掉 |
| SQLite | 无需安装，库文件随应用创建 |

### 1. 启动

```bash
# 仓库根目录
dotnet run --project Samples/Euonia.Sample.Webapi --launch-profile http

# 访问 Swagger
open http://localhost:5240/swagger
```

没有 RabbitMQ 时用环境变量关掉传输（不改 `appsettings.json`）：

```bash
Euonia__Bus__RabbitMq__Enabled=false \
  dotnet run --project Samples/Euonia.Sample.Webapi --launch-profile http
```

### 2. 登录拿令牌

```bash
TOKEN=$(curl -s -X POST http://localhost:5240/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"admin123"}' | jq -r .token)

curl -s http://localhost:5240/api/user/me -H "Authorization: Bearer $TOKEN"
```

响应里的 `token` 只带身份与角色；权限码在请求时由 `ScopeSubjectResolver` 从授权表解析，所以改权限不用重新登录。

### 3. 走一遍业务闭环

```bash
# 建团队（管理员自动成为成员）
curl -s -X POST http://localhost:5240/api/team \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"支付组","description":"payment"}'

# 查列表（分页参数来自控制器签名）
curl -s "http://localhost:5240/api/team?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# 发布一条集成事件：控制台应打印 OnetimeCodeCreatedEto
curl -s -X POST http://localhost:5240/api/bus/publish/eto \
  -H "Authorization: Bearer $TOKEN"
```

### 4. 验证落点

```bash
# 数据库与日志
ls -lh Samples/Euonia.Sample.Webapi/euonia-sample.db
ls Samples/Euonia.Sample.Webapi/Logs/Error

# 健康检查（租户中间件默认排除 /health）
curl -s http://localhost:5240/health
```

- `euonia-sample.db`：首次启动由 `EnsureCreated()` 建表并写入管理员（`PersistServiceModule.cs:77-93`）。
- `Logs/{Debug,Info,Warning,Error,Fatal}/`：Serilog 按级别分文件（`Program.cs:31-40`）。
- 订阅者输出形如 `[MessageSendSubscriber] Received OTP: ...`（`MessageSendSubscriber.cs:19`）。

### 5. 改端口 / 重置数据

```bash
# 换端口
dotnet run --project Samples/Euonia.Sample.Webapi --urls http://localhost:5241

# 重置数据：停进程后删库（含 WAL/SHM），重启即重新种子
rm -f Samples/Euonia.Sample.Webapi/euonia-sample.db*
```

### 常见失败点

- **端口占用**：`Failed to bind to address http://localhost:5240`；换 `--urls` 或结束占用进程。
- **发布抛 `MessageTransportException`**：`RabbitMq:Enabled=true` 但 `Connection` 为空，模块不会注册传输器（`RabbitMqBusModule.cs:18-28`）。
- **`admin123` 登录 401**：库已存在时不会重新种子；删库重来，或用 `POST /api/user/me/password` 改密。
- **`no such table` / 缺列**：`EnsureCreated` 没有迁移，改实体后必须删库重建。
- **Swagger 404**：只有 `Development` 才注册 Swagger UI（`Startup.cs:75-79`），检查 `ASPNETCORE_ENVIRONMENT`。

## 相关文档

- [API 参考](API.md)
- [`Samples/Euonia.Sample.Webapi` README](../../../Samples/Euonia.Sample.Webapi/README.md) —— 外部依赖、完整端点与限制
- [Tenancy 手册](../Euonia.Tenancy/TUTORIAL.md) —— 示例里的 `AddTenancyAspNetCore` 用法
- [Telemetry 手册](../Euonia.Telemetry/TUTORIAL.md) —— 示例里的 `AddEuoniaTelemetry` 用法
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `HostUtility` / `AddJwtAuthentication`
- [`Source/Euonia.Bus/README.md`](../../../Source/Euonia.Bus/README.md) —— 通道与传输策略
- [根 README](../../../README.md)
