# Euonia.Hosting 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Hosting |
| 公共类型 | 25 |
| 命名空间 | 5 |

## 命名空间 Microsoft.AspNetCore.Authentication

### Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationDefaults

**类型**：static class

- static const System.String AuthenticationScheme = Bearer
- static const System.String JwtAccessTokenTyp = at+jwt

### Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationExtensions

**类型**：static class

- static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerAuthentication(Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder)
- static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerAuthentication(Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.String authenticationScheme)
- static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerAuthentication(Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, Action<Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions> configureOptions)
- static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerAuthentication(Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.String authenticationScheme, Action<Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions> configureOptions)
- static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddIdentityServerAuthentication(Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.String authenticationScheme, Action<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> jwtBearerOptions, Action<Duende.AspNetCore.Authentication.OAuth2Introspection.OAuth2IntrospectionOptions> introspectionOptions)

### Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationHandler

**类型**：class
基类 AuthenticationHandler<Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions>；接口 Microsoft.AspNetCore.Authentication.IAuthenticationHandler

- new Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationHandler(IOptionsMonitor<Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock)

### Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions

**类型**：class
基类 Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions

- new Microsoft.AspNetCore.Authentication.IdentityServerAuthenticationOptions()
- System.String ApiName { get; set; }
- System.String ApiSecret { get; set; }
- System.String Authority { get; set; }
- System.TimeSpan BackChannelTimeouts { get; set; }
- System.TimeSpan CacheDuration { get; set; }
- System.String CacheKeyPrefix { get; set; }
- Nullable<System.TimeSpan> DiscoveryDocumentRefreshInterval { get; set; }
- System.Boolean EnableCaching { get; set; }
- Duende.IdentityModel.Client.DiscoveryPolicy IntrospectionDiscoveryPolicy { get; set; }
- System.Net.Http.HttpMessageHandler JwtBackChannelHandler { get; set; }
- Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents JwtBearerEvents { get; set; }
- Nullable<System.TimeSpan> JwtValidationClockSkew { get; set; }
- System.Boolean LegacyAudienceValidation { get; set; }
- System.String NameClaimType { get; set; }
- Duende.AspNetCore.Authentication.OAuth2Introspection.OAuth2IntrospectionEvents OAuth2IntrospectionEvents { get; set; }
- System.Boolean RequireHttpsMetadata { get; set; }
- System.String RoleClaimType { get; set; }
- System.Boolean SaveToken { get; set; }
- Microsoft.AspNetCore.Authentication.SupportedTokens SupportedTokens { get; set; }
- System.Boolean SupportsIntrospection { get; }
- System.Boolean SupportsJwt { get; }
- Func<Microsoft.AspNetCore.Http.HttpRequest, System.String> TokenRetriever { get; set; }

### Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions

**类型**：class

- new Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions()
- System.String Audience { get; set; }
- System.String Authority { get; set; }
- IEnumerable<System.String> Issuer { get; set; }
- System.String NameClaimType { get; set; }
- System.Boolean RequireHttpsMetadata { get; set; }
- System.String RoleClaimType { get; set; }
- System.String Scheme { get; set; }
- System.String SigningKey { get; set; }
- System.Boolean UsePolicy { get; set; }
- System.Boolean ValidateAudience { get; set; }
- System.Boolean ValidateIssuer { get; set; }

### Microsoft.AspNetCore.Authentication.ScopeClaimsTransformer

**类型**：class
接口 Microsoft.AspNetCore.Authentication.IClaimsTransformation

- new Microsoft.AspNetCore.Authentication.ScopeClaimsTransformer()
- Task<System.Security.Claims.ClaimsPrincipal> TransformAsync(System.Security.Claims.ClaimsPrincipal principal)

### Microsoft.AspNetCore.Authentication.SupportedTokens

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Both |
| Jwt |
| Reference |


## 命名空间 Microsoft.AspNetCore.Authorization

### Microsoft.AspNetCore.Authorization.AuthorizationOptionsExtensions

**类型**：static class

- static Microsoft.AspNetCore.Authorization.AuthorizationOptions AddScopePolicy(Microsoft.AspNetCore.Authorization.AuthorizationOptions options, System.String policyName, System.String[] scopes)

### Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder RequireScope(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder builder, System.String[] scope)

### Microsoft.AspNetCore.Authorization.AuthorizeRolesAttribute

**类型**：class
基类 Microsoft.AspNetCore.Authorization.AuthorizeAttribute；接口 Microsoft.AspNetCore.Authorization.IAuthorizeData

- new Microsoft.AspNetCore.Authorization.AuthorizeRolesAttribute(System.String[] roles)

### Microsoft.AspNetCore.Authorization.TokenValidation.SchemeCredential

**类型**：struct
接口 IEquatable<Microsoft.AspNetCore.Authorization.TokenValidation.SchemeCredential>

- new Microsoft.AspNetCore.Authorization.TokenValidation.SchemeCredential(System.String Scheme, System.String Credential)
- System.String Credential { get; set; }
- static Microsoft.AspNetCore.Authorization.TokenValidation.SchemeCredential Empty { get; }
- System.String Scheme { get; set; }

### Microsoft.AspNetCore.Authorization.ScopePolicy

**类型**：static class

- static Microsoft.AspNetCore.Authorization.AuthorizationPolicy Create(System.String[] scopes)

### Microsoft.AspNetCore.Authorization.TokenValidation

**类型**：class

- new Microsoft.AspNetCore.Authorization.TokenValidation()
- static Func<Microsoft.AspNetCore.Http.HttpContext, System.String> ForwardReferenceToken(System.String introspectionScheme = "introspection")
- static Microsoft.AspNetCore.Authorization.TokenValidation.SchemeCredential GetSchemeAndCredential(Microsoft.AspNetCore.Http.HttpContext context)

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.ApplicationBuilderExtensions

**类型**：static class

- static void InitializeApplication(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseCulture(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDefaultRequestContextAccessor(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseJwt(Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.String schema = "Bearer")

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddJwtAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions> optionsAction)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddJwtAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String configurationSectionName)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddJwtAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions bearerOptions)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddJwtAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScopeTransformation(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddUserPrincipal(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
## 命名空间 Nerosoft.Euonia.Hosting

### Nerosoft.Euonia.Hosting.ApplicationInitializationContextExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder GetApplicationBuilder(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- static Microsoft.AspNetCore.Hosting.IWebHostEnvironment GetEnvironment(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- static Microsoft.AspNetCore.Hosting.IWebHostEnvironment GetEnvironmentOrNull(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Hosting.AuditingApplicationBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseAuditing(Microsoft.AspNetCore.Builder.IApplicationBuilder app)

### Nerosoft.Euonia.Hosting.ExecutionContextApplicationBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseExecutionContext(Microsoft.AspNetCore.Builder.IApplicationBuilder app)

### Nerosoft.Euonia.Hosting.HostBuilderOptions

**类型**：class

- new Nerosoft.Euonia.Hosting.HostBuilderOptions()
- System.Object ApplicationName { get; set; }
- System.Boolean CaptureStartupErrors { get; set; }
- Action<Microsoft.Extensions.Hosting.IHostBuilder> ConfigureHostBuilder { get; set; }
- Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> ConfigureWebHostBuilder { get; set; }
- System.Boolean EnableHttp2 { get; set; }
- static const System.String ApplicationNameVariable = SERVICE_NAME

### Nerosoft.Euonia.Hosting.HostingModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Hosting.HostingModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Hosting.HostUtility

**类型**：static class

- static void Run<TStartup>(System.String[] args, Action<Nerosoft.Euonia.Hosting.HostBuilderOptions> optionsAction = null, Func<System.String[], Nerosoft.Euonia.Hosting.HostBuilderOptions, Microsoft.Extensions.Hosting.IHostBuilder> createHostBuilder = null)
- static void Run<TStartup>(System.String[] args, Nerosoft.Euonia.Hosting.HostBuilderOptions options, Func<System.String[], Nerosoft.Euonia.Hosting.HostBuilderOptions, Microsoft.Extensions.Hosting.IHostBuilder> createHostBuilder = null)
- static System.Threading.Tasks.Task RunAsync<TStartup>(System.String[] args, Action<Nerosoft.Euonia.Hosting.HostBuilderOptions> optionsAction = null, Func<System.String[], Nerosoft.Euonia.Hosting.HostBuilderOptions, Microsoft.Extensions.Hosting.IHostBuilder> createHostBuilder = null)
- static System.Threading.Tasks.Task RunAsync<TStartup>(System.String[] args, Nerosoft.Euonia.Hosting.HostBuilderOptions options, Func<System.String[], Nerosoft.Euonia.Hosting.HostBuilderOptions, Microsoft.Extensions.Hosting.IHostBuilder> createHostBuilder = null)

### Nerosoft.Euonia.Hosting.TelemetryApplicationBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseEuoniaTelemetry(Microsoft.AspNetCore.Builder.IApplicationBuilder app)

### Nerosoft.Euonia.Hosting.TenancyApplicationBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseTenancy(Microsoft.AspNetCore.Builder.IApplicationBuilder app)

### Nerosoft.Euonia.Hosting.TenancyAspNetCoreServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTenancyAspNetCore(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> configure = null)

### Nerosoft.Euonia.Hosting.TenantHttpKeys

**类型**：static class

- static const System.String Candidates = Euonia:Tenant:Candidates
- static const System.String Context = Euonia:Tenant:Context
- static const System.String Failure = Euonia:Tenant:Failure
