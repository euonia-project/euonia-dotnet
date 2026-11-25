# Euonia.Osba 权限示例

本示例演示 `BusinessObjectFactory` 在调用业务方法之前执行的权限判定：
类型级 `[Permission]` 对所有方法生效，方法级 `[Permission]` 只约束被工厂调用的那个方法。

完整可运行工程：[docs/examples/osba-console](../../docs/examples/osba-console/README.md)。

## 安装

```bash
dotnet add package Euonia.Osba
```

## 完整代码

完整 `Program.cs` 见 [osba-console/Program.cs](../../docs/examples/osba-console/Program.cs)。
关键部分：

```csharp
[Permission("order:read")]                    // 类型级：对该类型的所有方法生效
public sealed class Order : EditableObject<Order>
{
    public string Name { get; set; } = string.Empty;

    [Permission("order:update")]               // 方法级：只在 UpdateAsync 被工厂调用时生效
    [FactoryUpdate]
    protected override Task UpdateAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
```

```csharp
services.AddBusinessObject(typeof(Order).Assembly);                  // 工厂与业务上下文
services.AddPermission(p => p.Scan(typeof(Order).Assembly));         // 可选策略引擎
```

```csharp
var order = new Order { Name = "A-1", BusinessContext = businessContext };
order.MarkAsChanged();
var saved = await factory.SaveAsync(order, CancellationToken.None);
```

## 运行

```bash
dotnet run --project docs/examples/osba-console
```

预期输出：

```text
granted(order:read+order:update) => saved=A-1
missing(order:update) => SecurityException
missing(order:read) => SecurityException
```

## 输出解释

| 输出 | 原因 |
|---|---|
| `granted(order:read+order:update) => saved=A-1` | 类型级 `order:read` 与方法级 `order:update` 都已授予，更新成功。 |
| `missing(order:update) => SecurityException` | 只授予 `order:read`，方法级 `order:update` 未满足，工厂边界拒绝。 |
| `missing(order:read) => SecurityException` | 只授予 `order:update`，类型级 `order:read` 未满足，更新路径同样需要它。 |

## 要点

- **类型级 vs 方法级**：类型级要求对该类型的所有方法生效；方法级只对被工厂调用的那个方法生效。
- **同一个 `MethodInfo`**：工厂先解析即将 invoke 的方法，再交给 `ObjectAuthorization`；
  判定通过后才执行该方法。
- **查询语义**：`CanPerformOperation` 只反映类型级与已登记来源，不含方法级声明；
  条件分支需要方法级码时直接使用 `IPermissionChecker.IsGranted`。
- **失败形态**：有要求但判定不了抛 `InvalidOperationException`；判定得出但不满足抛
  `SecurityException`。

## 宿主装配

需要策略引擎时，宿主可选装配 `Euonia.Security`；装配细节与数据权限模型见
[Euonia.Security README](../Euonia.Security/README.md)。
