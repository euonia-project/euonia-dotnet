# Euonia.Linq 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Linq |
| 公共类型 | 29 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Linq

### AndSpecification<T>

**类型**：class
基类 Specification<T>；接口 ISpecification<T>

- new AndSpecification<T>(ISpecification<T> leftSide, ISpecification<T> rightSide)
- ISpecification<T> Left { get; }
- ISpecification<T> Right { get; }
- Expression<Func<T, System.Boolean>> Satisfy()

### CompositeSpecification<TEntity>

**类型**：class
基类 Specification<TEntity>；接口 ISpecification<TEntity>

- new CompositeSpecification<TEntity>(Nerosoft.Euonia.Linq.PredicateOperator composeType)
- CompositeSpecification<TEntity> Add(ISpecification<TEntity> specification)
- CompositeSpecification<TEntity> Add(Func<ISpecification<TEntity>> specification)
- CompositeSpecification<TEntity> AddIf(System.Boolean condition, ISpecification<TEntity> specification)
- CompositeSpecification<TEntity> AddIf(System.Boolean condition, Func<ISpecification<TEntity>> specification)
- CompositeSpecification<TEntity> AddIf(Func<System.Boolean> condition, ISpecification<TEntity> specification)
- CompositeSpecification<TEntity> AddIf(Func<System.Boolean> condition, Func<ISpecification<TEntity>> specification)
- Expression<Func<TEntity, System.Boolean>> Satisfy()
- CompositeSpecification<TEntity> With(ISpecification<TEntity>[] specifications)

### DateSegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.DateTime>；接口 ISpecification<TTarget>

- new DateSegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.DateTime> min, Nullable<System.DateTime> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### DateTimeSegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.DateTime>；接口 ISpecification<TTarget>

- new DateTimeSegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.DateTime> min, Nullable<System.DateTime> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### DecimalSegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.Decimal>；接口 ISpecification<TTarget>

- new DecimalSegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.Decimal> min, Nullable<System.Decimal> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### DirectSpecification<TEntity>

**类型**：class
基类 Specification<TEntity>；接口 ISpecification<TEntity>

- new DirectSpecification<TEntity>(Expression<Func<TEntity, System.Boolean>> predicate)
- Expression<Func<TEntity, System.Boolean>> Satisfy()

### DoubleSegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.Double>；接口 ISpecification<TTarget>

- new DoubleSegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.Double> min, Nullable<System.Double> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### Nerosoft.Euonia.Linq.ExpressionExtensions

**类型**：static class

- static IList<Expression<Func<T, System.Boolean>>> AddIf<T>(IList<Expression<Func<T, System.Boolean>>> expressions, System.Boolean condition, Expression<Func<T, System.Boolean>> expression)
- static IList<Expression<Func<T, System.Boolean>>> AddIf<T>(IList<Expression<Func<T, System.Boolean>>> expressions, Func<System.Boolean> condition, Expression<Func<T, System.Boolean>> expression)
- static Expression<Func<T, System.Boolean>> And<T>(Expression<Func<T, System.Boolean>> first, Expression<Func<T, System.Boolean>> second)
- static System.Linq.Expressions.Expression And(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression Call(System.Linq.Expressions.Expression instance, System.String methodName, System.Linq.Expressions.Expression[] values)
- static System.Linq.Expressions.Expression Call(System.Linq.Expressions.Expression instance, System.String methodName, System.Object[] values)
- static System.Linq.Expressions.Expression Call(System.Linq.Expressions.Expression instance, System.String methodName, System.Type[] paramTypes, System.Object[] values)
- static Expression<Func<T, System.Boolean>> Compose<T>(IEnumerable<Expression<Func<T, System.Boolean>>> expressions, Nerosoft.Euonia.Linq.PredicateOperator type = 0)
- static Expression<Func<T, System.Boolean>> Compose<T>(IEnumerable<Expression<Func<T, System.Boolean>>> expressions, Expression<Func<T, System.Boolean>> seed, Nerosoft.Euonia.Linq.PredicateOperator type = 0)
- static System.Linq.Expressions.Expression Contains(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression EndsWith(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression Equal(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression Equal(System.Linq.Expressions.Expression left, System.Object value)
- static Expression<Func<T, System.Boolean>> Extend<T>(Expression<Func<T, System.Boolean>> first, Expression<Func<T, System.Boolean>> second, Nerosoft.Euonia.Linq.PredicateOperator operator = 0)
- static System.Linq.Expressions.Expression Greater(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression Greater(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression GreaterEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression GreaterEqual(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression Less(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression Less(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression LessEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression LessEqual(System.Linq.Expressions.Expression left, System.Object value)
- static Expression<Func<T, System.Boolean>> Not<T>(Expression<Func<T, System.Boolean>> expression)
- static System.Linq.Expressions.Expression NotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right)
- static System.Linq.Expressions.Expression NotEqual(System.Linq.Expressions.Expression left, System.Object value)
- static System.Linq.Expressions.Expression Operation(System.Linq.Expressions.Expression left, Nerosoft.Euonia.Linq.QueryOperator operator, System.Object value)
- static Expression<Func<T, System.Boolean>> Or<T>(Expression<Func<T, System.Boolean>> first, Expression<Func<T, System.Boolean>> second)
- static System.Linq.Expressions.Expression Or(System.Linq.Expressions.Expression first, System.Linq.Expressions.Expression second)
- static System.Linq.Expressions.Expression Property(System.Linq.Expressions.Expression expression, System.String propertyName)
- static System.Linq.Expressions.Expression Property(System.Linq.Expressions.Expression expression, System.Reflection.MemberInfo member)
- static System.Linq.Expressions.Expression StartsWith(System.Linq.Expressions.Expression left, System.Object value)
- static Expression<TDelegate> ToLambda<TDelegate>(System.Linq.Expressions.Expression body, System.Linq.Expressions.ParameterExpression[] parameters)
- static System.Object Value<T>(Expression<Func<T, System.Boolean>> expression)

### Int32SegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.Int32>；接口 ISpecification<TTarget>

- new Int32SegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.Int32> min, Nullable<System.Int32> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### Int64SegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.Int64>；接口 ISpecification<TTarget>

- new Int64SegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.Int64> min, Nullable<System.Int64> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### ISpecification<TEntity>

**类型**：interface

- Expression<Func<TEntity, System.Boolean>> Satisfy()

### Nerosoft.Euonia.Linq.Lambda

**类型**：static class

- static System.Linq.Expressions.ConstantExpression Constant(System.Linq.Expressions.Expression expression, System.Object value)
- static Expression<Func<T, System.Boolean>> Contains<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> Ends<T>(System.String propertyName, System.String value)
- static Expression<Func<T, System.Boolean>> Equal<T>(System.String propertyName, System.Object value)
- static TAttribute GetAttribute<TAttribute>(System.Linq.Expressions.Expression expression)
- static TAttribute GetAttribute<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression)
- static TAttribute GetAttribute<TProperty, TAttribute>(Expression<Func<TProperty>> propertyExpression)
- static IEnumerable<TAttribute> GetAttributes<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression)
- static System.Int32 GetConditionCount(System.Linq.Expressions.LambdaExpression expression)
- static System.Reflection.MemberInfo GetMember(System.Linq.Expressions.Expression expression)
- static System.Linq.Expressions.MemberExpression GetMemberExpression(System.Linq.Expressions.Expression expression)
- static System.String GetMemberName(System.Linq.Expressions.MemberExpression memberExpression)
- static System.String GetName(System.Linq.Expressions.Expression expression)
- static List<System.String> GetNames<T>(Expression<Func<T, System.Object[]>> expression)
- static System.Linq.Expressions.ParameterExpression GetParameter(System.Linq.Expressions.Expression expression)
- static System.Object GetValue(System.Linq.Expressions.Expression expression)
- static Expression<Func<T, System.Boolean>> Greater<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> GreaterEqual<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> Less<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> LessEqual<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> NotEqual<T>(System.String propertyName, System.Object value)
- static Expression<Func<T, System.Boolean>> ParsePredicate<T>(System.String propertyName, System.Object value, Nerosoft.Euonia.Linq.QueryOperator operator)
- static Expression<Func<T, System.Boolean>> Starts<T>(System.String propertyName, System.String value)

### NotSpecification<TEntity>

**类型**：class
基类 Specification<TEntity>；接口 ISpecification<TEntity>

- new NotSpecification<TEntity>(ISpecification<TEntity> originalSpecification)
- new NotSpecification<TEntity>(Expression<Func<TEntity, System.Boolean>> originalSpecification)
- Expression<Func<TEntity, System.Boolean>> Satisfy()

### Orderable<T>

**类型**：class

- new Orderable<T>(IQueryable<T> enumerable)
- IQueryable<T> Queryable { get; set; }
- Orderable<T> Ascending<TKey>(Expression<Func<T, TKey>> keySelector)
- Orderable<T> Ascending<TKey1, TKey2>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2)
- Orderable<T> Ascending<TKey1, TKey2, TKey3>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2, Expression<Func<T, TKey3>> keySelector3)
- Orderable<T> Descending<TKey>(Expression<Func<T, TKey>> keySelector)
- Orderable<T> Descending<TKey1, TKey2>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2)
- Orderable<T> Descending<TKey1, TKey2, TKey3>(Expression<Func<T, TKey1>> keySelector1, Expression<Func<T, TKey2>> keySelector2, Expression<Func<T, TKey3>> keySelector3)

### OrSpecification<T>

**类型**：class
基类 Specification<T>；接口 ISpecification<T>

- new OrSpecification<T>(ISpecification<T> leftSide, ISpecification<T> rightSide)
- ISpecification<T> Left { get; }
- ISpecification<T> Right { get; }
- Expression<Func<T, System.Boolean>> Satisfy()

### Nerosoft.Euonia.Linq.PredicateBuilder

**类型**：static class

- static Expression<Func<T, System.Boolean>> Create<T>(Expression<Func<T, System.Boolean>> predicate)
- static Expression<Func<T, System.Boolean>> False<T>()
- static Expression<Func<T, System.Boolean>> GetCompareCondition<T, TValue>(T source, System.String propertyName, TValue value, Nerosoft.Euonia.Linq.QueryOperator operator)
- static Expression<Func<T, System.Boolean>> GetContainsCondition<T, TValue>(T source, System.String propertyName, List<TValue> value)
- static TProperty GetProperty<TObject, TProperty>(TObject source, System.String propertyName)
- static Expression<Func<TObject, System.Boolean>> GreaterThanOrEqual<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<TObject, System.Boolean>> PropertyEqual<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<TObject, System.Boolean>> PropertyGreaterThan<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<TObject, System.Boolean>> PropertyInRange<TObject, TValue>(System.String propertyName, TValue[] value)
- static Expression<Func<TObject, System.Boolean>> PropertyLessThan<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<TObject, System.Boolean>> PropertyLessThanOrEqual<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<TObject, System.Boolean>> PropertyNotEqual<TObject, TValue>(System.String propertyName, TValue value)
- static Expression<Func<T, System.Boolean>> True<T>()

### PredicateExpressionBuilder<TEntity>

**类型**：class

- new PredicateExpressionBuilder<TEntity>()
- void Append<TProperty>(Expression<Func<TEntity, TProperty>> property, Nerosoft.Euonia.Linq.QueryOperator operator, System.Object value)
- void Append(System.String property, Nerosoft.Euonia.Linq.QueryOperator operator, System.Object value)
- System.Linq.Expressions.ParameterExpression GetParameter()
- Expression<Func<TEntity, System.Boolean>> ToLambda()

### Nerosoft.Euonia.Linq.PredicateOperator

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| AndAlso |
| OrElse |


### Nerosoft.Euonia.Linq.QueryableExtensions

**类型**：static class

- static IQueryable<TEntity> Between<TEntity, TProperty>(IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, Nullable<System.Int32> min, Nullable<System.Int32> max, Nerosoft.Euonia.Linq.RangeBoundary boundary = 2)
- static IQueryable<TEntity> Between<TEntity, TProperty>(IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, Nullable<System.Double> min, Nullable<System.Double> max, Nerosoft.Euonia.Linq.RangeBoundary boundary = 2)
- static IQueryable<TEntity> Between<TEntity, TProperty>(IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, Nullable<System.Decimal> min, Nullable<System.Decimal> max, Nerosoft.Euonia.Linq.RangeBoundary boundary = 2)
- static IQueryable<TEntity> Between<TEntity, TProperty>(IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> propertyExpression, Nullable<System.DateTime> min, Nullable<System.DateTime> max, System.Boolean includeTime = true, Nullable<Nerosoft.Euonia.Linq.RangeBoundary> boundary = default)
- static Expression<Func<TEntity, System.Boolean>> GetWhereIfNotEmptyExpression<TEntity>(Expression<Func<TEntity, System.Boolean>> predicate)
- static IQueryable<TEntity> OrderBy<TEntity>(IQueryable<TEntity> source, System.String propertyName, Nerosoft.Euonia.Linq.SortType sortType)
- static IQueryable<TEntity> OrderByAscending<TEntity>(IQueryable<TEntity> source, System.String propertyName)
- static IQueryable<TEntity> OrderByDescending<TEntity>(IQueryable<TEntity> source, System.String propertyName)
- static IQueryable<TEntity> Where<TEntity>(IQueryable<TEntity> source, ISpecification<TEntity> criteria)
- static IQueryable<TEntity> Where<TEntity>(IQueryable<TEntity> source, System.String propertyName, System.Object propertyValue)
- static IQueryable<TEntity> Where<TEntity>(IQueryable<TEntity> source, System.String propertyName, System.Object propertyValue, Func<System.Linq.Expressions.Expression, System.Linq.Expressions.ConstantExpression, System.Linq.Expressions.Expression> valueExpression)
- static IQueryable<TEntity> WhereIf<TEntity>(IQueryable<TEntity> source, Expression<Func<TEntity, System.Boolean>> predicate, System.Boolean condition)
- static IQueryable<TEntity> WhereIfNotEmpty<TEntity>(IQueryable<TEntity> source, Expression<Func<TEntity, System.Boolean>> predicate)

### QueryCriteria<TEntity>

**类型**：class

- new QueryCriteria<TEntity>(ISpecification<TEntity> specification, System.Int32 offset, System.Int32 size)
- new QueryCriteria<TEntity>(ISpecification<TEntity> specification, Action<Orderable<TEntity>> collation, System.Int32 offset, System.Int32 size)
- Action<Orderable<TEntity>> Collation { get; set; }
- System.Int32 Offset { get; }
- System.Int32 Size { get; }
- ISpecification<TEntity> Specification { get; }

### QueryHandler<TEntity>

**类型**：class

- new QueryHandler<TEntity>(IQueryable<TEntity> query)
- QueryHandler<TEntity> AddCriteria(Expression<Func<TEntity, System.Boolean>> predicate)
- System.Int32 GetCount()
- Task<System.Int32> GetCountAsync(Func<IQueryable<TEntity>, Task<System.Int32>> action)
- QueryHandler<TEntity> OrderByAscending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
- QueryHandler<TEntity> OrderByDescending<TResult>(Expression<Func<TEntity, TResult>> keySelector)
- IList<TEntity> Query()
- Task<IList<TEntity>> QueryAsync(Func<IQueryable<TEntity>, Task<IList<TEntity>>> action)
- QueryHandler<TEntity> SetCollator(Action<Orderable<TEntity>> order)
- QueryHandler<TEntity> SetCollator(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order)
- QueryHandler<TEntity> SetPage(System.Int32 page)
- QueryHandler<TEntity> SetSize(System.Int32 size)

### Nerosoft.Euonia.Linq.QueryOperator

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Equal |
| StartsWith |
| EndsWith |
| Contains |
| NotContains |
| Like |
| NotLike |
| Is |
| NotEqual |
| GreaterThan |
| GreaterThanOrEqual |
| LessThan |
| LessThanOrEqual |


### Nerosoft.Euonia.Linq.RangeBoundary

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Left |
| Right |
| Both |
| Neither |


### SegmentSpecification<TTarget, TProperty, TValue>

**类型**：class
接口 ISpecification<TTarget>

- Expression<Func<TTarget, System.Boolean>> Satisfy()

### Nerosoft.Euonia.Linq.SortType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Unspecified |
| Ascending |
| Descending |


### Specification<TEntity>

**类型**：class
接口 ISpecification<TEntity>

- static Specification<TEntity> operator BitwiseAnd(Specification<TEntity> leftSideSpecification, Specification<TEntity> rightSideSpecification)
- static Specification<TEntity> operator BitwiseOr(Specification<TEntity> leftSideSpecification, Specification<TEntity> rightSideSpecification)
- static System.Boolean operator False(Specification<TEntity> specification)
- static Specification<TEntity> operator LogicalNot(Specification<TEntity> specification)
- static System.Boolean operator True(Specification<TEntity> specification)
- Expression<Func<TEntity, System.Boolean>> Satisfy()

### Nerosoft.Euonia.Linq.SpecificationExtensions

**类型**：static class

- static Specification<T> And<T>(ISpecification<T> first, ISpecification<T> second)
- static Specification<T> AndIf<T>(ISpecification<T> first, System.Boolean condition, Func<ISpecification<T>> second)
- static Specification<T> AndIf<T>(ISpecification<T> first, Func<System.Boolean> selector, Func<ISpecification<T>> second)

### TimeSpanSegmentSpecification<TTarget, TProperty>

**类型**：class
基类 SegmentSpecification<TTarget, TProperty, System.TimeSpan>；接口 ISpecification<TTarget>

- new TimeSpanSegmentSpecification<TTarget, TProperty>(Expression<Func<TTarget, TProperty>> property, Nullable<System.TimeSpan> min, Nullable<System.TimeSpan> max, Nerosoft.Euonia.Linq.RangeBoundary boundary)

### TrueSpecification<TEntity>

**类型**：class
基类 Specification<TEntity>；接口 ISpecification<TEntity>

- new TrueSpecification<TEntity>()
- Expression<Func<TEntity, System.Boolean>> Satisfy()
