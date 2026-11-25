# Euonia.Osba 教程

> 本页是端到端教程；API 速查见 [API.md](API.md)，完整签名见 [API-FULL.md](API-FULL.md)。

本教程运行一个最小控制台示例：工厂在调用业务方法之前执行权限判定。

## 安装

```bash
dotnet add package Euonia.Osba
```

示例工程已经包含在仓库中：

- [docs/examples/osba-console/Program.cs](../../../docs/examples/osba-console/Program.cs)
- [示例说明](../../../docs/examples/osba-console/README.md)

## 完整代码

完整 `Program.cs` 见上面的链接。核心结构：

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
var services = new ServiceCollection();
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
| `granted(order:read+order:update) => saved=A-1` | 类型级 `order:read` 与方法级 `order:update` 都满足，更新成功。 |
| `missing(order:update) => SecurityException` | 缺少方法级 `order:update`，工厂边界拒绝。 |
| `missing(order:read) => SecurityException` | 缺少类型级 `order:read`，更新路径同样被拒绝。 |

## 关键接线

- **工厂**：`AddBusinessObject` 扫描业务对象并注册 `BusinessObjectFactory`。
- **声明**：`[Permission]` 写在类型或工厂方法上；方法级只约束被调用的方法。
- **判定**：`factory.SaveAsync(order)` 先取 `MethodInfo`，交给 `ObjectAuthorization`，
  满足后才调用 `UpdateAsync`。
- **宿主装配**：需要策略引擎时，可选装配 `Euonia.Security`，调用
  `AddPermission(p => p.Scan(asm))`；具体装配见 [示例说明](../../../docs/examples/osba-console/README.md)。

## 常见失败点

- `Property 'X' is not registered.`：属性未注册或访问器名称不匹配；必须使用 `RegisterProperty` 的返回值。
- 状态转换异常：对已删除对象再 `MarkAsChanged`；自定义状态机需重写 `CanTransitionTo`。
- 首次 `SaveAsync` 才报缺少持久化方法：`ValidateOnStartup` 默认 `false`，生产宿主显式开启。
- 声明了 `[Permission]` 后抛异常：容器里没有判定实现，或目标未接入 `BusinessContext`。
- 方法级 `[Permission]` 不生效：确认该方法就是工厂调用的那个，且调用经过工厂。
- 后台任务拿不到 `BusinessContext`：用 `IServiceScopeFactory` 新建作用域并 `SetCurrent`。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Osba/README.md)
- [权限说明](../../../Source/Euonia.Osba/PERMISSION.md)
