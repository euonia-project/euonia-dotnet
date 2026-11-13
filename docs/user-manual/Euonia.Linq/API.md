# Euonia.Linq API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Linq` 提供规约模式、谓词/表达式构建器，以及过滤、分页、排序等 `IQueryable` 扩展，用于把查询条件建模为可组合、可复用的对象。

- **包名**：`Euonia.Linq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Linq`
- **完整选项/边界**：[Source/Euonia.Linq/README.md](../../../Source/Euonia.Linq/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `ISpecification<T>` / `Specification<T>` | 规约契约与可组合基类 | `Satisfy()` 返回谓词 |
| `Specification<T>.operator &` / `|` / `!` | 逻辑组合规约 | 返回新的 `Specification<T>` |
| `AndSpecification<T>` / `OrSpecification<T>` / `NotSpecification<T>` | 组合规约实现 | 由运算符创建 |
| `CompositeSpecification<T>` | 聚合多个规约 | `AndAlso` / `OrElse` |
| `SegmentSpecification<TTarget, TProperty, TValue>` | 范围过滤基类 | `RangeBoundary` 决定边界 |
| `RangeBoundary` | 边界包含方式 | `Left`、`Right`、`Both`、`Neither` |
| `PredicateBuilder.True<T>()` / `False<T>()` | 恒真/恒假谓词 | 用于组合起点 |
| `PredicateBuilder.PropertyEqual<TObject, TValue>` | 属性等于值 | 支持可空属性 |
| `PredicateBuilder.GetCompareCondition<T, TValue>` | 属性比较条件 | 支持 `QueryOperator` 关系运算 |
| `Lambda.ParsePredicate<T>(propertyName, value, @operator)` | 按属性名动态生成谓词 | 属性名不存在时抛异常；`Where(propertyName, value)` 才会静默回退 |
| `Lambda.GetConditionCount(expression)` | 统计条件数量 | 忽略字符串字面量中的逻辑文本 |
| `PredicateExpressionBuilder<T>` | 以操作符枚举构建谓词 | `Append` + `ToLambda` |
| `QueryHandler<T>` | 流式查询构建器 | `Query`、`QueryAsync`、`GetCount` |
| `QueryHandler<T>.SetPage(int)` / `SetSize(int)` | 设置页码与每页大小 | 页码默认 `1`；大小默认 `int.MaxValue`，0 返回空 |
| `QueryHandler<T>.OrderByAscending/Descending` / `SetCollator(...)` | 强类型排序或自定义排序 | 排序延迟到构建查询时；`SetCollator` 接受 `Orderable<T>` 或有序查询委托 |
| `QueryCriteria<T>` / `Orderable<T>` | 查询参数（规约/偏移/大小/排序）与链式排序辅助 | `QueryCriteria` 用于仓储查询参数 |
| `QueryableExtensions.Where(ISpecification<T>)` | 按规约过滤 | `Satisfy()` 为 `null` 时返回原查询 |
| `QueryableExtensions.WhereIf(predicate, condition)` | 条件过滤 | `condition=false` 返回原查询 |
| `QueryableExtensions.Between(..., boundary)` | 范围过滤 | 默认 `RangeBoundary.Both` |
| `QueryableExtensions.OrderBy(propertyName, sortType)` | 按属性名排序 | 解析失败返回原查询 |

### 配置说明

`Euonia.Linq` 没有 `Add*` 注册、配置节或 `*Options`；所有行为通过方法参数和对象属性传入，因此本页不提供 appsettings 示例。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整规约、表达式与查询扩展](../../../Source/Euonia.Linq/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 在模块中注册查询服务
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 在应用服务中使用查询
- [Euonia.Validation 用户手册](../Euonia.Validation/API.md) —— 查询参数校验
- [仓库根 README](../../../README.md)
