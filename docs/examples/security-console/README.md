# security-console —— Euonia.Security 最小可运行示例

只依赖 `Euonia.Security`（不引用任何对象框架），演示两件事：

1. **操作权限**：类型级与方法级 `[Permission]` 声明如何被收集与判定；
2. **数据（行级）权限**：`ScopeModel<T>` + `IScopeSubjectResolver` + `IScopeGuard.Apply` / `Allows`。

## 运行

```bash
dotnet run --project docs/examples/security-console
```

需要 .NET 11 SDK。第一次运行会还原依赖。

## 预期输出

```text
gate: read=allowed approve=denied delete=allowed export=allowed audit=denied
visible=A-1,A-2
allows(read)=True,True,False,False
allows(order:delete)=True,False,False,True
```

## 逐行说明

| 输出 | 含义 |
|---|---|
| `gate: read=allowed` | `Order` 上的类型级 `[Permission("order:read")]` 命中，用户持有 `order:read`。 |
| `approve=denied` | `Approve()` 的方法级 `[Permission("order:approve")]` 命中，用户没有 `order:approve`。 |
| `delete=allowed` | `Delete()` 的方法级 `[Permission("order:delete")]` 命中，用户持有该码。 |
| `export=allowed` | `Export()` 要求 `order:export` + 角色（`manager`/`auditor`），用户两者都满足。 |
| `audit=denied` | `Audit()` 的码已授予，但角色 `auditor` 不满足——角色要求同样参与判定。 |
| `visible=A-1,A-2` | `IScopeGuard.Apply` 按部门维度下推，只保留 `dept=sales` 的行。 |
| `allows(read)=...` | 逐行判定的结果与 `Apply` 一致（同一份策略与授权数据）。 |
| `allows(order:delete)=...` | `OrderScope` 为 `order:delete` 单独声明了「本人可删」，与默认的部门策略不同。 |

## 代码要点

- **装配**：`AddPermission(p => p.Scan(typeof(Order).Assembly))` 扫描程序集即可；权限码写在
  `[Permission]` 上，不需要为新增的码修改配置。
- **通用宿主闸门**：`Program.cs` 的 `Verdict` 演示宿主要写的全部判定逻辑——
  `PermissionRequirements.For(type, method)` 收集「类型级 + 这一个方法」的要求，
  再逐条交给 `IPermissionChecker.IsGranted` / `IsInRole`。
- **预热**：`IScopeGuard` 的同步成员只读已解析的快照；示例在判定前
  `await guard.EnsureResolvedAsync()`。异步宿主入口应先预热，同步入口再判定。
- **授权值**：`DeptSubjectResolver` 就是应用自己的「用户被授予了什么」查询；
  示例固定返回 `dept=sales`、`owner=alice` 与四个权限码。

## 换掉示例数据

改写 `DeptSubjectResolver.ResolveAsync` 即可接入真实授权数据（用户、角色、组织、权限表）。
权限码不放进令牌：每次解析都从数据源实时读取，撤销在下次解析时生效。
