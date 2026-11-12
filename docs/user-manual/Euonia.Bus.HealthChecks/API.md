# Euonia.Bus.HealthChecks API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

Euonia 总线的 ASP.NET Core 健康检查：统计已分配传输策略的数量，并按阈值报告死信、发件箱失败与收件箱失败的积压。本页用运维视角演示就绪/存活探针与阈值验证；完整说明见 [项目 README](../../../Source/Euonia.Bus.HealthChecks/README.md)。

- **NuGet 包**：`Euonia.Bus.HealthChecks`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.HealthChecks`
- **完整选项**：[Source/Euonia.Bus.HealthChecks/README.md](../../../Source/Euonia.Bus.HealthChecks/README.md)

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddEuoniaBusHealthChecks(...)` | 在 `IHealthChecksBuilder` 上注册总线检查 | `configure`、`failureStatus`、`tags`、`timeout` |
| `HealthChecksBuilderExtensions.Name` | 注册名称常量 | `euonia-bus` |
| `BusHealthCheck` | `IHealthCheck` 实现 | 依赖 `IConfigurator` 与可选存储 |
| `BusHealthCheckOptions.MaxDeadLetters` | 死信上限 | 默认 `0` |
| `BusHealthCheckOptions.MaxOutboxFailed` | 发件箱失败上限 | 默认 `-1`（不检查） |
| `BusHealthCheckOptions.MaxInboxFailed` | 收件箱失败上限 | 默认 `-1`（不检查） |
| `BusHealthCheckOptions.RequireTransporter` | 无策略时是否不健康 | 默认 `false` |
| `IDeadLetterStore` | 死信存储契约 | `AddInMemoryDeadLetters()` 注册 |
| `IOutboxStore` / `IInboxStore` | 发件箱/收件箱存储契约 | 未注册则对应项跳过 |
| `IConfigurator.StrategyAssignedTypes` | 检查使用的传输器计数来源 | 已调用 `SetStrategy` 的名称 |

### 配置项

配置节 `Euonia:Bus:HealthChecks`（`Source/Euonia.Bus.HealthChecks/BusHealthCheckOptions.cs:12`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `MaxDeadLetters` | `int` | `0` |
| `MaxOutboxFailed` | `int` | `-1` |
| `MaxInboxFailed` | `int` | `-1` |
| `RequireTransporter` | `bool` | `false` |

```json
{
  "Euonia": {
    "Bus": {
      "HealthChecks": {
        "MaxDeadLetters": 10,
        "MaxOutboxFailed": 100,
        "MaxInboxFailed": 100,
        "RequireTransporter": true
      }
    }
  }
}
```

```bash
Euonia__Bus__HealthChecks__MaxDeadLetters=10    # .NET 原生：双下划线表示层级
EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS=10        # 仓库 alias：大写单下划线
Euonia__Bus__HealthChecks__RequireTransporter=true
EUONIA_BUS_HEALTHCHECKS_REQUIRETRANSPORTER=true
```

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.HealthChecks/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.Abstract 用户手册](../Euonia.Bus.Abstract/API.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/API.md)
- [仓库根 README](../../../README.md)
