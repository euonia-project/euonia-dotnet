# osba-console —— Euonia.Osba 最小可运行示例

演示**工厂边界上的方法级权限**：`[Permission]` 写在业务类型与工厂方法上，
`BusinessObjectFactory` 在调用业务方法之前完成判定。

## 运行

```bash
dotnet run --project docs/examples/osba-console
```

需要 .NET 11 SDK。第一次运行会还原依赖。

## 预期输出

```text
granted(order:read+order:update) => saved=A-1
missing(order:update) => SecurityException
missing(order:read) => SecurityException
```

## 逐行说明

| 输出 | 含义 |
|---|---|
| `granted(order:read+order:update) => saved=A-1` | 类型级 `order:read` 与方法级 `order:update` 都授予，更新保存成功。 |
| `missing(order:update) => SecurityException` | 只授予 `order:read`：方法级 `order:update` 未授予，工厂边界拒绝。 |
| `missing(order:read) => SecurityException` | 只授予 `order:update`：类型级 `order:read` 对该类型的所有操作生效，更新路径同样需要它。 |

## 代码要点

- **业务对象**：`Order : EditableObject<Order>`，`[FactoryUpdate]` 标注的 `UpdateAsync` 上是
  方法级 `[Permission("order:update")]`；类型级 `[Permission("order:read")]` 对所有操作生效。
- **方法级隔离**：`order:update` 只在 `UpdateAsync` 被工厂调用时要求；
  同类型的其它入口方法上的码不会叠加到更新路径上。
- **装配**（最小四步）：

  ```csharp
  services.AddBusinessObject(typeof(Order).Assembly);                  // 业务对象与工厂
  services.AddPermission(p => p.Scan(typeof(Order).Assembly));          // 权限声明与模型
  services.AddSingleton<IScopeSubjectResolver>(new CodeSubjectResolver(codes)); // 授权值来源
  services.AddSingleton(CreateUser("alice"));                           // 当前用户
  ```

- **业务上下文**：工厂通过 `BusinessContext` 解析判定实现与授权数据。示例用
  `BusinessContextAccessor.SetCurrent(scope.ServiceProvider)` 建立环境作用域，
  再把 `BusinessContext` 赋给对象。
- **判定入口**：`factory.SaveAsync(order, cancellationToken)` 先判定、后调用工厂方法；
  未授予时抛 `SecurityException`，不会执行更新。

## 与 Security 示例的分工

本示例只装配到 `AddPermission(p => p.Scan(...))` 为止：权限码写在声明处，判定由工厂边界完成。
需要了解权限要求如何收集、数据权限如何下推，见 `docs/examples/security-console`。
