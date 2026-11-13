# Euonia.Bus.HealthChecks 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.HealthChecks |
| 公共类型 | 3 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.HealthChecksBuilderExtensions

**类型**：static class

- static const System.String Name = euonia-bus
- static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddEuoniaBusHealthChecks(Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, Action<Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheckOptions> configure = null, Nullable<Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus> failureStatus = default, IEnumerable<System.String> tags = null, Nullable<System.TimeSpan> timeout = default)

## 命名空间 Nerosoft.Euonia.Bus.HealthChecks

### Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheck

**类型**：class
接口 Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck

- new Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheck(System.IServiceProvider provider, IOptions<Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheckOptions> options)
- Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheckOptions

**类型**：class

- new Nerosoft.Euonia.Bus.HealthChecks.BusHealthCheckOptions()
- System.Int32 MaxDeadLetters { get; set; }
- System.Int32 MaxInboxFailed { get; set; }
- System.Int32 MaxOutboxFailed { get; set; }
- System.Boolean RequireTransporter { get; set; }
- static const System.String SectionName = Euonia:Bus:HealthChecks
