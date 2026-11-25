# Euonia.Hosting.Azfunc API 参考

- **包名**：`Euonia.Hosting.Azfunc`
- **目标框架**：`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting.Azfunc`

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `EuoniaFunctionsOptions` | 函数宿主选项 | 异常处理、HTTP 上下文中转默认开启 |
| `AddEuoniaFunctions(Action<EuoniaFunctionsOptions>?)` | 注册选项、`IHttpContextAccessor`、`ClaimsPrincipal` 与 `UserPrincipal` | `configure` 可为 `null` |
| `AddEuoniaFunctionsSecurity(Action<PermissionOptions>?)` | 在用户主体基础上注册权限体系 | 仅控制器权限时传 `options => options.NoModels()` |
| `UseEuoniaFunctions()`（`FunctionsApplicationBuilder`） | 注册请求上下文与异常中间件 | 需先调用 `AddEuoniaFunctions` |
| `UseEuoniaFunctions()`（`IFunctionsWorkerApplicationBuilder`） | 供 `ConfigureFunctionsWorkerDefaults` 使用 | 需先调用 `AddEuoniaFunctions` |
| `FunctionBase<T>` | 函数基类 | 提供 `Logger`、`ExecuteAsync`、`Ok` |
| `EnableRequestTrace` | 请求追踪响应头 | 默认 `true` |
| `EnableJwtAuthentication` / `JwtAuthentication` | Bearer JWT 校验与函数级认证 | 默认关闭；支持 `[Authorize]` 角色与 `[AllowAnonymous]` |
| `EnableRateLimiting` / `RateLimiting` | 函数级双窗口限流 | 默认关闭；按函数名 + IP 分区 |
| `RateLimitPartitionKeySelector` | 自定义限流分区键 | 默认客户端 IP |
