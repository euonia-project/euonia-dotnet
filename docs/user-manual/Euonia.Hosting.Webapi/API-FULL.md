# Euonia.Hosting.Webapi 完整 API 参考

> 语义与用法见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Hosting.Webapi |
| 公共类型 | 9 |
| 命名空间 | 6 |

## 命名空间 Microsoft.AspNetCore.Authorization

### Microsoft.AspNetCore.Authorization.PermissionAuthorizationHandler

**类型**：class
基类 Microsoft.AspNetCore.Authorization.AuthorizationHandler<Microsoft.AspNetCore.Authorization.PermissionRequirement>

- PermissionAuthorizationHandler(Nerosoft.Euonia.Security.IScopeGuard guard, Microsoft.AspNetCore.Http.IHttpContextAccessor accessor)

### Microsoft.AspNetCore.Authorization.PermissionPolicyProvider

**类型**：class
接口 Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider

- PermissionPolicyProvider(Microsoft.AspNetCore.Authorization.DefaultAuthorizationPolicyProvider fallback)
- Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetDefaultPolicyAsync()
- Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetFallbackPolicyAsync()
- Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy> GetPolicyAsync(System.String policyName)

### Microsoft.AspNetCore.Authorization.PermissionRequirement

**类型**：class
接口 Microsoft.AspNetCore.Authorization.IAuthorizationRequirement

- PermissionRequirement(System.String[] permissions)
- System.String[] Permissions { get; }

### Microsoft.AspNetCore.Authorization.RequiresPermissionAttribute

**类型**：class
基类 Microsoft.AspNetCore.Authorization.AuthorizeAttribute

- RequiresPermissionAttribute(System.String[] permissions)
- System.String[] Permissions { get; }
- static const System.String PolicyPrefix = Permission:
- static const System.String PolicySeparator = |

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.WebApplicationExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseEuoniaWebApi(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
- static Microsoft.AspNetCore.Builder.WebApplication UseEuoniaWebApi(Microsoft.AspNetCore.Builder.WebApplication app)

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaWebApi(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Nerosoft.Euonia.Hosting.Webapi.EuoniaWebApiOptions> configure = null)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddPermissionGuard(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddPermissionGuard(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Nerosoft.Euonia.Security.PermissionOptions> configure)

## 命名空间 Nerosoft.Euonia.Hosting

### Nerosoft.Euonia.Hosting.RateLimitingApplicationBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseEuoniaRateLimiting(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaRateLimiting(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Nerosoft.Euonia.Hosting.RateLimiting.EuoniaRateLimitingOptions> configure = null)

## 命名空间 Nerosoft.Euonia.Hosting.RateLimiting

### Nerosoft.Euonia.Hosting.RateLimiting.EuoniaRateLimitingOptions

**类型**：class

- Nerosoft.Euonia.Hosting.RateLimiting.DualWindowRateLimiterOptions DefaultLimiter { get; set; }
- System.Boolean EnableDefaultPolicy { get; set; }
- System.Func<Microsoft.AspNetCore.Http.HttpContext, System.String> PartitionKeySelector { get; set; }
- System.String PolicyName { get; set; }
- static const System.String DefaultPolicyName = euonia

## 命名空间 Nerosoft.Euonia.Hosting.Webapi

### Nerosoft.Euonia.Hosting.Webapi.EuoniaWebApiOptions

**类型**：class

- System.String[] AllowedOrigins { get; set; }
- System.Boolean EnableExceptionHandling { get; set; }
- System.Boolean EnableHealthChecks { get; set; }
- System.Boolean EnableHttpsRedirection { get; set; }
- System.Boolean EnableOpenApi { get; set; }
- System.String HealthCheckPath { get; set; }
