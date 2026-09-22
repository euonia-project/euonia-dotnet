# Euonia.Core 修复报告

## 概述
本次针对 `euonia-net` 仓库中 Euonia.Core 项目展开审计与修复，共修改 **24 个源文件**，并新增 **51 个回归测试**。问题分为以下类别：
- **系统 / ID 生成**（3 处）：ULID 编码、Snowflake 递增、随机 ID 质量；
- **系统 / 核心类型**（4 处）：`ObjectId` 值语义、`LikeOperator` 匹配、`ObjectPool` 归还竞争、`WeakEventManager` 线程安全、`DisposableObject` 终结器；
- **线程 / 异步协调**（2 处）：`AsyncManualResetEvent` 取消、`DeferralManager` 挂起；
- **安全 / 身份**（1 处）：`UserPrincipal` null 安全与角色来源；
- **扩展方法**（3 处）：范围判断、集合相等、认证校验；
- **校验特性**（2 处）：`CollectionCountAttribute`、`GuidAttribute`；
- **异常与集合**（4 处）：`BusinessException`、`AsyncCollectionDisposable`、`PageableCollection`、`TreeView`、`EquatableReadOnlyList`；
- **反射工具**（3 处）：`AssemblyHelper`、`EnumHelper`、`Reflect`；
- **回归测试**。

所有修改均通过构建与测试验证：解决方案构建 **0 错误 0 警告**；10 个测试项目共 **285 个测试全部通过**（Euonia.Core.Tests 由 25 → 78）。

---

## 一、系统 / ID 生成

### 1. `Source/Euonia.Core/System/UlidGenerator.cs`
**具体问题**：`Encode` 按「每字节取高 5 位 / 低 5 位」切分编码（16 字节 → 32 字符），既不符合 ULID 规范（应为 26 个 Crockford Base32 字符），时间戳 / 随机数的位序也错乱，生成的 ID 无法被标准 ULID 解析 / 排序。

**修改依据**：ULID 规范（`github.com/ulid/spec`）要求把 48 位时间戳 + 80 位随机数拼成 128 位，按每 5 位一组、从最高位向低位流式编码成 26 个字符。改为缓冲 17 字节（容纳跨字节边界位）后按位偏移 `0..128` 步进 5 位编码，输出标准 26 字符 ULID。

### 2. `Source/Euonia.Core/System/SnowflakeId.cs`
**具体问题**：`_machineId/_datacenterId/_sequence/_lastTimestamp` 及锁均为 `static`，跨实例共享单一可变状态；`GetNextTimestamp` 只在 `timestamp <= lastTimestamp` 时**重新取一次**，快速连续调用 / 并发下可能产出相同时间戳，导致 ID 不唯一。

**修改依据**：Snowflake 算法要求时间戳严格单调递增（同毫秒由 sequence 或时钟等待去重）；状态应属实例而非全局。将字段与锁改为实例级；`GetNextTimestamp` 改为 `while (timestamp <= lastTimestamp) { Thread.Sleep(1); ... }` 自旋，保证严格大于上一次。

### 3. `Source/Euonia.Core/System/RandomId.cs`
**具体问题**：用 `new Random(DateTime.UtcNow.Ticks)` 作为种子 —— ID 可预测、熵低（同一毫秒重复调用生成相同 key）；并对**共享静态字符数组**做 10 万次交换，并发下存在数据竞争。

**修改依据**：ID 用于主键 / 混淆场景，需要不可预测、无共享可变状态、每次独立随机。改为固定常量字符集 + 本地副本，用 `RandomNumberGenerator`（加密级随机源）生成种子，再执行 Fisher-Yates 洗牌保证均匀分布。

---

## 二、系统 / 核心类型

### 4. `Source/Euonia.Core/System/ObjectId.cs`
**具体问题**：
- `==`/`!=` 用 `EqualityComparer<object>.Default` —— `ObjectId(5)`（int）与 `ObjectId(5L)`（long）被判不相等；
- `implicit operator long/int` 直接 `(long)Value` / `(int)Value` 强转，Value 为其他数值类型时抛 `InvalidCastException`；
- `GetHashCode` 用 `HashCode.Combine(Value)`，与 Equals 语义不一致，破坏字典 / 哈希集合的「相等则哈希相等」契约。

**修改依据**：`ObjectId` 为值语义结构体，底层 `Value` 可能为 int/long/Guid/string，相等与哈希必须类型无关地一致。新增 `ValueEquals`：先行 `Equals` 短路，数值类型统一 `Convert.ToDecimal(InvariantCulture)` 规范化比较（并处理溢出转 false）；`==`/`!=`/`Equals(object)` 均走它。转换改用 `Convert.ToInt64/ToInt32(InvariantCulture)`；`GetHashCode` 对数值走 decimal 规范化哈希。文件头部补充 `using System; using System.Globalization;`。

### 5. `Source/Euonia.Core/System/LikeOperator.cs`
**具体问题**：原有「快速路径」分支判断错误，核心匹配依赖递归回溯，遇到多个 `*` 通配符时退化，结果不可靠，还可能造成栈溢出 / 超时。

**修改依据**：通配符匹配应采用带回溯的贪心双指针线性算法（对应 SQL LIKE 语义）。删除快速路径，重写 `LikeStringCore`：`starIndex`/`starMatchContentIndex` 记录最近 `*` 位置以便回溯，`*` 匹配零或多个、`?` 匹配单字符，一次遍历完成；保留 `EqualsCharDelegate` 的大小写 / 区域性比较策略。

### 6. `Source/Euonia.Core/System/ObjectPool.cs`
**具体问题**：`Return` 中 `if (_items[i] == null) { _items[i] = value; return; }` 存在竞争：两个线程同时归还到同一空位时，其中一个对象被覆盖而**丢失**。

**修改依据**：归还必须是原子的「读-判-写」。改为 `Interlocked.CompareExchange(ref _items[i], value, null) == null` 判断槽位确实从 null 变为 value，实现无锁、不丢对象。

### 7. `Source/Euonia.Core/System/WeakEventManager.cs`
**具体问题**：`AddEventHandler`/`RemoveEventHandler`/`GetEventHandler`/`RemoveEventHandlers` 直接操作共享 `Dictionary<string, List<Subscription>>` 无锁；读取时并发移除可抛 `InvalidOperationException` 或丢失事件；静态处理器（`Target == null`）判断方式不稳；`Subscription.GetHashCode` 组合方式可能碰撞/异常。

**修改依据**：弱事件表本质是并发访问的集合，读改写必须串行化。新增 `_lock`，所有发布 / 移除 / 快照操作加锁；用 `SubscriberEquals` 统一比较订阅者；`Subscription.GetHashCode` 改为 `(Subscriber?.GetHashCode() ?? 0) ^ Handler.GetHashCode()`。

### 8. `Source/Euonia.Core/System/DisposableObject.cs`
**具体问题**：终结器 `~DisposableObject()` 调完 `Dispose(false)` 后又触发 `Disposed` 事件 —— 在终结器线程回调托管订阅者，产生不可预知副作用，甚至导致对象复活（resurrection）。

**修改依据**：.NET 终结器纪律要求 Finalizable 路径只释放非托管资源，不触碰托管事件。移除此处的事件引发，终结器仅执行 `Dispose(false)`。

---

## 三、线程 / 异步协调

### 9. `Source/Euonia.Core/Threading/Coordination/AsyncManualResetEvent.cs`
**具体问题**：`WaitAsync(CancellationToken)` 内部写的是 `var waitTask = WaitAsync(cancellationToken); ...` —— 即**递归调用自身**，既不能真正应用取消，行为也偏离预期。

**修改依据**：取消应作用于底层的等待任务。改为先 `WaitAsync()` 拿到等待任务，若未完成再以 `waitTask.WaitAsync(cancellationToken)` 转发取消（netstandard2.1 由 `Extensions.Threading` 扩展提供，net10 用内置 `Task.WaitAsync`）。

### 10. `Source/Euonia.Core/Threading/Coordination/DeferralManager.cs`
**具体问题**：`_countdownEvent = new AsyncCountdownEvent(1)` 字段初始化让计数器启动即为 1；即使从未调用 `GetDeferral` 请求延迟，`WaitForDeferralsAsync` 也会等待这个**从未被 `Signal`** 的计数 —— 永久挂起。

**修改依据**：与 Nito `DeferralManager` 设计一致 —— 计数应反映「真实活跃延迟数」。移除初始化，改为 `null`；首次 `IncrementCount`（取延迟）时惰性创建（计数 1）；从未请求延迟时 `WaitForDeferralsAsync` 直接返回已完成任务。

---

## 四、安全 / 身份

### 11. `Source/Euonia.Core/Security/UserPrincipal.cs`
**具体问题**：
- `Tenant`、`FindClaim`、`FindClaims`、`GetAllClaims` 直接访问可能为 null 的 `Claims`，抛 `NullReferenceException`；
- `IsInRole` 依赖 `ClaimsPrincipal.IsInRole(role)`，只按身份配置的角色类型查找，且空主体时崩溃，自定义 `role` 声明（`UserClaimTypes.Role`）不被识别；
- `IsInRoles` 对 null 参数抛 NRE；
- 逗号分隔版未去除空白项。

**修改依据**：角色来源应同时覆盖 `UserClaimTypes.Role` 与各身份 `RoleClaimType`（标准角色 URI）；空主体应返回「非在角色中」而非抛异常。上述访问全部改为 null 安全（空数组兜底）；`IsInRole` 改为基于新增的 `FindRoleClaims()` 汇总两种角色来源；`IsInRoles` 保护 null 输入；分隔版 `Trim` 后过滤空项。

---

## 五、扩展方法

### 12. `Source/Euonia.Core/Extensions/Extensions.Compare.cs`
**具体问题**：`IsNotInRange` 写成 `value.CompareTo(minValue) < 0 && value.CompareTo(maxValue) > 0` ——「小于下界**且**大于上界」恒为 false，方法永远返回 false。

**修改依据**：区间外 = 低于下界 **或** 高于上界，与 `IsBetween` 互补。`&&` → `||`。

### 13. `Source/Euonia.Core/Extensions/Extensions.Collection.cs`
**具体问题**：
- `ForEach`、`Contains`、`Join`、`ToView`、`Paginate`、`Convert`、`Shuffle` 对 null 参数抛 `NullReferenceException`；
- `Contains(string, StringComparison)` 内部调用泛型的 `t.Equals(value, ...)`，StringComparison 语义不生效；
- `Equals<T>` 用 `source.All(dest.Contains)`，不尊重重复项 —— `{1,1,2}` 与 `{1,2,2}` 被判相等；
- `Shuffle` 使用共享静态 `Random`，并发竞争且可预测。

**修改依据**：参数校验应抛 `ArgumentNullException`；比较需显式 `string.Equals(t, value, comparison)`；集合相等应为**多重集意义**（忽略顺序、尊重次数），改为排序后逐位比较（O(n log n)）；随机数需每线程独立（`[ThreadStatic]`）。

### 14. `Source/Euonia.Core/Extensions/Extensions.Claims.cs`
**具体问题**：`EnsureAuthenticated` 的条件是 `!user.IsAuthenticated && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(UserId)`—— 只要用户名与 ID 均非空，**即使未认证也不抛异常**，语义反向 / 不完整。

**修改依据**：该方法语义是「强制已认证」，只应以 `IsAuthenticated` 为准。改为 `if (!user.IsAuthenticated) throw ...`。

---

## 六、校验特性（DataAnnotations）

### 15. `Source/Euonia.Core/Annotations/CollectionCountAttribute.cs`
**具体问题**：
- 各构造函数缺少范围校验，负数 `minimum`、`maximum < minimum` 被接受；
- `IsValid` 只认 `ICollection`；把 `string` 当集合按字符数校验；对 `IReadOnlyCollection` 等泛型集合既无 `Count` 反射也无校验，直接「成功」。

**修改依据**：约束应在构造期即显式失败；计数需覆盖泛型最小公共接口，且字符串不应被当作字符集合校验。新增 `ValidateRange`（minimum ≥ 0、maximum ≥ minimum）并在全部 6 个构造函数调用；`IsValid` 重写为 `TryGetCount` —— `ICollection` → 反射 `Count` 属性 → 枚举计数；`string` 旁路保持「非集合」语义。

### 16. `Source/Euonia.Core/Annotations/GuidAttribute.cs`
**具体问题**：只接受可被 `Guid.TryParse` 解析的字符串；当属性类型本身是 `Guid`（无需字符串化）时被判无效。

**修改依据**：校验应覆盖强类型 GUID 值。`IsValid` 增加 `Guid guid when guid != Guid.Empty => Success` 分支；null 保持有效（配合 `[Required]` 使用）。

---

## 七、异常与集合

### 17. `Source/Euonia.Core/Exceptions/BusinessException.cs`
**具体问题**：`BusinessException(string code)` 只存 `_code`，`Message` 为空字符串，二义。

**修改依据**：按业务异常惯例（ABP 风格）仅传 code 时消息即 code，便于日志与界面展示。改为 `: base(code)`。

### 18. `Source/Euonia.Core/Disposing/Asynchronous/AsyncCollectionDisposable.cs`
**具体问题**：`Create` 工厂返回类型标为 `CollectionDisposable` 且参数为 `IDisposable[]`，与异步释放语义不符，无法基于 `IAsyncDisposable` 集合创建。

**修改依据**：工厂方法应返回自身类型、参数与幂等异步释放语义一致。改为返回 `AsyncCollectionDisposable`，参数为 `IAsyncDisposable[]` / `IEnumerable<IAsyncDisposable>`。

### 19. `Source/Euonia.Core/Collections/PageableCollection.cs`
**具体问题**：
- `PageCount` 用 `(int)Math.Ceiling((double)TotalCount / PageSize)`，double 精度在超大计数下可能出错；
- `StartPosition`/`EndPosition` 无参数校验，`PageSize <= 0` 或 `PageNumber < 1` 时产生负数 / 非法位置；
- `EndPosition` 在 `TotalCount <= 0` 时返回负值。

**修改依据**：分页公式用纯整数运算 `TotalCount/PageSize + (余数 ? 1 : 0)` 避免精度损失，`TotalCount <= 0` 返回 0；非法参数显式抛 `InvalidOperationException`；`EndPosition` 裁剪到 `TotalCount`。

### 20. `Source/Euonia.Core/Collections/TreeView.cs`
**具体问题**：`Children` 集合属性无初始化，默认实例上 `Children.Add(...)` 或遍历时抛 `NullReferenceException`。

**修改依据**：对象模型惯例 —— 集合属性默认空集合，避免调用方每次判空。`= new List<TreeView<TEntity>>()`。

### 21. `Source/Euonia.Core/Collections/EquatableReadOnlyList.cs`
**具体问题**：
- `default(EquatableReadOnlyList<T>)`（`_array == null`）在 `Count`、索引器、`GetEnumerator`、`Equals` 上全部 NRE；
- `GetHashCode` 用 `HashCode.Combine` 逐项，与 `Equals`（序列比较）口径不一致。

**修改依据**：值类型默认实例必须可用；`IEquatable` 与哈希口径需一致。新增 `Items => _array ?? Array.Empty<T>()` 兜底，所有成员统一走 `Items`；`GetHashCode` 改为稳定多项式 `17*31 + itemHash`；枚举去掉无谓的 `.As<IEnumerable<T>>()` 转型。

---

## 八、反射工具

### 22. `Source/Euonia.Core/Reflection/AssemblyHelper.cs`
**具体问题**：
- `_typeCache.GetOrAdd(assembly, assembly.GetTypes())` 传的是**已求值**的值（`GetOrAdd(key, value)` 重载）——命中缓存时仍强制执行昂贵的 `GetTypes()`；
- `ReflectionTypeLoadException` 时返回的数组含 null 项；
- `GetAssemblyFiles` 用 `EndsWith(".dll")` 扩展名判断，大小写敏感；
- `GetDefinedTypes` 同样在缓存路径上强制求值。

**修改依据**：`ConcurrentDictionary` 的 valueFactory 重载才能延迟求值；部分加载失败应只返回可加载类型。改为 `GetOrAdd(assembly, _ => GetLoadableTypes(assembly))`；异常时过滤 null 类型；文件过滤改用 `Path.GetExtension` + `StringComparison.OrdinalIgnoreCase`。

### 23. `Source/Euonia.Core/Reflection/EnumHelper.cs`
**具体问题**：`GetAttribute` 以 `DeclaredMembers.Length == 1` 作为取特性的前提 —— 枚举有多个成员时直接返回 default，即使特性真实存在；残留空 `{ }` 代码块。

**修改依据**：取特性只应关注「当前枚举值对应的字段」，与成员数量无关。改为 `Enum.GetName` → `GetRuntimeField` → `GetCustomAttribute<T>()`，与同文件 `GetCustomerAttribute` 的既有路径一致。

### 24. `Source/Euonia.Core/Reflection/Reflect.cs`
**具体问题**：
- `SetValue(obj, type, path, value)` 中 `absolutePropertyPath.StartsWith(objectPath!)` —— `Type.FullName` 为 null（泛型 / 动态类型）时抛 NRE；
- 前缀剥离用 `Replace(objectPath + ".", "")` 会**全局替换**路径中出现的同名片段，可能误删；
- `GetProperty<T, TResult>` 对 `UnaryExpression` / 非 `MemberExpression` 硬强转，抛 `InvalidCastException` 而非明确错误。

**修改依据**：反射工具应容错并给出明确异常。`objectPath != null` 守卫 + `Substring` 精确剥离前缀（`GetValue`/`SetValue` 两处同步修正）；`GetProperty` 改为模式匹配安全解构（MemberAccess / Convert / 其他），失败抛 `ArgumentException`。

---

## 九、回归测试

在 `Tests/Euonia.Core.Tests` 下新增 8 个测试文件，逐类覆盖上述修复点：

| 文件 | 覆盖问题 | 测试数 |
| --- | --- | --- |
| `Collections/PageableCollectionTest.cs` | 分页数学与参数守卫 | 8 |
| `Collections/EquatableReadOnlyListTest.cs` | 默认实例、相等与哈希 | 6 |
| `Threading/DeferralManagerTest.cs` | 延迟计数与等待 | 4 |
| `ObjectIdTest.cs` | 跨数值相等、转换、哈希 | 6 |
| `ValidationAttributesTest.cs` | 集合计数 / GUID 校验 | 14 |
| `LikeOperatorTest.cs` | 通配符匹配 | 8 |
| `BusinessExceptionTest.cs` | code → Message | 2 |
| `Extensions/ExtensionsCompareTest.cs` | 范围判断 | 3 |

**验证结果**：`Euonia.slnx` 构建 **0 警告 0 错误**；`Euonia.Test.slnx` 全部 10 个测试项目共 **285 个测试全部通过**（Euonia.Core.Tests 25 → 78）。