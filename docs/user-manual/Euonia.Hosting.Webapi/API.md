# Euonia.Hosting.Webapi API 参考

- **包名**：`Euonia.Hosting.Webapi`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting.Webapi`

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `EuoniaWebApiOptions` | 宿主选项 | 异常、OpenAPI、健康检查、HTTPS 默认开启 |
| `AddEuoniaWebApi(Action<EuoniaWebApiOptions>?)` | 注册控制器、异常处理、OpenAPI、健康检查与 CORS | `configure` 可为 `null` |
| `UseEuoniaWebApi()`（`WebApplication`） | 配置最小宿主管道并映射端点 | 异常 → HTTPS → CORS → 端点 |
| `UseEuoniaWebApi()`（`IApplicationBuilder`） | 供 Startup 使用，内部调用 `UseEndpoints` | 调用前需 `UseRouting` |
| `EuoniaWebApiOptions.HealthCheckPath` | 健康检查路径 | 默认 `/health` |
| `EuoniaWebApiOptions.AllowedOrigins` | CORS 来源白名单 | 空数组时不注册 CORS |
| `AddPermissionGuard(Action<PermissionOptions>?)` | 注册权限策略提供程序与授权处理器 | 无参重载使用 `NoModels()` |
| `[RequiresPermission(params string[] permissions)]` | 声明操作所需权限码 | 多码为“任一满足” |
| `PermissionPolicyProvider` / `PermissionAuthorizationHandler` | 权限策略还原与 `IScopeGuard` 判定 | 由 `AddPermissionGuard` 注册 |
| `AddEuoniaRateLimiting(Action<EuoniaRateLimitingOptions>?)` | 注册双窗口限流默认策略 | 默认策略名 `euonia`，按 IP 分区 |
| `UseEuoniaRateLimiting()` | 启用 ASP.NET Core 限流中间件 | 需先注册限流服务 |
