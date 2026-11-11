# Euonia.Linq

Euonia 的 LINQ 工具库：规约模式（Specification）、谓词与表达式构建器，以及过滤、分页、排序等 `IQueryable` 扩展，用于把查询条件建模为可组合、可复用的对象。

LINQ utilities for Euonia: the specification pattern, predicate and expression builders, and `IQueryable` extensions for filtering, pagination and ordering. Query criteria can be modeled as composable, reusable objects.

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

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础工具
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储查询
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 仓储实现

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
