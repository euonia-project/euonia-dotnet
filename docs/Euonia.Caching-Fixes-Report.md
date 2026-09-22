# Euonia.Caching 修复报告

## 概述
本次针对 `euonia-net` 仓库中 Euonia.Caching 系列项目与测试展开分析（延续 Euonia.Bus → Euonia.Pipeline 修复轮次），核心问题是**缓存句柄释放不完整导致的定时器/底层缓存资源泄漏**，散布于三种句柄实现，并补了一处回归测试来直接暴露该缺陷。

- **`DictionaryCacheHandle` 定时器泄漏**（1 处，核心）：显式 `Dispose()` 后过期扫描 `Timer`（5000ms 周期）永不停止，继续保持整个句柄/管理器对象图无法被 GC，且继续向已释放的管理器触发逐出事件；
- **同类加固**（2 处）：`MemoryCacheHandle` 与 `RuntimeCacheHandle` 释放时补上对底层 `MemoryCache` 的显式释放（`RuntimeCacheHandle` 需避开全局共享的 `MemoryCache.Default`）；
- **测试警告清理**（2 处）：`DefaultCacheManagerTest` 中 `Task.Delay` 未传取消令牌（xUnit1051）；
- **回归测试补全**（1 个新测试）：验证“释放后不再触发过期扫描”。

验证结果：`Euonia.Build.slnx` **0 错误 0 警告**；`Euonia.Test.slnx` **0 错误**，仅剩 1 条与缓存无关的既有警告（Application.Tests）；Default.Tests **4/4**（原 3 + 新增 1）、Memory.Tests **9/9**、Runtime.Tests **9/9** 全部通过。

---

## 一、核心问题：`DictionaryCacheHandle` 的过期扫描定时器永不停止

涉及文件：`Source/Euonia.Caching/Internal/DictionaryCacheHandle.cs`（根因三处交互）

### 具体问题
`DictionaryCacheHandle<T>` 在构造函数里启动了一个周期扫描定时器，并附带一个**只能经由终结器触达**的释放路径：

```csharp
// 原构造函数（现 47~52 行）
_cache = new ConcurrentDictionary<string, CacheItem<TValue>>();
_timer = new Timer(TimerLoop, null, _random.Next(1000, SCAN_INTERVAL), SCAN_INTERVAL); // SCAN_INTERVAL = 5000ms

// 原文件末尾的终结器（已删除）
~DictionaryCacheHandle()
{
    _timer.Dispose();
}
```

而基类 `Internal/BaseCache.cs` 的标准 Dispose 模式是：

```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);   // ← 显式 Dispose 后终结器不再运行
}

protected virtual void Dispose(bool disposeManaged) { ... }   // 空实现
```

`BaseCacheHandle<T>.Dispose(bool)` 也只释放了 `Stats`，**没有任何一层在 `Dispose(bool)` 里停止 `_timer`**。于是：

1. 用户显式 `manager.Dispose()` → 各句柄 `Dispose(true)` → `GC.SuppressFinalize(this)` → 终结器调度被取消；
2. `_timer.Dispose()` 的唯一调用点（终结器）永远不再执行 → 定时器每 5 秒持续触发 `TimerLoop → ScanForExpiredItems`；
3. 定时器的回调持有 `this`（实例方法），而正在运行的 `System.Threading.Timer` 由运行时保持存活 → **整个句柄 + 管理器订阅链永久扎根，无法被垃圾回收**（真实泄漏）；
4. 扫描继续执行 `RemoveInternal` + `TriggerCacheSpecificRemove`，向**已释放**的管理器逐出过期项并触发 `OnRemoveByHandle`（`Reason = Expired`）——对已释放实例的“僵尸活动”。

### 红灯复现
新增回归测试 `DictionaryCacheHandleTest.TestExpirationScan_StopsAfterDispose`（构造 `CacheFactory.Build(...).WithDictionaryHandle()` 的管理器，写入 100ms 过期项后立即 `Dispose()`，监视 `OnRemoveByHandle` 是否再触发）。修复前运行：

```
Nerosoft.Euonia.Caching.Tests.DictionaryCacheHandleTest.TestExpirationScan_StopsAfterDispose [FAIL]
  Assert.Empty() Failure: Collection was not empty
  Collection: [CacheItemRemovedEventArgs :key - Expired 1]
```

即释放后约 1~5 秒内，定时器扫描仍把过期项逐出并抛给已释放的管理器——缺陷精确复现。

### 修改依据
按标准 Dispose 模式为句柄补上 `Dispose(bool)` 的重写，在释放托管资源时停止定时器，并删除已无意义的终结器（`BaseCache.Dispose()` 本身会抑制终结器，且定时器使对象根本到不了终结器阶段）：

```csharp
protected override void Dispose(bool disposeManaged)
{
    if (disposeManaged)
    {
        _timer.Dispose();
    }

    base.Dispose(disposeManaged);
}
```

对象随后可被正常回收，释放后再无扫描与逐出事件。

---

## 二、同类加固：两种 `MemoryCache` 句柄释放底层缓存

### `MemoryCacheHandle`（`Source/Euonia.Caching.Memory/MemoryCacheHandle.cs`）
底层是 `Microsoft.Extensions.Caching.Memory.MemoryCache`（含内部计时器与 `RegisterPostEvictionCallback` 订阅）。此前 `Dispose()` 链路完全不触碰 `_cache`；代码在 `Clear()` 里已经体现了“换新实例必须 Dispose 旧实例”的意图（`// 释放旧实例，避免其内部定时器与订阅在 GC 前持续存活造成资源泄漏`），但句柄整体释放时却遗漏了同样的处理。本次补上：

```csharp
protected override void Dispose(bool disposeManaged)
{
    if (disposeManaged)
    {
        _cache.Dispose();
    }

    base.Dispose(disposeManaged);
}
```

### `RuntimeCacheHandle`（`Source/Euonia.Caching.Runtime/RuntimeCacheHandle.cs`）
底层是 `System.Runtime.Caching.MemoryCache`，同样未随句柄释放。特殊之处：名为 `"default"` 时复用**进程级共享单例** `MemoryCache.Default`，绝不能对其 `Dispose`。因此新增 `_isDefaultCache` 标记并在释放时跳过：

```csharp
// 构造函数：默认实例分支
_isDefaultCache = true;
_cache = MemoryCache.Default;
// 其他分支保持 _isDefaultCache = false，走自建实例

protected override void Dispose(bool disposeManaged)
{
    if (disposeManaged && !_isDefaultCache)
    {
        _cache.Dispose();
    }

    base.Dispose(disposeManaged);
}
```

> 说明：这两处优先级低于第一项——`MemoryCache`/`System.Runtime.Caching.MemoryCache` 可被 GC 回收，无“不可回收”的确定性泄漏；这里的价值是**及时释放内部定时器与订阅**，语义与手柄整体生命周期一致，且与 `MemoryCacheHandle.Clear()` 已有的显式释放意图对齐。

---

## 三、测试警告清理
涉及文件：`Tests/Euonia.Caching.Default.Tests/DefaultCacheManagerTest.cs`
- 第 42、61 行 `await Task.Delay(3000/8000);` → 传入 `TestContext.Current.CancellationToken`，消除 2 条 xUnit1051 警告。

---

## 四、回归测试补全
新增 `Tests/Euonia.Caching.Default.Tests/DictionaryCacheHandleTest.cs`：
- `TestExpirationScan_StopsAfterDispose`——`CacheFactory.Build` + `WithDictionaryHandle` 构建管理器，写入 100ms 绝对过期项后立即 `Dispose()`，等待 7 秒（> 扫描周期 5s + 初始随机 1~5s 延迟）观察 `OnRemoveByHandle`。修复前必红（见第一节复现），修复后常绿，证明 `Dispose()` 真正停止了定时器。

原有 Default（3）/ Memory（9）/ Runtime（9）测试保持不动、全绿。

---

## 五、验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx`（`--no-incremental`） | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx`（`--no-incremental`） | 0 错误，1 条既有警告（Application.Tests `UnitOfWorkInterceptorTests.cs:138` xUnit1031，非本次引入） |
| `Euonia.Caching.Default.Tests`（`dotnet exec`） | 4/4（原 3 + 新增 1） |
| `Euonia.Caching.Memory.Tests` | 9/9 |
| `Euonia.Caching.Runtime.Tests` | 9/9 |

> 注：本机 `dotnet test`（xUnit.v3 MTP 适配器）会报 “Zero tests ran”/exit 5 的误报，故统一用 `dotnet exec <TestDll>` 跑真实测试，与 Bus / Pipeline 轮次一致。

## 六、遗留观察（未改动，供后续决策）
- **`MemoryCacheService` / `RuntimeCacheService` 无显式释放出口**：两者按单例持有 `MemoryCacheManager` / `RuntimeCacheManager`（每个值类型一个 `ICacheManager<T>`），服务关闭时这些管理器并不会被 `Dispose()`。本次句柄级修复已保证“何时释放、何时停止后台资源”的正确性，但服务层若要 `Dispose`，需先让这些组件走 DI 生命周期管理（如夺回 `IDisposable` 并在容器释放时统一释放），留待后续。
- **`ConfigurationBuilder.GetTimeSpan` 的正则**：`@"\b[0-9]+[S|H|M]\b"` 中字符类 `[S|H|M]` 把 `|` 也当作可匹配字符（应为 `[SHM]`），属偏执刻板但非功能缺陷（极少输入会命中 `|`），未改动。
- **`CacheFactory.Build` 已读的配置启用了该句柄的过期扫描**（本仓库默认无过期项时几乎不产生实际逐出，但定时器仍每 5 秒触发一次空扫描）：修复后只要句柄被释放，扫描即停；未释放的长期运行管理器保持现有扫描语义不变。