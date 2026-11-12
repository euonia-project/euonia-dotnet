# Euonia.Linq

Euonia 的 LINQ 工具库：规约模式（Specification）、谓词与表达式构建器，以及过滤、分页、排序等 `IQueryable` 扩展，用于把查询条件建模为可组合、可复用的对象。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Linq`
- **NuGet 包**：`Euonia.Linq`

## 安装

```bash
dotnet add package Euonia.Linq
```

## 主要功能

- **规约模式**：`Specification<T>` 支持 `&`、`|`、`!` 组合，`ISpecification<T>` 可被仓储与查询直接消费。
- **表达式构建**：`PredicateBuilder`、`Lambda`、`PredicateExpressionBuilder<T>` 支持按属性名与运算符动态生成表达式。
- **范围过滤**：整型、长整型、浮点、十进制、日期、时间与时间跨度等 `SegmentSpecification`。
- **查询构建器**：`QueryHandler<T>` 以流式 API 组合条件、分页与排序。
- **`IQueryable` 扩展**：`Where(specification)`、`WhereIf`、`WhereIfNotEmpty`、`Between`、`OrderBy`（属性名 + 排序方向）。

## 快速开始

定义规约并组合：

```csharp
using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

public sealed class AdultSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> Satisfy() => person => person.Age >= 18;
}

public sealed class ActiveSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> Satisfy() => person => person.IsActive;
}

var criteria = new AdultSpecification() & new ActiveSpecification();
var people = await db.People.Where(criteria).ToListAsync();
```

使用 `QueryHandler<T>` 组合分页与排序：

```csharp
var handler = new QueryHandler<Person>(db.People)
    .AddCriteria(person => person.Name.Contains("a"))
    .OrderByDescending(person => person.CreatedAt)
    .SetPage(1)
    .SetSize(20);

var items = handler.Query();
var total = handler.GetCount();
```

按属性名动态构建条件：

```csharp
var predicate = Lambda.ParsePredicate<Person>("Age", 18, QueryOperator.GreaterThanOrEqual);
var result = await db.People.Where(predicate).ToListAsync();
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ISpecification<T>` / `Specification<T>` | 规约契约与可组合基类 |
| `AndSpecification<T>` / `OrSpecification<T>` / `NotSpecification<T>` | 逻辑组合规约 |
| `CompositeSpecification<T>` | 聚合多个规约（`AndAlso` / `OrElse`） |
| `SegmentSpecification<...>` | 范围过滤基类，配合 `RangeBoundary` 使用 |
| `PredicateBuilder` | `True<T>`、`False<T>`、比较与包含条件构建 |
| `Lambda` | 属性名/表达式解析与动态谓词生成 |
| `PredicateExpressionBuilder<T>` | 以操作符枚举构建谓词 |
| `QueryHandler<T>` | 流式查询构建器 |
| `QueryCriteria<T>` / `Orderable<T>` | 查询条件与排序描述 |
| `QueryableExtensions` | `Where`、`WhereIf`、`Between`、`OrderBy` 等扩展 |

## 选项（代码配置）

`Euonia.Linq` 是纯静态工具库，没有 `Add*` 注册、配置节或 `*Options`；可调参数都通过方法实参与对象属性传入：

| 配置点 | 默认值 | 说明 |
|--------|--------|------|
| `QueryHandler<T>.SetPage(int)` | `1` | 页码从 1 开始，小于 1 抛 `ArgumentOutOfRangeException` |
| `QueryHandler<T>.SetSize(int)` | `int.MaxValue` | 每页大小，`0` 返回空列表，小于 0 抛异常 |
| `QueryHandler<T>.SetCollator(...)` | 无排序 | 用 `Orderable<T>` 或 `Func<IQueryable<T>, IOrderedQueryable<T>>` 指定排序 |
| `QueryableExtensions.Between(..., boundary)` | `RangeBoundary.Both` | `DateTime` 的 `includeTime: false` 分支默认 `RangeBoundary.Left` |
| `QueryableExtensions.WhereIf(predicate, condition)` | — | `condition` 为 `false` 时返回原查询 |

## 注意事项 / 限制

- `WhereIfNotEmpty` 只允许一个条件属性（例如 `t => t.Name == "a"`）；包含 `&&`/`||` 的谓词会抛 `InvalidOperationException`，空字符串或 `null` 值会被忽略。
- 按属性名动态调用的 `Where(propertyName, value)`、`OrderBy(propertyName, sortType)` 在属性不存在或值无法转换时**静默返回原查询**，不会抛异常；来自外部输入的属性名应先校验。
- `QueryHandler<T>.Query()` 每次基于原始查询重新构建，重复调用不会叠加过滤条件；`GetCount()` 基于未分页查询计算总数，不受 `SetPage`/`SetSize` 影响。
- `Specification<T>` 的 `&`、`|`、`!` 运算符定义在抽象类上，不是 `ISpecification<T>` 接口；`ISpecification<T>` 可用 `And`/`AndIf` 扩展，但内部会强制转换为 `Specification<T>`，其他实现会抛 `InvalidCastException`。
- `SegmentSpecification` 要求 `min` 与 `max` 至少提供一个；两者都提供时最小值不得大于最大值，否则构造失败。`RangeBoundary` 决定两端是否包含。
- `QueryHandler<T>` 持有可变的页码、大小、谓词与排序状态，不是线程安全的；不要跨请求共享同一个实例。

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础工具
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储查询
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 仓储实现

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
