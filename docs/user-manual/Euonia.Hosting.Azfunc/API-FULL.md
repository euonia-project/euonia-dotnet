# Euonia.Hosting.Azfunc 完整 API 参考

> 语义与用法见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Hosting.Azfunc |
| 公共类型 | 4 |
| 命名空间 | 3 |

## 命名空间 Nerosoft.Euonia.Hosting.Azfunc

### EuoniaFunctionsOptions

**类型**：class

- System.Boolean EnableExceptionHandling { get; set; }
- System.Boolean EnableHttpContext { get; set; }
- System.Boolean EnableJwtAuthentication { get; set; }
- Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions JwtAuthentication { get; set; }
- System.Boolean EnableRateLimiting { get; set; }
- System.Boolean EnableRequestTrace { get; set; }
- System.Func<Microsoft.AspNetCore.Http.HttpContext, System.String> RateLimitPartitionKeySelector { get; set; }
- Nerosoft.Euonia.Hosting.RateLimiting.DualWindowRateLimiterOptions RateLimiting { get; set; }

### FunctionBase<T>

**类型**：class

- FunctionBase<T>(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)

## 命名空间 Microsoft.Extensions.DependencyInjection

### ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaFunctions(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Nerosoft.Euonia.Hosting.Azfunc.EuoniaFunctionsOptions> configure = null)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaFunctionsSecurity(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Nerosoft.Euonia.Security.PermissionOptions> configurePermissions = null)

## 命名空间 Microsoft.Extensions.Hosting

### ApplicationBuilderExtensions

**类型**：static class

- static Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder UseEuoniaFunctions(Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder builder)
- static Microsoft.Azure.Functions.Worker.Builder.FunctionsApplicationBuilder UseEuoniaFunctions(Microsoft.Azure.Functions.Worker.Builder.FunctionsApplicationBuilder application)
