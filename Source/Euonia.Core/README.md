# 概述

Euonia.Core 是 Euonia 框架的基础库，提供一组必备的、与具体框架解耦的通用构建模块——集合、反射辅助、扩展方法、异步线程原语、释放工具、异常类型与常用工具类——这些模块被 Euonia 下其他所有包复用。

- **目标框架**：`netstandard2.1`、`net7.0`、`net8.0`、`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.*`（部分工具按惯例直接放入 `System` / 全局命名空间）
- **开源**：依据仓库内 `LICENSE` 许可发布

# 快速开始

通过 NuGet 安装：

```bash
dotnet add package Euonia.Core
```

直接使用库中的类型，无需额外配置：

```csharp
using Nerosoft.Euonia;

var snowflakeId = ObjectId.Snowflake();   // 时间有序的唯一 ID
```

# 命名空间导航

| 命名空间 | 内容 |
| --- | --- |
| （全局命名空间 + `System`） | 断言、`Optional<T>`、ID 生成、单例/对象池、弱引用、异常类型、扩展方法等 |
| `Nerosoft.Euonia.Collections` | 双端队列、类型列表、可观测分组、分页集合、树等 |
| `Nerosoft.Euonia.Reflection` | 类型内省、反射读写、枚举辅助、程序集加载 |
| `Nerosoft.Euonia.Threading` | `AsyncLock`、信号量、事件、监视器、`AsyncContext` 等异步原语 |
| `Nerosoft.Euonia.Threading.Interop` | 将 `WaitHandle` / .NET 事件转为 `Task` |
| `Nerosoft.Euonia.Disposing` | 同步/异步幂等释放原语 |
| `Nerosoft.Euonia.Security` | 用户主体、声明类型、账号/凭据异常 |
| `Nerosoft.Euonia.Windows` | `ICommand` 实现（`DelegateCommand`、`GenericCommand`） |
| `System.ComponentModel.DataAnnotations` | Guid、集合元素数量校验特性 |

# 全局命名空间与 `System`

以下工具类按惯例直接放到全局命名空间或 `System` 命名空间（`using System;` 即可用，无需额外引入）。

## 断言与校验

### `Check`

断言工具类，条件不满足时抛出异常：

```csharp
Check.Ensure(list != null && list.Count > 0, "list 不能为空");
var item = Check.EnsureNotNull(value, nameof(value));
var code = Check.EnsureNotNullOrEmpty(code, nameof(code), maxLength: 16, minLength: 2);
Check.EnsureIsMatch(email, nameof(email), @"^[^@\s]+@[^@\s]+$");
Check.EnsureAssignableTo<Animal>(typeof(Dog), nameof(type));
Check.EnsureLengthInRange(name, "name", maxLength: 50);
```

### `CheckResult<TValue>`

携带值与校验状态的结果对象，可链式处理：

```csharp
var result = Check.Ensure(42, v => v > 0);          // IsValid == true
result.Success(v => Console.WriteLine($"ok: {v}"))
      .Failure(v => Console.WriteLine($"bad: {v}"));

if (result.IsValid)
{
    int value = result;   // 隐式转换为 TValue
}
```

### `ArgumentAssert`

参数空引用断言：

```csharp
ArgumentAssert.ThrowIfNull(configuration, nameof(configuration));
ArgumentAssert.For<string>.ThrowIfNull(name);   // 泛型特化版本
```

### `ExceptionAssert`

按条件抛出指定类型异常：

```csharp
ExceptionAssert.ThrowIf<ArgumentOutOfRangeException>(count < 0, "count 必须大于等于 0");
ExceptionAssert.ThrowIf<InvalidOperationException>(notReady,
    () => new InvalidOperationException("未就绪"));
ExceptionAssert.ThrowIf<NotSupportedException>(unsupported);
```

### `Invariant`

仅在 DEBUG 构建下生效的不变量断言（Release 中被编译移除）：

```csharp
Invariant.Require(_state == State.Ready, "状态必须为 Ready");
```

## `Optional<T>`

可空值的容器（有值时保证非空）：

```csharp
Optional<string> maybe = Optional<string>.OfNullable(GetNameOrNull());

maybe.IfPresent(n => Console.WriteLine(n));            // 有值才执行
string name = maybe.Or("guest");                        // 默认值
int length = maybe.Select(s => s.Length).Or(-1);        // 链式转换
string value = maybe.GetOrThrow<InvalidOperationException>(
    () => new InvalidOperationException("缺失"));
```

## `Check`（全局、非泛型断言之补充）

见上文 `Check` 一节。另外还有：

### `CheckResult<TValue>`、`ArgumentAssert`、`ExceptionAssert`、`Invariant`

均已包含在使用示例中，此处从略。

## `Clock`

Unix 时间戳辅助：

```csharp
long ms = Clock.GetUnixTimestampMillis();
long ticks = Clock.GetUnixTimestampTicks();
long then = Clock.ToUnixTimestampMillis(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
```

## ID 生成

### `ObjectId` 与 `ObjectId<T>`

可包装 `int` / `long` / `Guid` / `string` 的值类型 ID，支持双向隐式转换，并提供多种 ID 生成器入口：

```csharp
ObjectId snowflake = ObjectId.Snowflake();               // long 雪花 ID
ObjectId ulid = ObjectId.Ulid();                         // 26 位 ULID 字符串
ObjectId guid = ObjectId.NewGuid(GuidType.SequentialAtEnd); // SQL Server 风格顺序 Guid
ObjectId random = ObjectId.Random();                     // 随机字符串 ID

ObjectId id = 42;                                        // long -> ObjectId
long back = id;                                          // ObjectId -> long

ObjectId<Guid> gid = Guid.NewGuid();                     // 泛型变体
Guid g = gid;
```

### `GuidType`

Guid 生成策略枚举：`Empty`、`Simple`（随机 v4）、`SequentialAsString`（MySQL/PostgreSQL 风格）、`SequentialAsBinary`（Oracle 风格）、`SequentialAtEnd`（SQL Server 风格）。

## 单例与对象池

### `Singleton<T>`

```csharp
var svc = Singleton<MyService>.Get(() => new MyService());
MyService same = Singleton<MyService>.InstanceDOIs;
```

### `ObjectPool<T>` 与 `IObjectPoolPolicy<T>`

```csharp
sealed class ListPolicy : IObjectPoolPolicy<List<int>>
{
    public List<int> CreateNew() => new();
    public bool Return(List<int> value) { value.Clear(); return true; }
}

var pool = new ObjectPool<List<int>>(new ListPolicy());
var list = pool.Lease();
try { list.Add(1); }
finally { pool.Return(list); }
```

## 生命周期、弱引用与事件

### `DisposableObject` / `DisposedEventArgs`

提供 `Disposed` 事件与终结器兜底的抽象基类：

```csharp
sealed class Resource : DisposableObject
{
    private Stream? _stream;
    protected override void Dispose(bool disposing)
    {
        if (disposing) _stream?.Dispose();
    }
}

using var r = new Resource();
r.Disposed += (s, e) => Console.WriteLine($"Disposed, hash={e.HashCode}");
```

### `Weak<T>` 与 `WeakEventManager`

```csharp
var weak = new Weak<MyClass>(instance);
if (weak.Target is { } alive)
{
    alive.DoWork();
}
```

### `AsyncEventHandler<TEventArgs>` 与 `Empty`、`Unit`

```csharp
bool isEmpty = Empty.Value.ToString() == "";   // true
Task unitTask = Unit.Task;                    // 已完成的任务
```

### `LikeOperator` 与 `TextCaseType` / `TextTrimType`

```csharp
bool a = LikeOperator.LikeString("Hello, World!", "hello*"); // true
bool b = LikeOperator.LikeString("abc", "a?c");              // true

string trimmed = "  text  ".Trim(TextTrimType.Both);
string camel = "hello_world".ToCamelCase();                 // "helloWorld"
```

### `EnumHelper` 与 `EnumParser<T>`、`Reflect`（见下方 Reflection 章节）

## `RequestContext`

HTTP 请求上下文载体：

```csharp
var ctx = new RequestContext
{
    Method = "GET",
    Path = "/api/orders",
    Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer abc" }
};
string auth = ctx.Authorization;   // "Bearer abc"
```

## 其它

### `ShortUniqueId`

Hashids 风格的短 ID 编码器：

```csharp
var codec = new ShortUniqueId(salt: "my-key", minHashLength: 8);
string hash = codec.Encode(12345);                 // 例如 "Mz4ZQJkX"
long decoded = codec.DecodeSingleInt64(hash);      // 12345
```

### `ObjectId` / `ObjectId<T>`、`UlidGenerator`、`GuidGenerator`、`RandomId`

见上方「ID 生成」一节。

---

# `Nerosoft.Euonia.Collections`

```csharp
using Nerosoft.Euonia.Collections;
```

## `DequeCollection<T>`

环形缓冲实现的双端队列，两端 O(1) 进出队，同时实现 `IList<T>`：

```csharp
var deque = new DequeCollection<string>();
deque.AddToBack("b");
deque.AddToFront("a");                    // [a, b]
deque.InsertRange(2, new[] { "c", "d" }); // [a, b, c, d]

string first = deque.RemoveFromFront();   // "a"
string last = deque.RemoveFromBack();     // "d"
deque[0] = "B";
string[] snapshot = deque.ToArray();      // ["B", "c"]
```

## `TypeList` / `TypeList<T>`

仅允许"可赋值给基类型"的类型集合：

```csharp
class Animal { }
class Dog : Animal { }
class Cat : Animal { }

var types = new TypeList<Animal>();
types.AddType<Dog>();
types.TryAddType<Dog>();                  // false，已存在
bool hasDog = types.ContainsType<Dog>(); // true
types.RemoveType<Cat>();
```

## `ObservableGroup` / `ReadOnlyObservableGroup` / `ObservableGroupedCollection`

可观测分组集合（每个组本身是 `ObservableCollection` + 不可变 `Key`）：

```csharp
var group = new ObservableGroup<string, int>("A", new[] { 1, 2, 3 });
group.Add(4);
string key = group.Key;   // "A"

var grouped = new ObservableGroupedCollection<char, string>(
    words.GroupBy(w => w[0]));
grouped.Add(new ObservableGroup<char, string>('x', new[] { "xray" }));
```

## `PageableCollection` / `ViewCollection`

分页 / 视图集合：

```csharp
var page = new PageableCollection<int>(new[] { 1, 2, 3, 4 })
{
    PageNumber = 2,
    PageSize = 2,
    TotalCount = 4
};
page.StartPosition;   // 3
page.EndPosition;     // 4
page.PageCount;       // 2

var view = new ViewCollection<string>(items, totalCount: 120);
view.TotalCount;      // 120
```

## 其它

`TreeView<TEntity>`（树节点）、`EquatableReadOnlyList<T>`（按元素比较相等性）、`ReadOnlyObservableGroup`、`ObservableGroupedCollection` 等，API 详见源码与下文"反射/工具"章节示例。

---

# `Nerosoft.Euonia.Reflection`

```csharp
using Nerosoft.Euonia.Reflection;
```

## 类型内省：

```csharp
var isDerived = typeof(Dog).IsSubclassOf(typeof(Animal));
var isGeneric = typeof(List<int>).IsAssignableToGenericType(typeof(IList<>));  // true
```

## `Reflect` / `Reflect<TTarget>`

从 Lambda 表达式提取 `PropertyInfo` / `MethodInfo`，并支持读写属性值（含点分隔路径）：

```csharp
class Address { public string City { get; set; } }
class Person
{
    public string Name { get; set; }
    public Address Address { get; set; }
    public void Work() { }
}

var person = new Person { Name = "Alice", Address = new Address { City = "Rome" } };

Reflect.SetValue(person, "Bob", p => p.Name);
object name = Reflect.GetValue(person, p => p.Name);          // "Bob"

object city = Reflect.GetValue(person, typeof(Person), "Address.City"); // "Rome"
Reflect.SetValue(person, typeof(Person), "Address.City", "Paris");
```

## `TypeHelper` / `EnumHelper` / `EnumParser<T>`

```csharp
int n = TypeHelper.CoerceValue<int, string>("42");            // 42
var color = TypeHelper.CoerceValue<ConsoleColor>(typeof(string), "Red");

var desc = EnumHelper.GetCustomerAttribute<DescriptionAttribute>(Status.Active);
if (EnumParser<DayOfWeek>.TryParse("Friday", out var day)) { }
```

## `AssemblyHelper` / `AssemblyLoadContextManager`

```csharp
var types = AssemblyHelper.GetAllTypes(typeof(string).Assembly);
var assemblies = AssemblyHelper.LoadAssemblies(AppContext.BaseDirectory,
    SearchOption.TopDirectoryOnly)-var
```

## `PropertyAccessorCache<T>`

缓存属性的 Lambda 访问器表达式：

```csharp
LambdaExpression expr = PropertyAccessorCache<Person>.Get(nameof(Person.Name));
// expr 形如 p => p.Name
```

---

# `Nerosoft.Euonia.Threading`

所有协调原语均返回 `AwaitableDisposable<T>`，请始终使用 `using (await x.LockAsync())` 形式。

## `AsyncLock`

不可重入的异步互斥锁：

```csharp
private readonly AsyncLock _mutex = new();

public async Task DoStuffAsync()
{
    using (await _mutex.LockAsync())
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

## `AsyncSemaphore`

异步计数信号量：

```csharp
var semaphore = new AsyncSemaphore(2apsed);

using (await semaphore.LockAsync())   // 占一个槽位
{
    // 最多 2 个并发调用者
}

await semaphore.WaitAsync();
try { /* ... */ }
finally { semaphore.Release(); }
```

## `AsyncAutoResetEvent` / `AsyncManualResetEvent`

```csharp
var are = new AsyncAutoResetEvent();
are.Set();
await are.WaitAsync();   // 已置位则立即返回并自动复位

var mre = new AsyncManualResetEvent();
Task waiter = mre.WaitAsync();
mre.Set();               // 所有等待者完成
await waiter;
mre.Reset();             // 复位后新等待者再次阻塞
```

## `AsyncLazy<T>`

线程安全、可直接 await 的异步惰性初始化：

```csharp
var lazy = new AsyncLazy<int>(async () =>
{
    await Task.Delay(100);
    return 42;
}, AsyncLazyFlags.RetryOnFailure);

int answer = await lazy;
```

## `AsyncMonitor` 与 `AsyncConditionVariable`

```csharp
var monitor = new AsyncMonitor();

using (await monitor.EnterAsync())
{
    while (!IsReady)
        await monitor.WaitAsync();   // 等待期间释放监视器
    monitor.Pulse();                 // 唤醒一个等待者
}
```

## `AsyncCountdownEvent`

```csharp
var cde = new AsyncCountdownEvent(2);
Task allDone = cde.WaitAsync();
cde.Signal();
cde.Signal();          // 归零 -> allDone 完成
await allDone;
```

## `AsyncProducerConsumerQueue` / `AsyncCollection`

```csharp
var queue = new AsyncProducerConsumerQueue<int>(maxCount: 5);
await queue.EnqueueAsync(10);
int value = await queue.DequeueAsync();

queue.CompleteAdding();
foreach (var item in queue.GetConsumingEnumerable()) { }
```

## `PauseToken` / `PauseTokenSource`

协作式暂停：

```csharp
var source = new PauseTokenSource();
var pauseToken = source.Token;

async Task WorkerAsync()
{
    while (true)
    {
        await pauseToken.WaitWhilePausedAsync();  // 未暂停时立即返回
        await DoWorkAsync();
    }
}

source.IsPaused = true;    // 工作循环在此阻塞
source.IsPaused = false;   // 恢复
```

## `DeferralManager` / `IDeferralSource`

让事件参数声明"异步处理器仍在运行"：

```csharp
var manager = new DeferralManager();

using (manager.DeferralSource.GetDeferral())
{
    await ProcessSlowlyAsync();   // 离开作用域才算完成
}

await manager.WaitForDeferralsAsync();
```

## `State` 与 `StatefulMutex`

绑定状态令牌的跨任务互斥锁，状态失效则获取失败：

```csharp
var mutex = new StatefulMutex();
var state = mutex.State;

using (await mutex.AcquireAsync(state)) { }

mutex.InvalidateState();
using (await mutex.AcquireAsync(state)) { }   // 抛出 InvalidOperationException
```

## `NotifyTaskCompletion<T>`

任务包装器，完成时触发 `PropertyChanged`：

```csharp
var watcher = new NotifyTaskCompletion<int>(LoadAsync());
watcher.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(watcher.IsFaulted) && watcher.IsFaulted)
        Console.WriteLine(watcher.ErrorMessage);
};
```

## `AsyncContext` 与 `AsyncContextThread`

```csharp
AsyncContext.Run(async () =>
{
    await Task.Delay(100);
    // 此处 AsyncContext.Current != null
});

int result = AsyncContext.Run(async () =>
{
    await Task.Delay(100);
    return 42;
});

using (var thread = new AsyncContextThread())
{
    await thread.Factory.Run(async () => await DoWorkAsync());
    await thread.JoinAsync();
}
```

## `TimeoutValue` / `TimeoutTask`

```csharp
TimeoutValue timeout = TimeSpan.FromSeconds(5);   // TimeSpan 隐式转换
timeout.InMilliseconds;                           // 5000
timeout.IsInfinite;

using (var t = new TimeoutTask(TimeSpan.FromSeconds(5), CancellationToken.None))
{
    await t.Task; // 5 秒后完成
}
```

## `TaskHelper` / `TaskConstants` / `CancellationTokenTaskSource`

```csharp
Task done = TaskConstants.Completed;               // 复用已完成实例
Task<int> zero = TaskConstants<int>.Default;

int result = TaskHelper.Run(async (int x) => new ValueTask<int>(x * 2), 21);

using (var source = new CancellationTokenTaskSource<int>(cts.Token))
{
    Task<int> first = await Task.WhenAny(source.Task, DoWorkAsync());
    if (first == source.Task) { /* 已取消 */ }
}
```

---

# `Nerosoft.Euonia.Threading.Interop`

```csharp
using Nerosoft.Euonia.Threading.Interop;
```

将回调 / 句柄等待转换为 `Task`：

```csharp
public event EventHandler? MyEvent;

Task<EventArguments<object, EventArgs>> next =
    EventAsyncFactory.FromEvent(h => MyEvent += h, h => MyEvent -= h);

MyEvent?.Invoke(this, EventArgs.Empty);
var args = await next;
```

```csharp
var wh = new ManualResetEvent(false);
Task signalled = WaitHandleAsyncFactory.FromWaitHandle(wh);
wh.Set();
await signalledEDER;
```

---

# 释放（`Nerosoft.Euonia.Disposing`）

幂等的同步 / 异步释放原语。

## 同步

```csharp
public sealed class Resource : SingleDisposable<FileStream>
{
    public Resource(FileStream stream) : base(stream) { }
    protected override void Dispose(FileStream context) => context.Dispose();
}

using (var r = new Resource(stream)) { }

// 委托释放
using (AnonymousDisposable.Create(() => Console.WriteLine("cleanup"))) { }

// 组合释放
using (var group = new CollectionDisposable(stream1, stream2))
{
    group.Add(someExtraDisposable);
}
```

## 异步

```csharp
public sealed class Resource : AsyncSingleDisposable<Stream>
{
    public Resource(Stream stream) : base(stream) { }
    protected override ValueTask DisposeAsync(Stream context) => context.DisposeAsync();
}

await using (var r = new Resource(stream)) { }
```

---

# 异常类型（`System` 命名空间）

所有异常均声明在 `System` 命名空间，抛出 / 捕获无需额外 `using`。

## HTTP 状态异常

| 异常 | HTTP 状态码 |
| --- | --- |
| `BadRequestException` | 400 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `MethodNotAllowedException` | 405 |
| `RequestTimeoutException` | 408 |
| `ConflictException` | 409 |
| `TooManyRequestsException` | 429 |
| `UpgradeRequiredException` | 426 |
| `InternalServerErrorException` | 500 |
| `BadGatewayException` | 502 |
| `ServiceUnavailableException` | 503 |
| `GatewayTimeoutException` | 504 |

```csharp
throw new NotFoundException("订单 42 不存在。");
```

## `HttpStatusException`

携带任意状态码的基异常：

```csharp
throw new HttpStatusException(HttpStatusCode.Forbidden, "不能执行此操作");
```

## `HttpStatusCodeAttribute` 与 `ExceptionPrompt`

```csharp
var status = ex switch
{
    HttpStatusException http => http.StatusCode,
    _ => ex.GetType().GetCustomAttribute<HttpStatusCodeAttribute>()?.StatusCode
         ?? HttpStatusCode.InternalServerError
};
```

## 业务与通用异常

```csharp
throw new NotFoundException("订单 42 不存在。");     // 404
throw new BusinessException("ORDER_OUT_OF_STOCK", "库存不足");
throw new ConfigurationException("缺失必要配置。");
throw new InvalidValueException("值无效。");
```

---

# `Nerosoft.Euonia.Security`

```csharp
using Nerosoft.Euonia.Security;
```

## `UserPrincipal`

基于 `ClaimsPrincipal` 的只读类型化访问器：

```csharp
var principal = new UserPrincipal(httpContext.User);

if (principal.IsAuthenticated && principal.IsInRole("Admin"))
{
    string id = principal.UserId;
    string name = principal.Username;
    string tenant = principal.Tenant;
    IEnumerable<string> roles = principal.Roles;
}

bool any = principal.IsInRoles("Admin,User", separator: ",");
var claim = principal.FindClaim("email");
```

## `UserClaimTypes`

OIDC/JWT 声明名常量（`Subject = "sub"`、`Name = "name"`、`Role = "role"`、`Tenant = "tenant"` 等）。

## 账号与凭据异常

```csharp
throw new AccountNotFoundException("user@example.com");    // 账号不存在
throw new AccountLockedException("user@example.com");     // 账号已锁定
throw new CredentialIncorrectException(userName);         // 凭据不正确

catch (AccountException ex)
{
    var identity = ex.Identity;
    var details = ex.Details;
}
```

---

# `Nerosoft.Euonia.Windows`

```csharp
using Nerosoft.Euonia.Windows;
```

`ICommand` 实现，适用于 WPF / WinUI 等 MVVM 场景。

## `DelegateCommand` / `DelegateCommand<T>`

```csharp
var cmd = new DelegateCommand<string>(
    s => Save(s),
    s => !string.IsNullOrEmpty(s));

if (cmd.CanExecute(text)) { cmd.Execute(text); }
cmd.RaiseCanExecuteChanged();
```

## `GenericCommand` / `GenericCommand<T>`

```csharp
var cmd = new GenericCommand
{
    CanExecuteCallback = p => p is int n && n > 0,
    ExecuteCallback = p => Process((int)p!)
};

cmd.Execute(42);
```

---

# `System.ComponentModel.DataAnnotations` 验证特性

## `GuidAttribute`

校验字符串是否为合法 GUID：

```csharp
public class OrderModel
{
    [Required, Guid]
    public string OrderId { get; set; }
}
```

## `CollectionCountAttribute`

校验集合元素数量范围：

```csharp
public class PagedModel
{
    [CollectionCount(1, 100)]   // 至少 1 个，至多 100 个
    public ICollection<string> Tags { get; set; }
}
```

---

# 扩展方法（全局 `Extensions`）

库在全局命名空间提供了 `Extensions` 静态扩展类，按领域分文件组织：

```csharp
// 字符串
string kebab = "HelloWorld".ToKebabCase();     // "hello-world"
string snake = "HelloWorld".ToSnakeCase();     // "hello_world"
string camel = "hello_world".ToCamelCase();    // "helloWorld"
bool isMail = "a@b.com".IsEmail();             // true
var trimmed = "  text  ".Trim(TextTrimType.Both refinery);

// 集合
items.ForEach(x => Console.WriteLine(x));
var filtered = people.WhereIf(onlyAdults, p => p.Age >= 18);
var v = dict.GetValueOrDefault("k");
dict.GetOrAdd("k", () => new Value());
var sorted = deps.SortByDependencies();        // 拓扑排序

// 对象 / 反射
var session = obj.As<ISession>();
var to = obj.To<double>();
object city = Reflect.GetValue(person, typeof(Person), "Address.City");

// 异常 / 事件
string full = ex.GetFullMessage();
handler.CheckAndInvoke(this, e);

// 枚举
bool valid = someEnum.IsValid();
string desc = DayOfWeek.Monday.GetDescription();

// 线程 / Task
await someTask.WaitAsync(ct);
Task ignored = someTask.Ignore();
SynchronizationContextSwitcher.NoContext(() => { });
```

# 文档

更多详细信息与高级用法，参见 [Euonia GitHub 仓库](https://github.com/NerooftDev/Euonia) 上的官方文档。