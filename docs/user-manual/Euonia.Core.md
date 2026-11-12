# Euonia.Core 用户手册

Euonia 的基础库：断言、ID 生成、异步协调原语、对象池、释放模式、反射/集合工具与常用扩展方法。本页是端到端教程与 API 速查；安装、完整类型清单与注意事项见 [项目 README](../../Source/Euonia.Core/README.md)。

- **NuGet 包**：`Euonia.Core`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.*`；`Check`、`ObjectId`、`Extensions` 在**全局命名空间**，`Optional<T>`、`ObjectPool<T>`、`Singleton<T>` 在 `System`
- **第三方依赖**：无（仅 BCL）
- **完整参考**：[Source/Euonia.Core/README.md](../../Source/Euonia.Core/README.md)

## 教程

### 场景：库存预占工具（控制台，零依赖）

一个后台脚本要按 SKU 预占库存：校验入参、并发扣减、为每次预占生成有序订单号、打印明细。整个场景只用 `Euonia.Core`，不引入 DI、不接数据库。

### 前置条件

- .NET 10 SDK（`dotnet --version` ≥ 10）；任意控制台项目；无需外部服务。

### 1. 安装

```bash
dotnet new console -n StockTool
cd StockTool
dotnet add package Euonia.Core
```

### 2. 断言 + ID：入参与订单号

```csharp
using System.Collections.Concurrent;
using Nerosoft.Euonia.Threading;

var inventory = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
inventory["SKU-1001"] = 5;

var gate = new AsyncLock();

async Task<ObjectId> ReserveAsync(string sku, int quantity)
{
    // 参数校验：null/空 → ArgumentException（字符串版）
    Check.EnsureNotNullOrEmpty(sku, nameof(sku));
    Check.Ensure(sku.Length <= 32, "sku 长度不能超过 32");

    // 业务前置条件：失败抛 InvalidOperationException
    Check.Ensure(quantity > 0, "quantity 必须大于 0");

    using (await gate.LockAsync())
    {
        var available = inventory.GetValueOrDefault(sku);
        Check.Ensure(available >= quantity, $"库存不足：{sku} 剩余 {available}");

        inventory[sku] = available - quantity;

        // ULID：26 位、按时间有序，适合多实例部署
        return ObjectId.Ulid();
    }
}
```

要点：`AsyncLock` 不可重入，锁内只做内存操作；`ObjectId.Ulid()` 不依赖机器位，多实例也不会重复。

### 3. 对象池 + 扩展方法：组装明细

```csharp
sealed class DetailBufferPolicy : IObjectPoolPolicy<List<string>>
{
    public List<string> CreateNew() => new(capacity: 8);

    public bool Return(List<string> value)
    {
        value.Clear();   // 归还前重置，避免下一位租用者读到脏数据
        return true;
    }
}

var pool = new ObjectPool<List<string>>(new DetailBufferPolicy(), maxItems: 4);
var buffer = pool.Lease();

try
{
    var orderId = await ReserveAsync("SKU-1001", 2);

    buffer.Add($"order={orderId}");
    buffer.Add($"sku={"SKU-1001".ToKebabCase()}");   // "sku-1001"
    buffer.Add($"remaining={inventory.GetValueOrDefault("SKU-1001")}");

    Console.WriteLine(string.Join(", ", buffer));
}
finally
{
    pool.Return(buffer);
}
```

### 4. Optional：表达「可能没有」而不是 null

```csharp
// 命令行参数可能缺失：用 Optional 明确表达，不把 null 语义漏到下游
Optional<string> remark = Optional<string>.OfNullable(args.FirstOrDefault());

var label = remark
    .Select(value => value.Trim().ToUpperInvariant())
    .Or("(无备注)");

Console.WriteLine($"remark={label}");
Console.WriteLine($"stock={inventory.GetValueOrDefault("SKU-1001")}");
```

### 5. 运行与验证

```bash
dotnet run
# 期望输出（ULID 每次不同）：
# order=01J8Z...9K, sku=sku-1001, remaining=3
# remark=(无备注)
# stock=3

dotnet run -- 加急
# remark=加急
```

再跑一次不存在的 SKU，确认断言形态：

```bash
# 把 ReserveAsync 的 sku 改成 "SKU-9999" 后运行
# 未处理异常：System.InvalidOperationException: 库存不足：SKU-9999 剩余 0
```

### 常见失败点

- **多实例部署出现重复 ID**：`ObjectId.Snowflake()` 的 machineId/datacenterId 固定为 0，分布式场景请用 `ObjectId.Ulid()` 或 `ObjectId.Guid(GuidType)`。
- **异步锁卡死**：`AsyncLock` 不可重入，同一异步流里嵌套 `LockAsync()` 会等待自己；把加锁范围收敛到一个方法。
- **catch 不到预期的异常**：`Check.Ensure` 抛 `InvalidOperationException`；`EnsureNotNull<T>`（泛型）抛 `ArgumentNullException`，而字符串版 `EnsureNotNull` / `EnsureNotNullOrEmpty` 抛的是 `ArgumentException`。
- **对象池泄漏**：`Lease()` 后必须 `Return()`（用 `try/finally`），否则策略里的清理逻辑不会执行。
- **CS0104 二义性**：全局命名空间的 `Check` / `ObjectId` 与 `using` 引入的同名类型冲突，用 `global::Check` 限定。

## API 参考

### 断言与校验

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `Check.Ensure(bool, string)` | 状态断言，失败抛 `InvalidOperationException` | 第二参数为消息模板 |
| `Check.EnsureNotNull<T>(T, string)` | 泛型空值断言，失败抛 `ArgumentNullException` | 第二参数为参数名 |
| `Check.EnsureNotNull(string, string, int maxLength, int minLength)` | 字符串非 null + 长度范围，失败抛 `ArgumentException` | `maxLength = int.MaxValue`、`minLength = 0` |
| `Check.EnsureNotNullOrEmpty(string, string)` | 字符串非 null/空，失败抛 `ArgumentException` | 仅两个参数 |
| `Check.EnsureIsMatch(string, string, string, Action<string>, RegexOptions)` | 正则断言 | 第四参数是失败回调，第五参数默认 `RegexOptions.None` |
| `CheckResult<TValue>` | 携带值的校验结果，可链式 `Success` / `Failure` | `IsValid`、隐式转 `TValue` |
| `Invariant.Require(bool, string?)` | 不变式断言（`[Conditional("DEBUG")]`，仅 Debug 生效） | 失败抛 `InvalidOperationException` |

### ID 与时间

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `ObjectId.Snowflake()` | 雪花 ID（long） | 机器位固定 0，单进程用 |
| `ObjectId.Ulid()` | 26 位 ULID 字符串 | 按时间有序，推荐分布式使用 |
| `ObjectId.Guid(GuidType)` | 生成指定风格的 Guid 并包装为 `ObjectId` | `GuidType.SequentialAtEnd` 等 5 种 |
| `ObjectId.Random()` | 随机字符串 ID | 基于时间戳种子 |
| `ObjectId.NewGuid(GuidType)` | 只返回 `Guid` | 与 `Guid(GuidType)` 对应 |
| `ObjectId<T>` | 泛型变体（`Guid` / `long` 等） | 与基础类型双向隐式转换 |
| `Clock.GetUnixTimestampMillis()` | Unix 毫秒时间戳 | 另有 `GetUnixTimestampTicks`、`ToUnixTimestampMillis` |

### 异步与生命周期

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AsyncLock.LockAsync(CancellationToken)` | 异步互斥锁，**不可重入** | 返回 `Task<IDisposable>`，配合 `using` |
| `AsyncSemaphore.WaitAsync(CancellationToken)` | 异步信号量 | 构造时指定初始许可数 |
| `AsyncLazy<T>` | 异步惰性初始化 | 工厂只执行一次 |
| `ObjectPool<T>(IObjectPoolPolicy<T>, int? maxItems)` | 基于策略的对象池 | `maxItems` 默认不限 |
| `IObjectPoolPolicy<T>.CreateNew()` / `Return(T)` | 池化策略 | `Return` 返回 `false` 表示丢弃 |
| `DisposableObject` | 幂等释放基类，重写 `Dispose(bool)` | 提供 `Disposed` 事件 |
| `Singleton<T>.Get(Func<T>)` / `Instance` | 进程级单例容器 | 未初始化直接读 `Instance` 抛 `KeyNotFoundException` |

### 集合、反射与扩展

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `DequeCollection<T>` | 环形缓冲双端队列，两端 O(1) | `AddToFront` / `AddToBack` / `RemoveFromFront` |
| `TypeList<T>` | 类型列表（用于程序集/类型集合） | 实现 `ITypeList<T>` |
| `PageableCollection<T>` | 分页集合 | 页码从 1 开始 |
| `Reflect.GetValue(object, Type, string)` | 按路径反射读值 | 支持 `"Address.City"` |
| `PropertyAccessorCache<T>` | 属性访问器缓存 | 避免重复反射 |
| `"PayWeb".ToKebabCase()` | 命名风格转换 | 另有 `ToCamelCase` / `ToSnakeCase` |
| `dictionary.GetOrAdd(key, factory)` | 字典取值或创建 | 两个工厂重载 |
| `items.ForEach(action)` | 集合遍历 | 返回 `void` |

### 选项（代码配置）

`Euonia.Core` **没有配置节**，也不读取 `IConfiguration`：以上类型要么是静态工具，要么可直接 `new`，无需 `appsettings` 与环境变量。

## 相关文档

- [项目 README](../../Source/Euonia.Core/README.md) —— 完整类型清单、注意事项、版本与兼容
- [Euonia.Osba 用户手册](Euonia.Osba.md) —— 基于本包权限契约的业务对象运行时
- [Euonia.Security 用户手册](Euonia.Security.md) —— 使用本包 `UserPrincipal` / `IPermissionChecker`
- [Euonia.Mapping 用户手册](Euonia.Mapping.md) —— 对象映射抽象
- [仓库根 README](../../README.md)
