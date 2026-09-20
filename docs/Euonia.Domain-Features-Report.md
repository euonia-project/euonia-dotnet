# Euonia.Domain 实用功能增强报告

## 概述
在 Euonia.Domain（延续上一轮值对象相等性修复）基础上新增三类实用功能：**ValueObject 表现力增强**、**Aggregate 事件模式便捷方法**、以及**事件关联标识（CorrelationId）透传**。全部新增功能均有回归测试覆盖（新建 `ValueObjectFeatureTest` / `AggregatePatternTest` / `EventCorrelationTest`，Domain.Tests 由 5 用例扩至 15）。

**追加轮（Primitives，Domain.Tests 15 → 22）**：新增**值对象标记接口 `IValueObject`**、**实体身份等值（`Entity<TKey>` 的 `Equals`/`GetHashCode`/`==`/`!=`）与 `IsTransient()`**、以及 **Aggregate 幂等注册与类型筛选**。详见下方「七、基元强化」章节。

验证结果：`Euonia.Build.slnx` **0 错误 0 警告**；`Euonia.Test.slnx` **0 错误**，仅剩 1 条既有无关警告（Application.Tests）；全仓 14 个测试程序集 `dotnet exec` 全绿（Domain.Tests **22/22**）。

---

## 一、ValueObject 增强（`Source/Euonia.Domain/ValueObject.cs`）

### 1. 集合/数组属性改为「按元素序列」比较（核心）
上一轮修复后，单值属性（含 null）已按值比较，但**集合属性仍走引用比较**：属性是 `int[]`/`List<T>` 时，`left.Equals(right)` 是相同引用判断，两个内容相同但实例不同的集合判为不相等——破坏值对象“按属性值整体判等”的语义。本次抽出 `ValuesEqual`：

```csharp
if (left is IEnumerable sequenceLeft && right is IEnumerable sequenceRight)
{
    return sequenceLeft.Cast<object>().SequenceEqual(sequenceRight.Cast<object>());
}

return left is TValueObject ? ReferenceEquals(left, right) : left.Equals(right);
```

行为变化：
- `[1,2,3]` 与 `[1,2,3]`（不同实例）→ **相等**；
- `[1,2,3]` 与 `[3,2,1]` → 不相等（保持序列有序语义）；
- 嵌套值对象仍在属性值上递归等值（同一类型 `TValueObject` 自身保持引用比较，避免自引用结构死循环）。

### 2. `GetHashCode` 与序列等值对齐
原实现对集合属性直接取 `value.GetHashCode()`（引用哈希），与新的序列等值不一致。新增 `GetValueHashCode`：对 `IEnumerable` 按元素逐个累加，保证序列相等 ⇒ 哈希相等。

### 3. `ToString()` 结构化输出
新增质数级可读输出，逐属性打印取值（null 显示为 `null`，序列显示为 `[…]`）：

```
Name = foo
Age = 42
```

方便调试与日志（Entity 已有 `ToString`，VO 一并补齐）。

---

## 二、Aggregate 便捷模式（`Source/Euonia.Domain/Aggregate.cs`）

### 1. `GetAndClearEvents()`
把“读取待处理事件 + 清空”两步合并为一次调用，正是**工作单元（UoW）统一收集并分发领域事件**前的标准动作，避免先 `GetEvents()` 再 `ClearEvents()` 之间被重复触发。

### 2. `LoadFromHistory(IEnumerable<DomainEvent>)`
从事件存储重放聚合：对每个事件按其**运行时类型**查处理器并调用（与 `Register<T>` 的类型键匹配），**不会重新入队**、不追加到 `_events`——与 `Apply` 语义一致，适配事件溯源重建。

### 3. `HasEvents` / `EventsCount`
快速判断与计数待处理事件，减少 `GetEvents().Count > 0` 的样板代码。

---

## 三、事件关联标识透传（Event.CorrelationId）

### 背景
领域事件在跨进程/跨服务传播时常用关联标识（CorrelationId）串联因果链。仓内其他层已有共识：`Euonia.Bus.RoutedMessage.CorrelationId`、`MessageContext.CorrelationId`、`MessageHeaders.CorrelationId`、`AuditingRecord.CorrelationId`，唯独 Domain `Event` 没有自己的关联标识。

### 改动
- **`Event`**：新增 `CorrelationId` 属性（存放在 `Properties` 元数据字典，键 `nerosoft.euonia.internal.event.correlation`，与 `EventId` 同源）；**构造时默认取 `EventId`**，保证事件无需额外配置即有稳定关联值；
- **`EventAggregate`**：新增 `CorrelationId` 属性；
- **`DomainEvent.GetEventAggregate()`**：透传 `CorrelationId`，使持久化的事件聚合记录同样携带关联值。

```csharp
// Event 构造
Properties[PROPERTY_ID] = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();
Properties[PROPERTY_CORRELATION_ID] = Properties[PROPERTY_ID];
```

---

## 四、回归测试补全（Domain.Tests 5 → 15）

| 测试文件 | 用例覆盖 |
| --- | --- |
| `ValueObjectFeatureTest.cs`（新增） | `ToString` 输出包含取值；集合内容相同 ⇒ 相等且哈希一致；集合顺序不同 ⇒ 不相等 |
| `AggregatePatternTest.cs`（新增） | `GetAndClearEvents` 返回待处理事件后清空；空队列返回空；`LoadFromHistory` 触发处理器但不记录；`HasEvents`/`EventsCount` 随增删变化 |
| `EventCorrelationTest.cs`（新增） | `CorrelationId` 默认等于 `EventId`；可显式赋值；`GetEventAggregate` 透传 |
| `ValueObjectEqualityTest.cs`（既有） | 保持原 5 用例（null 相等、单侧 null、哈希契约、泛型基类强转、HashSet）全绿 |

> 本次功能以“先写测试、后补实现”进行验证：新 API 尚不存在时测试先呈编译失败（红灯），实现后 15/15 转绿。

## 七、基元强化（追加轮，Domain.Tests 15 → 22）

### 1. 值对象标记接口 `IValueObject`（`Seedwork/IValueObject.cs`）
非泛型标记接口，供泛型约束（如仓储、映射器）标识值对象类型，而不必依赖具体的 `ValueObject<T>` 基类。`ValueObject<T>` 现实现 `IValueObject`。

```csharp
public interface IValueObject { }

public class ValueObject<TValueObject> : IValueObject, IEquatable<TValueObject> ...
```

### 2. 实体身份等值（`Entity<TKey>`）
DDD 中实体以「运行类型 + 标识符」定义身份。现为 `Entity<TKey>` 提供：
- `Equals(object)`：同运行类型且 `Id` 相等 ⇒ 相等（`null` 安全，走 `default` 值比较）；
- `GetHashCode()`：`HashCode.Combine(GetType(), Id)`，与等值一致（同 Id 同类型 ⇒ 同哈希）；
- `==` / `!=` 运算符：委托 `Equals`，`null` 安全；
- `IsTransient()`：`Id` 为默认值时返回 `true`（未持久化），典型用于工作单元决定 Insert vs Update。

### 3. Aggregate 幂等注册与类型筛选（`Aggregate<T>`）
- **`Register<T>` 幂等**：原实现 `_handlers.Add` 对同一事件类型重复注册抛 `ArgumentException`；改为索引赋值 `_handlers[typeof(T)] = ...`，**后注册覆盖先注册**，支持基类/派生类分层注册同一事件处理器，不打断多类型注册。
- **`GetEvents<TEvent>()`**：按事件类型筛选待处理事件，利于只分发某一类领域事件。

### 4. 回归测试（`PrimitivesFeatureTest.cs`，新增 7 用例）
| 用例 | 覆盖 |
| --- | --- |
| `ValueObject_Implements_IValueObject_Marker` | 值对象可被 `IValueObject` 引用 |
| `Entity_SameTypeSameId_AreEqual` | 同类型同 Id ⇒ 相等、`==`、同哈希 |
| `Entity_DifferentId_AreNotEqual` | Id 不同 ⇒ 不相等 |
| `Entity_DifferentType_SameId_AreNotEqual` | 同 Id 不同类型 ⇒ 不相等（身份含类型） |
| `Entity_IsTransient_ReflectsDefaultId` | 默认 Id ⇒ 瞬态；有值 ⇒ 非瞬态 |
| `Aggregate_Register_IsIdempotent_LastWins` | 同类型注册两次 ⇒ 后注册生效 |
| `Aggregate_GetEvents_OfType_Filters` | 类型筛选返回对应事件且总数不变 |

> 追加轮同样采用「先写测试、后补实现」：新 API 尚不存在时测试先呈编译失败（红灯，8 处编译错误），实现后 22/22 转绿。

## 验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx`（`--no-incremental`） | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx`（`--no-incremental`） | 0 错误，1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Domain.Tests`（`dotnet exec`） | **22/22** |
| 其余 13 个测试程序集（`dotnet exec`） | 全绿（Application 21 / Bus.InMemory 10 / Bus.RabbitMq 10 / Bus 22 / Caching.Default 4 / Caching.Memory 9 / Caching.Runtime 9 / Core 78 / Linq 38 / Mapping.Automapper 3 / Mapping.Mapster 3 / Osba 131 / Pipeline 10） |

> 本机再次确认：`dotnet test`（xUnit.v3 MTP 适配器）的 “Zero tests ran”/exit 5 误报仍然存在，统一用 `dotnet exec <TestDll>` 跑真实测试，结果可靠。

## 遗留观察（未改动，供后续决策）
- **`Aggregate<TKey>.Register<T>` 幂等语义为「后注册覆盖」**：新增用例验证；如需「单事件多处理器依次触发」需改为按类型列表追加，不在本次范围。
- **实体等值含运行类型**：引入 `GetType() == other.GetType()`，同 Id 的基类/派生类实体判为不同身份——符合 DDD 但若与 ORM 导航属性混用需注意代理类型不一致的问题。
- **`GetAndClearEvents()` 返回 `_events.ToList()` 快照**：进入队列的事件随后被清空，返回列表不再被聚合内部修改，调用方可安全置入消息总线。
- **`Event.CorrelationId` 默认等于 `EventId`**：如需“同一业务单元共享同一关联值”，请在发布事件前显式赋值，或将某服务级 `CorrelationId` 传入；本次仅提供事件自身的默认值。