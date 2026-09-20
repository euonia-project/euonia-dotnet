# Euonia.Domain 修复报告

## 概述
本次针对 `euonia-net` 仓库中 Euonia.Domain 项目展开分析（延续 Euonia.Bus → Euonia.Pipeline → Euonia.Caching 修复轮次），聚焦其基元类型 `ValueObject<TValueObject>` 的**值相等语义缺陷**：值对象是可空成员场景下不相等、以及 `Equals(object)` 对非 `TValueObject` 实例的非法强转。两处都直接破坏值对象的契约（DDD 中值对象按属性比较，且相等的值对象哈希必须一致）。

- **可空成员相等性错误**（核心，1 处）：属性两端为 `null` 时被判定为不相等；
- **`Equals(object)` 强转异常**（1 处）：`obj is ValueObject<TValueObject> item && Equals((TValueObject)item)` 在其它同类实例上抛 `InvalidCastException`；
- **回归测试补全**（1 个新测试项目 + 5 个测试）：新建 `Tests/Euonia.Domain.Tests` 并加入 `Euonia.Test.slnx`。

验证结果：`Euonia.Build.slnx` **0 错误 0 警告**；`Euonia.Test.slnx` **0 错误**，仅剩 1 条与 Domain 无关的既有警告（Application.Tests）；全仓 14 个测试程序集 `dotnet exec` 全绿，其中 Domain.Tests **5/5**（新增）。

---

## 一、核心问题：`ValueObject<TValueObject>.Equals` 把“两端都是 null”判为不相等

涉及文件：`Source/Euonia.Domain/ValueObject.cs:39-42`（根因）

### 具体问题
`Equals(TValueObject other)` 逐属性比较时采用：

```csharp
if (left == null || right == null)
{
    return false;
}
return left is TValueObject ? ReferenceEquals(left, right) : left.Equals(right);
```

值对象的核心语义是“按属性取值结构比较”。当某个成员在**两个实例上都是 `null`**（例如可空引用类型、可空值类型或尚未赋值的可选字段）时，两侧取值一致，理应相等——但旧逻辑直接返回 `false`。后果：

1. `personA == personB`、`personA.Equals(personB)` 均为 `false`，即便二者所有属性完全相同；
2. 破坏 `GetHashCode` 一致性契约的另一面：两个“真正相等”的对象在 `HashSet`/`Dictionary`/`Distinct()` 中互相找不到（本项目 `GetHashCode` 对 null 的贡献是确定的，会一致，但 `Equals` 判错导致集合成员查找失败）；
3. 违反 DDD 值对象不变量的经典前提——等价性应由值而非实例身份决定。

### 红灯复现
新增回归测试 `Tests/Euonia.Domain.Tests/ValueObjectEqualityTest.cs`（`PersonDescriptor : ValueObject<PersonDescriptor>`，含 `string Name` 与 `int Age`）。修复前运行：

```
Nerosoft.Euonia.Domain.Tests.ValueObjectEqualityTest.Test_Equal_WhenAllPropertiesMatch_IncludingNulls [FAIL]
  Assert.True() Failure
  Expected: True
  Actual:   False

Nerosoft.Euonia.Domain.Tests.ValueObjectEqualityTest.Test_Contains_InHashSet_WithNullProperties [FAIL]
  Assert.Contains() Failure: Item not found in set
```

即 `Name = null, Age = 42` 的实例与自身结构的副本比较不相等、HashSet 找不到——缺陷精确复现。

### 修改依据与内容
两侧同为 `null` 时视为“取值一致”而相等，仅一侧为 `null` 时判不相等（避免 `left == right` 引用重叠的歧义，此处 left/right 为 `object` 装箱，`==` 在该上下文等价于两侧都为空）：

```csharp
if (left == null || right == null)
{
    return left == right;
}
```

---

## 二、`Equals(object)` 对非 `TValueObject` 实例强转抛 `InvalidCastException`

涉及文件：`Source/Euonia.Domain/ValueObject.cs:66`（根因）

### 具体问题
原实现：

```csharp
return obj is ValueObject<TValueObject> item && Equals((TValueObject)item);
```

`obj is ValueObject<TValueObject>` 只验证“是同一个泛型基类类型”，随后直接 `(TValueObject)` 强转。当 `obj` 是 `ValueObject<TValueObject>` 泛型基类的直接实例（该类为 `public class`，可直接构造）、或从同一泛型基类派生的其它类型时，强转就抛 `InvalidCastException`，而不是按值对象语义返回 `false`。

### 红灯复现
```csharp
object baseValueObject = new ValueObject<PersonDescriptor>();
Assert.False(left.Equals(baseValueObject));
```
修复前运行：
```
Nerosoft.Euonia.Domain.Tests.ValueObjectEqualityTest.Test_EqualsObject_WithGenericBase_NoCastException [FAIL]
  System.InvalidCastException :
  Unable to cast object of type 'Nerosoft.Euonia.Domain.ValueObject`1[...PersonDescriptor]'
  to type '...PersonDescriptor'.
```

### 修改依据与内容
先做更精确的运行时类型判定 `is TValueObject` 再调用类型化 `Equals`，天然避免非法强转，同时保持“同类型才比较”的语义：

```csharp
return obj is TValueObject item && Equals(item);
```

（`ValueObject<TValueObject>` 的约束 `TValueObject : ValueObject<TValueObject>` 保证 `item` 必定可安全传给 `Equals(TValueObject)`。）

---

## 三、回归测试补全

### 新建测试项目 `Tests/Euonia.Domain.Tests`
- 仓库此前**没有 Euonia.Domain 的测试项目**（`Euonia.Domain.Tests.csproj` 为本次新增），已加入 `Euonia.Test.slnx`；
- 引用 `Source/Euonia.Domain` 与 `Source/Euonia.Core`，模板沿用 `Tests/common.props`（net10.0、xUnit.v3、Moq 等）。

### 新增 `ValueObjectEqualityTest.cs`（5 个用例）
| 用例 | 验证点 |
| --- | --- |
| `Test_Equal_WhenAllPropertiesMatch_IncludingNulls` | `==` / `Equals(TValueObject)` / `Equals(object)` / `!=` 对 null 属性一致 |
| `Test_NotEqual_WhenOneSideHasNull_OtherHasValue` | 一侧 null、一侧有值 → 判不相等（对照，确保不误伤） |
| `Test_HashCode_ConsistentWithEquality` | 相等对象哈希一致（`GetHashCode` 契约） |
| `Test_EqualsObject_WithGenericBase_NoCastException` | 泛型基类实例不再抛强转异常 |
| `Test_Contains_InHashSet_WithNullProperties` | `HashSet` 成员查找与值对象相等语义一致 |

---

## 四、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx`（`--no-incremental`） | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx`（`--no-incremental`） | 0 错误，1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Domain.Tests`（`dotnet exec`，新增） | **5/5** |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Application 21 / Bus.InMemory 10 / Bus.RabbitMq 10 / Bus 22 / Caching.Default 4 / Caching.Memory 9 / Caching.Runtime 9 / Core 78 / Linq 38 / Mapping.Automapper 3 / Mapping.Mapster 3 / Osba 131 / Pipeline 10） |

> 注：本机 `dotnet test`（xUnit.v3 MTP 适配器）会报 “Zero tests ran”/exit 5 的误报，故统一用 `dotnet exec <TestDll>` 跑真实测试，与既有轮次一致。

## 五、遗留观察（未改动，供后续决策）
- **`Aggregate<TKey>.Register<T>` 用 `_handlers.Add`**：同一事件类型重复注册会抛 `ArgumentException`。若某聚合根通过基类/派生类链对同一事件注册了多个处理器，会直接异常；单处理器语义可接受，但若需支持多处理器需改为“列表追加”，留待后续。
- **`AggregateExtensions.Find` 的 `t.Id.Equals(id)`**：`TKey` 为引用类型（如 `string`）且某实体 `Id` 为 `null` 时会 `NullReferenceException`；此用法的合理场景有限，仅在扩展方法层面可改进为 `EqualityComparer<TKey>.Default`。
- **`AuditingOptions` 为空类、`Caching/IEntityCache` 为空接口**：均为占位/契约桩，无行为可测。