# Euonia.Linq 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Linq` 提供规约模式、谓词/表达式构建器，以及过滤、分页、排序等 `IQueryable` 扩展，用于把查询条件建模为可组合、可复用的对象。

- **包名**：`Euonia.Linq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Linq`
- **完整选项/边界**：[Source/Euonia.Linq/README.md](../../../Source/Euonia.Linq/README.md)

## 教程：组合规约并分页查询人员列表

这个场景用两个规约筛选“成年且活跃”的人员，再用 `QueryHandler` 做降序排序和分页，并验证总数不受分页影响。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 示例使用内存集合；换成 EF Core 时 API 相同，但表达式翻译由 provider 决定。

### 1. 创建项目并安装

```bash
dotnet new console -n PeopleQuery
cd PeopleQuery
dotnet add package Euonia.Linq
```

### 2. 定义实体与规约

```csharp
using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

public sealed record Person(int Id, string Name, int Age, bool IsActive, DateTime CreatedAt);

public sealed class AdultSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> Satisfy() => person => person.Age >= 18;
}

public sealed class ActiveSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> Satisfy() => person => person.IsActive;
}
```

### 3. 组合规约并查询

```csharp
var people = new List<Person>
{
    new(1, "Ada", 30, true, new DateTime(2026, 1, 1)),
    new(2, "Bob", 17, true, new DateTime(2026, 1, 2)),
    new(3, "Cara", 25, false, new DateTime(2026, 1, 3)),
    new(4, "Dan", 40, true, new DateTime(2026, 1, 4)),
    new(5, "Eve", 22, true, new DateTime(2026, 1, 5))
};

var criteria = new AdultSpecification() & new ActiveSpecification();
var adults = people.AsQueryable().Where(criteria).ToList();

Console.WriteLine(string.Join(", ", adults.Select(person => person.Name)));
// Ada, Dan, Eve
```

### 4. 分页、排序与总数

```csharp
var handler = new QueryHandler<Person>(people.AsQueryable())
    .AddCriteria(person => person.Age >= 18)
    .OrderByDescending(person => person.CreatedAt)
    .SetPage(1)
    .SetSize(2);

var page = handler.Query();
var total = handler.GetCount();

Console.WriteLine(string.Join(", ", page.Select(person => person.Name)));
Console.WriteLine($"total={total}");
```

```bash
dotnet run
```

预期输出：

```text
Eve, Dan
total=4
```

`GetCount()` 基于未分页的过滤结果计算，所以是 4 而不是 2；重复调用 `Query()` 不会叠加过滤条件。

### 5. 按属性名动态构建条件

```csharp
var predicate = Lambda.ParsePredicate<Person>("Age", 18, QueryOperator.GreaterThanOrEqual);
var result = people.AsQueryable().Where(predicate).Select(person => person.Name).ToList();
Console.WriteLine(string.Join(", ", result));
```

动态属性名不存在或值无法转换时，`Where(propertyName, value)`/`OrderBy(propertyName, sortType)` 会**静默返回原查询**；来自外部输入时应先做白名单校验。

### 常见失败点

- **`WhereIfNotEmpty` 抛 `InvalidOperationException`**：谓词包含多个条件（`&&`/`||`）时会抛异常；拆成多个调用或改用 `WhereIf`。
- **动态过滤没效果**：属性名大小写/拼写不匹配，或值无法转换为属性类型；检查属性名并先做类型转换。
- **`OrderBy` 没排序**：属性名解析失败或 `SortType` 不是 `Ascending`/`Descending`；使用强类型 `OrderByAscending`/`OrderByDescending`。
- **`Specification<T>` 组合抛 `InvalidCastException`**：`And`/`AndIf` 扩展内部强制转换为 `Specification<T>`；只有接口实现时应改用运算符或自定义组合。

## 相关文档

- [API 参考](API.md)
- [Source README：完整规约、表达式与查询扩展](../../../Source/Euonia.Linq/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/TUTORIAL.md) —— 在模块中注册查询服务
- [Euonia.Application 用户手册](../Euonia.Application/TUTORIAL.md) —— 在应用服务中使用查询
- [Euonia.Validation 用户手册](../Euonia.Validation/TUTORIAL.md) —— 查询参数校验
- [仓库根 README](../../../README.md)
