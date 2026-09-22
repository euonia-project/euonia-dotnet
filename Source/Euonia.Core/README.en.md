# Overview

Euonia.Core is the foundation library of the Euonia framework. It provides a set of essential, framework-agnostic building blocks—collections, reflection helpers, extension methods, threading abstractions, disposal primitives, exception types, and common utilities—that are reused across all other Euonia packages.

- **Target frameworks**: `netstandard2.1`, `net7.0`, `net8.0`, `net9.0`, `net10.0`
- **Root namespaces**: `Nerosoft.Euonia.*` (some utilities intentionally live in the global `System` namespace)
- **Open source**: licensed under the `LICENSE` in this repository

# Getting Started

Install the package via NuGet:

```bash
dotnet add package Euonia.Core
```

The library is designed to be used directly, for example:

```csharp
using Nerosoft.Euonia;
using Nerosoft.Euonia.Collections;

var snowflakeId = ObjectId.Snowflake();   // time-ordered long ID
var deque = new DequeCollection<int>();   // double-ended queue
```

# Namespace Map

| Namespace | Contents |
| --- | --- |
| (global namespace + `System`) | Assertions, `Optional<T>`, ID generators, singletons/object pools, weak references, exceptions, extension methods |
| `Nerosoft.Euonia.Collections` | Deque, type lists, observable groups, paged collections, trees |
| `Nerosoft.Euonia.Reflection` | Type introspection, reflected get/set, enum helpers, assembly loading |
| `Nerosoft.Euonia.Threading` | `AsyncLock`, semaphores, events, monitors, `AsyncContext` and more async primitives |
| `Nerosoft.Euonia.Threading.Interop` | `WaitHandle` / .NET events as `Task` |
| `Nerosoft.Euonia.Disposing` | Synchronous / asynchronous idempotent disposal primitives |
| `Nerosoft.Euonia.Security` | User principal, claim types, account/credential exceptions |
| `Nerosoft.Euonia.Windows` | `ICommand` implementations (`DelegateCommand`, `GenericCommand`) |
| `System.ComponentModel.DataAnnotations` | `Guid` and collection-count validation attributes |

---

# Global Namespace & `System`

The following utilities are intentionally placed in the global namespace or `System` namespace and are available without any `using`.

## Assertions

### `Check`

Static assertion helpers that throw when a condition is not met:

```csharp
Check.Ensure(list != null && list.Count > 0, "list must not be empty");
var item = Check.EnsureNotNull(value, nameof(value));
var code = Check.EnsureNotNullOrEmpty(code, nameof(code), maxLength: 16, minLength: 2);
Check.EnsureIsMatch(email, nameof(email), @"^[^@\s]+@[^@\s]+$");
Check.EnsureAssignableTo<Animal>(typeof(Dog), nameof(type));
Check.EnsureLengthInRange(name, "name", maxLength: 50);
```

### `CheckResult<TValue>`

The result of a condition check, holding the value and validation state, with a chainable API:

```csharp
var result = Check.Ensure(42, v => v > 0);          // IsValid == true
result.Success(v => Console.WriteLine($"ok: {v}"))
      .Failure(v => Console.WriteLine($"bad: {v}"));

if (result.IsValid)
{
    int value = result;                             // implicit conversion to TValue
}
```

### `ArgumentAssert`

Null-argument assertions that throw `ArgumentNullException`:

```csharp
ArgumentAssert.ThrowIfNull(configuration, nameof(configuration));
ArgumentAssert.For<string>.ThrowIfNull(name);       // generic specialization
```

### `ExceptionAssert`

Throw a specific exception type based on a condition:

```csharp
ExceptionAssert.ThrowIf<ArgumentOutOfRangeException>(count < 0, "count must be >= 0");
ExceptionAssert.ThrowIf<InvalidOperationException>(notReady,
    () => new InvalidOperationException("not ready"));
ExceptionAssert.ThrowIf<NotSupportedException>(unsupported);
```

### `Invariant`

Debug-only invariant check (compiled out in Release builds):

```csharp
Invariant.Require(_state == State.Ready, "state must be Ready");
```

## `Optional<T>`

A container for a nullable value (a present value is guaranteed non-null), similar to Java's Optional:

```csharp
Optional<string> maybe = Optional<string>.OfNullable(GetNameOrNull());

maybe.IfPresent(n => Console.WriteLine(n));            // runs only when present
string name = maybe.Or("guest");                        // fallback value
int length = maybe.Select(s => s.Length).Or(-1);        // chained transformation
string value = maybe.GetOrThrow<InvalidOperationException>(
    () => new InvalidOperationException("missing"));

var present = Optional<int>.Of(42);                     // Of throws on null
bool empty = present.IsEmpty;                           // false
Console.WriteLine(Optional<int>.Empty);                 // "Optional.empty"
```

## `Clock`

Unix-timestamp helpers:

```csharp
long ms = Clock.GetUnixTimestampMillis();
long ticks = Clock.GetUnixTimestampTicks();
long then = Clock.ToUnixTimestampMillis(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
```

## ID Generation

### `ObjectId` / `ObjectId<T>`

A value-type ID that wraps `int` / `long` / `Guid` / `string` with bidirectional implicit conversions, plus factory methods for various ID generators:

```csharp
ObjectId snowflake = ObjectId.Snowflake();               // long snowflake ID
ObjectId ulid = ObjectId.Ulid();                         // 26-char ULID string
ObjectId guid = ObjectId.NewGuid(GuidType.SequentialAtEnd); // SQL Server style sequential Guid
ObjectId random = ObjectId.Random();                     // random string ID

ObjectId id = 42;                                        // long -> ObjectId
long back = id;                                          // ObjectId -> long

ObjectId<Guid> gid = Guid.NewGuid();                     // generic variant
Guid g = gid;
```

### `ShortUniqueId`

A Hashids-style short-ID encoder/decoder:

```csharp
var codec = new ShortUniqueId(salt: "my-key", minHashLength: 8);
string hash = codec.Encode(12345);                       // e.g. "Mz4ZQJkX"
long decoded = codec.DecodeSingleInt64(hash);            // 12345
bool ok = codec.TryDecodeSingleInt64(hash, out long id); // non-throwing decode
```

### `GuidType`

Guid generation strategy enum: `Empty`, `Simple` (random v4), `SequentialAsString` (MySQL/PostgreSQL), `SequentialAsBinary` (Oracle), `SequentialAtEnd` (SQL Server).

## Singletons & Object Pools

### `Singleton<T>`

A `ConcurrentDictionary`-backed generic singleton container:

```csharp
var svc = Singleton<MyService>.Get(() => new MyService());
MyService same = Singleton<MyService>.Instance;
```

### `ObjectPool<T>` & `IObjectPoolPolicy<T>`

A policy-driven object pool:

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

## Lifecycle, Weak References & Events

### `DisposableObject` & `DisposedEventArgs`

An abstract base with a `Disposed` event plus finalizer fallback:

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

### `Weak<T>`

Typed weak reference:

```csharp
var weak = new Weak<MyClass>(instance);
if (weak.Target is { } alive)
{
    alive.DoWork();
}
```

### `WeakEventManager`

Weak event manager that prevents leaks caused by event sources holding targets:

```csharp
var mgr = new WeakEventManager();
mgr.AddEventHandler<DisposedEventArgs>(OnDisposed, "Disposed");
// ...
mgr.HandleEvent(this, new DisposedEventArgs(GetHashCode()), "Disposed");
mgr.RemoveEventHandler<DisposedEventArgs>(OnDisposed, "Disposed");
```

### `AsyncEventHandler<TEventArgs>`

Async event delegate:

```csharp
async Task OnSaved(object sender, SavedEventArgs args) { /* ... */ }
AsyncEventHandler<SavedEventArgs> handler = OnSaved;
```

### `Gen2GcCallback`

Invokes a callback on each Gen-2 GC while the target is alive; useful for cache cleanup:

```csharp
Gen2GcCallback.Register(state => CleanupCaches(state), cacheInstance);
```

### `ManagedFinalizerQueue`

Asynchronously finalizes resources when they are collected (weakly tracked):

```csharp
var registration = ManagedFinalizerQueue.Instance.Register(leakyHandle, asyncFinalizer);
registration.Dispose();   // cancel the pending finalization
```

### `Empty` & `Unit`

- `Empty`: a singleton empty object, `Empty.Value`, `ToString()` returns empty string.
- `Unit`: the functional `void` type, `Unit.Value` and `Unit.Task`:

```csharp
Unit u = Unit.Value;            // ToString() == "()"
await Unit.Task;                // an already-completed task
```

## Other Utilities

### `LikeOperator`

Wildcard string matching with `*` (zero-or-more) and `?` (one char):

```csharp
bool a = LikeOperator.LikeString("Hello, World!", "hello*"); // true
bool b = LikeOperator.LikeString("abc", "a?c");              // true
```

### `MethodInvokerBuilder`

A compiled-expression, high-performance method invoker:

```csharp
var method = typeof(Calculator).GetMethod(nameof(Calculator.Add));
var invoker = MethodInvokerBuilder.Build(method);
object? sum = await invoker(calculator, new object?[] { 2, 3 });  // boxed result
```

### `EnumObject<TValue>`

A simple "name + value" enum object:

```csharp
var opt = new EnumObject<int> { Name = "Active", Value = 1 };
```

### `RequestContext`

A read-only carrier of HTTP request information with handy shortcuts such as `Authorization`, `UserAgent`, and `Host`:

```csharp
var ctx = new RequestContext
{
    Method = "GET",
    Path = "/api/orders",
    Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer abc" }
};
string authToken = ctx.Authorization;   // "Bearer abc"
```

### Enums: `TextCaseType`, `TextTrimType`

- `TextCaseType`: `None` / `Upper` / `Lower` / `Title`
- `TextTrimType`: `None` / `Head` / `Tail` / `Both` / `All`

---

# `Nerosoft.Euonia.Collections`

```csharp
using Nerosoft.Euonia.Collections;
```

## `DequeCollection<T>`

A double-ended queue backed by a circular buffer with O(1) operations on both ends; also implements `IList<T>`:

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

## `EquatableReadOnlyList<T>`

A read-only list with value-based equality:

```csharp
var a = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });
var b = new EquatableReadOnlyList<int>(new[] { 1, 2, 3 });

bool same = a == b;    // true (element-wise comparison)
```

## `TypeList` / `TypeList<TBaseType>`

A `List<Type>` restricted to types assignable to a base type:

```csharp
class Animal { }
class Dog : Animal { }
class Cat : Animal { }

var types = new TypeList<Animal>();
types.Add<Dog>();
types.Add(typeof(Cat));
bool hasDog = types.Contains<Dog>();   // true
types.Remove<Cat>();
```

## `ObservableGroup` / `ReadOnlyObservableGroup` / `ObservableGroupedCollection`

XAML-binding-friendly observable grouped collections (each group is itself an `ObservableCollection` with an immutable `Key`):

```csharp
var grouped = new ObservableGroupedCollection<char, string>(
    words.GroupBy(w => w[0]));

var group = new ObservableGroup<string, int>("A", new[] { 1, 2, 3 });
group.Add(4);
group.Key;                              // "A"
IGrouping<string, int> g = group;       // usable directly as IGrouping

var ro = new ReadOnlyObservableGroup<string, int>("A", sourceCollection);
```

The non-generic `IReadOnlyObservableGroup` interface exposes just `Key` and `Count` for easy binding.

## `PageableCollection<T>` & `ViewCollection<T>`

Paged / view collections:

```csharp
var page = new PageableCollection<int>(new[] { 1, 2, 3, 4 })
{
    PageNumber = 2,
    PageSize = 2,
    TotalCount = 4
};
page.StartPosition;  // 3
page.EndPosition;    // 4
page.PageCount;      // 2

var view = new ViewCollection<string>(items, totalCount: 120);
view.TotalCount;     // 120
```

## `TreeView<TEntity>`

A lightweight tree node (all members virtual):

```csharp
var root = new TreeView<Category>
{
    Entity = new Category { Id = 1 },
    Children = new List<TreeView<Category>>()
};
root.Children.Add(new TreeView<Category> { Entity = new Category { Id = 2 } });
root["color"] = "red";                  // stored in the Properties dictionary
```

---

# `Nerosoft.Euonia.Reflection`

```csharp
using Nerosoft.Euonia.Reflection;
```

## `Reflect` & `Reflect<TTarget>`

Strongly-typed reflection utilities—extract `PropertyInfo` / `MethodInfo` from lambda expressions, read/write property values (including dotted paths), detect generic assignability, and locate methods:

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

MethodInfo mi = Reflect.GetMethodInfo(() => person.Work());   // Work()
bool list = Reflect.IsAssignableToGenericType(typeof(List<int>), typeof(IList<>)); // true

// Variant scoped to a fixed target type:
MethodInfo work = Reflect<Person>.GetMethod(p => p.Work());
PropertyInfo nameProperty = Reflect<Person>.GetProperty(p => p.Name);
Reflect<Person>.SetValue(person, "Dave", p => p.Name);
object city2 = Reflect<Person>.GetValue(person, "Address.City");
```

## `TypeHelper`

Value coercion across primitives, enums, `Nullable<T>`, and `TypeDescriptor` converters:

```csharp
int n = TypeHelper.CoerceValue<int, string>("42");                 // 42
var color = TypeHelper.CoerceValue<ConsoleColor>(typeof(string), "Red");
object maybeNull = TypeHelper.CoerceValue(typeof(int?), typeof(string), ""); // null
```

## `EnumHelper` & `EnumParser<T>`

```csharp
enum Status { [Description("Active")] Active, Inactive }

Status[] values = EnumHelper.GetEnumValues<Status>();
string[] names = EnumHelper.GetEnumNames<Status>();
var desc = EnumHelper.GetCustomerAttribute<DescriptionAttribute>(Status.Active);

if (EnumParser<DayOfWeek>.TryParse("Friday", out var day)) { }
var another = EnumParser<DayOfWeek>.Parse("Monday");
```

## `AssemblyHelper` & `AssemblyLoadContextManager`

Assembly scanning / loading and plugin-scoped cache management:

```csharp
IReadOnlyList<Type> allTypes = AssemblyHelper.GetAllTypes(typeof(string).Assembly);
var assemblies = AssemblyHelper.LoadAssemblies(AppContext.BaseDirectory,
    SearchOption.TopDirectoryOnly);

// .NET 5+: cache entries scoped to an AssemblyLoadContext
var entry = AssemblyLoadContextManager.CreateCacheInstance(
    objectType: typeof(MyPlugin),
    cachingItem: new object(),
    unloadAction: ctx => AssemblyLoadContextManager.RemoveFromCache(cache, ctx));
```

## `PropertyAccessorCache<T>`

Builds and caches lambda accessors for public instance properties:

```csharp
LambdaExpression expr = PropertyAccessorCache<Person>.Get(nameof(Person.Name));
// expr is shaped like p => p.Name
```

---

# `Nerosoft.Euonia.Threading`

```csharp
using Nerosoft.Euonia.Threading;
```

All coordination primitives return `AwaitableDisposable<T>`—always use `using (await x.LockAsync())`.

## `AsyncLock`

A non-reentrant async mutex:

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

An async counting semaphore (also usable as a multi-lock):

```csharp
var semaphore = new AsyncSemaphore(2);

using (await semaphore.LockAsync())   // consumes a slot
{
    // up to 2 concurrent callers
}

await semaphore.WaitAsync();
try { /* ... */ }
finally { semaphore.Release(); }
```

## `AsyncAutoResetEvent` / `AsyncManualResetEvent`

```csharp
var are = new AsyncAutoResetEvent();
are.Set();
await are.WaitAsync();   // returns immediately if set, then auto-resets

var mre = new AsyncManualResetEvent();
Task waiter = mre.WaitAsync();
mre.Set();               // completes all current and future waiters
await waiter;
mre.Reset();             // subsequent waiters block again
```

## `AsyncLazy<T>`

Thread-safe, directly awaitable async lazy initialization:

```csharp
var lazy = new AsyncLazy<int>(async () =>
{
    await Task.Delay(100);
    return 42;
}, AsyncLazyFlags.RetryOnFailure);

int answer = await lazy;   // or await lazy.Task;
```

## `AsyncMonitor` & `AsyncConditionVariable`

```csharp
var monitor = new AsyncMonitor();
using (await monitor.EnterAsync())
{
    while (!IsReady)
        await monitor.WaitAsync();   // releases the monitor while waiting
    monitor.Pulse();                 // wakes one waiter
}

// A condition variable is bound to a specific AsyncLock
var mutex = new AsyncLock();
var cv = new AsyncConditionVariable(mutex);
using (await mutex.LockAsync())
{
    while (queue.Count == 0)
        await cv.WaitAsync();        // releases the lock while waiting
    var item = queue.Dequeue();
}
```

## `AsyncCountdownEvent`

```csharp
var cde = new AsyncCountdownEvent(2);
Task allDone = cde.WaitAsync();
cde.Signal();   // 1 remaining
cde.Signal();   // 0 remaining -> allDone completes
await allDone;
```

## `AsyncProducerConsumerQueue<T>` & `AsyncCollection<T>`

```csharp
var queue = new AsyncProducerConsumerQueue<int>(maxCount: 5);
await queue.EnqueueAsync(10);
int value = await queue.DequeueAsync();

queue.CompleteAdding();
foreach (var item in queue.GetConsumingEnumerable()) { /* ... */ }

// AsyncCollection<T> has the same semantics and can wrap any IProducerConsumerCollection<T>
var collection = new AsyncCollection<int>(maxCount: 5);
await collection.AddAsync(1);
int item = await collection.TakeAsync();
```

## `PauseToken` / `PauseTokenSource`

Cooperative pausing (the "pause" counterpart of `CancellationToken`):

```csharp
var source = new PauseTokenSource();
var pauseToken = source.Token;

async Task WorkerAsync()
{
    while (true)
    {
        await pauseToken.WaitWhilePausedAsync();  // returns immediately when not paused
        await DoWorkAsync();
    }
}

source.IsPaused = true;    // the worker blocks here
source.IsPaused = false;   // resumes
```

## `DeferralManager` / `IDeferralSource`

Lets event args declare that async handlers are still running:

```csharp
var manager = new DeferralManager();

// inside an async event handler:
using (manager.DeferralSource.GetDeferral())
{
    await ProcessSlowlyAsync();   // only completes when the block exits
}

// after invoking all handlers:
await manager.WaitForDeferralsAsync();
```

## `State` & `StatefulMutex`

Cross-task mutual exclusion tied to a state token; acquisition fails once the state is invalidated:

```csharp
var mutex = new StatefulMutex();
var state = mutex.State;

using (await mutex.AcquireAsync(state)) { /* guarded region */ }

mutex.InvalidateState();
using (await mutex.AcquireAsync(state)) { }   // throws InvalidOperationException
```

## `NotifyTaskCompletion<TResult>`

An observable task wrapper that raises `PropertyChanged` on completion:

```csharp
var watcher = new NotifyTaskCompletion<int>(LoadAsync());
watcher.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(watcher.IsFaulted) && watcher.IsFaulted)
        Console.WriteLine(watcher.ErrorMessage);
};
```

## `AsyncContext` & `AsyncContextThread`

An async event loop running on the current thread:

```csharp
AsyncContext.Run(async () =>
{
    await Task.Delay(100);
    // AsyncContext.Current != null here
});

int result = AsyncContext.Run(async () =>
{
    await Task.Delay(100);
    return 42;
});

using (var thread = new AsyncContextThread())
{
    await thread.Factory.Run(async () => await DoWorkAsync()); // on the dedicated thread
    await thread.JoinAsync();
}
```

## Task Helpers

```csharp
Task done = TaskConstants.Completed;               // reused instances, no allocation
Task<int> zero = TaskConstants<int>.Default;

Task<int> t = TaskHelper.ExecuteAsTask(() => 42);
int doubled = TaskHelper.Run(async (int x) => new ValueTask<int>(x * 2), 21);

var lockResult = _lock.LockAsync();                // AwaitableDisposable<IDisposable>
using (await lockResult) { /* ... */ }
Task<IDisposable> asTask = _lock.LockAsync();      // implicit conversion to Task<T>

using (var source = new CancellationTokenTaskSource<int>(cts.Token))
{
    Task<int> first = await Task.WhenAny(source.Task, DoWorkAsync());
    if (first == source.Task) { /* cancelled */ }
}

SynchronizationContextSwitcher.NoContext(() => DoBlockingIoWork());
```

## `TimeoutValue` & `TimeoutTask`

```csharp
TimeoutValue timeout = TimeSpan.FromSeconds(5);   // TimeSpan implicitly converts
timeout.InMilliseconds;                           // 5000
timeout.IsInfinite;

using (var t = new TimeoutTask(TimeSpan.FromSeconds(5), CancellationToken.None))
{
    await t.Task;   // completes after 5s, or earlier when disposed
}
```

> Note: `TimeoutTask` is declared in the `Nerosoft.Euonia.Threading.Redis` namespace (as in the source).

---

# `Nerosoft.Euonia.Threading.Interop`

Turn callback/handle-based waits into `Task`:

```csharp
using Nerosoft.Euonia.Threading.Interop;
```

## `EventAsyncFactory`

A `Task` that completes on the next event occurrence:

```csharp
public event EventHandler? MyEvent;

Task<EventArguments<object, EventArgs>> next =
    EventAsyncFactory.FromEvent(h => MyEvent += h, h => MyEvent -= h);

MyEvent?.Invoke(this, EventArgs.Empty);
var args = await next;
```

## `WaitHandleAsyncFactory`

Wrap a `WaitHandle` wait:

```csharp
using var wh = new ManualResetEvent(false);
Task signalled = WaitHandleAsyncFactory.FromWaitHandle(wh);
wh.Set();
await signalled;

Task<bool> timedOut = WaitHandleAsyncFactory.FromWaitHandle(wh, TimeSpan.FromSeconds(2));
```

## `EventArguments<TSender, TEventArgs>`

A struct carrying the `(Sender, EventArgs)` pair.

---

# `Nerosoft.Euonia.Disposing`

```csharp
using Nerosoft.Euonia.Disposing;
```

Idempotent synchronous / asynchronous disposal primitives (later dispose calls either block until completion or become no-ops).

## Synchronous

```csharp
// Custom single-dispose base class
public sealed class Resource : SingleDisposable<FileStream>
{
    public Resource(FileStream stream) : base(stream) { }
    protected override void Dispose(FileStream context) => context.Dispose();
}

// Dispose a delegate
using (AnonymousDisposable.Create(() => Console.WriteLine("cleanup")))
{ }

// Dispose a set of disposables
using (var group = new CollectionDisposable(stream1, stream2))
{
    group.Add(someExtraDisposable);   // all disposed at the end of the block
}

var nothing = NoopDisposable.Instance;   // no-op singleton

// SingleNonblockingDisposable<T>: concurrent Dispose calls don't block; only the first runs cleanup
```

## Asynchronous

```csharp
public sealed class Resource : AsyncSingleDisposable<Stream>
{
    public Resource(Stream stream) : base(stream) { }
    protected override ValueTask DisposeAsync(Stream context) => context.DisposeAsync();
}

await using (var r = new Resource(stream)) { /* ... */ }

// Async delegate disposal (concurrent or serial, per DisposeFlags)
await using (var d = AsyncAnonymousDisposable.Create(() => stream.FlushAsync()))
{
    await d.AddAsync(() => Logger.LogAsync("done"));
}

// Async composite disposal with a serial flag
await using (var serial = new AsyncCollectionDisposable(resources, DisposeFlags.ExecuteSerially))
{ }

await using var noop = new AsyncNoopDisposable();   // no-op

// AsyncSingleNonblockingDisposable<T>: concurrent DisposeAsync calls don't await
```

## `DisposeFlags`

`ExecuteConcurrently` (default, concurrent) / `ExecuteSerially` (serial).

---

# Exceptions (`System` namespace)

All exceptions live in the `System` namespace so they can be thrown/caught without extra `using` directives.

## HTTP Status Exceptions

Each concrete exception derives directly from `System.Exception` and is annotated with `[HttpStatusCode]`:

| Exception | HTTP status |
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

## `HttpStatusException`

A base exception carrying an arbitrary status code via its `StatusCode` property:

```csharp
throw new HttpStatusException(HttpStatusCode.Forbidden, "You cannot do this.");
```

## `HttpStatusCodeAttribute`

Declaratively maps an exception type to an HTTP status code; the hosting layer reads it via reflection:

```csharp
var status = ex switch
{
    HttpStatusException http => http.StatusCode,
    _ => ex.GetType().GetCustomAttribute<HttpStatusCodeAttribute>()?.StatusCode
         ?? HttpStatusCode.InternalServerError
};
```

## `IExceptionPrompt` & `ExceptionPrompt`

A pluggable resolver for user-facing prompt text:

```csharp
ExceptionPrompt.AddPrompt(new MyPromptProvider());   // register a custom provider
string prompt = ExceptionPrompt.GetPrompt(ex);        // falls back to "Application error"
```

## Business & General Exceptions

```csharp
throw new NotFoundException("Order 42 was not found.");     // 404
throw new BusinessException("ORDER_OUT_OF_STOCK", "Out of stock"); // carries a stable code
throw new ConfigurationException("Missing required configuration.");
throw new InvalidValueException("The value is invalid.");
```

---

# `Nerosoft.Euonia.Security`

```csharp
using Nerosoft.Euonia.Security;
```

## `UserPrincipal`

A read-only, typed accessor over a `ClaimsPrincipal`:

```csharp
var principal = new UserPrincipal(httpContext.User);   // or new ClaimsPrincipal(identity)

if (principal.IsAuthenticated && principal.IsInRole("Admin"))
{
    string id = principal.UserId;       // resolves sub / NameIdentifier by auth scheme
    string name = principal.Username;   // resolves name / ClaimTypes.Name by auth scheme
    string tenant = principal.Tenant;
    IEnumerable<string> roles = principal.Roles;
}

bool any = principal.IsInRoles("Admin,User", separator: ",");
var claim = principal.FindClaim("email");
```

## `UserClaimTypes`

OIDC/JWT claim name constants (`Subject = "sub"`, `Name = "name"`, `Role = "role"`, `Tenant = "tenant"`, etc.).

## Account & Credential Exceptions

Derived from `System.Security.Authentication.AuthenticationException`:

- `AccountException` (`Identity` + `Details`) → `AccountNotFoundException` / `AccountExpiredException` / `AccountLockedException`
- `CredentialException` (`Credential` + `Details`) → `CredentialNotFoundException` / `CredentialIncorrectException` / `CredentialExpiredException`

```csharp
throw new AccountNotFoundException("user@example.com", "The account does not exist.");
throw new CredentialIncorrectException(userName, "The supplied credential is incorrect.");

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

`ICommand` implementations suitable for MVVM (WPF/WinUI, etc.).

## `DelegateCommand` / `DelegateCommand<T>`

Delegate-based commands:

```csharp
var cmd = new DelegateCommand<string>(
    s => Save(s),
    s => !string.IsNullOrEmpty(s));

if (cmd.CanExecute(text))
{
    cmd.Execute(text);
}
cmd.RaiseCanExecuteChanged();
```

## `GenericCommand` / `GenericCommand<T>`

Callback-property commands (no constructor arguments):

```csharp
var cmd = new GenericCommand
{
    CanExecuteCallback = p => p is int n && n > 0,
    ExecuteCallback = p => Process((int)p!)
};

cmd.Execute(42);
```

---

# `System.ComponentModel.DataAnnotations` Validation Attributes

## `GuidAttribute`

Validates that a value is a valid GUID string:

```csharp
public class OrderModel
{
    [Required, Guid]
    public string OrderId { get; set; }
}
```

## `CollectionCountAttribute`

Validates the element count of a collection property:

```csharp
public class PagedModel
{
    [CollectionCount(1, 100)]   // at least 1, at most 100 items
    public ICollection<string> Tags { get; set; }
}
```

Use `AllowNull` to control whether null is permitted.

---

# Global Extensions (`Extensions`) & `Nerosoft.Euonia.*` Extensions

The library provides a `public static partial class Extensions` in the global namespace (split by domain). Commonly used members:

```csharp
// String
string kebab = "HelloWorld".ToKebabCase();     // "hello-world"
string snake = "HelloWorld".ToSnakeCase();     // "hello_world"
string camel = "hello_world".ToCamelCase();    // "helloWorld"
bool isMail = "a@b.com".IsEmail();             // true
var trimmed = "  text  ".Trim(TextTrimType.Both);
var normalized = "title".Normalize(TextCaseType.Title);
string masked = "13800001111".Mask(3, 4);      // mask middle digits

// Collections
items.ForEach(x => Console.WriteLine(x));
var filtered = people.WhereIf(onlyAdults, p => p.Age >= 18);
var v = dict.GetValueOrDefault("k");
dict.GetOrAdd("k", () => new Value());
dict.AddIfNotContains("k", value);
var sorted = deps.SortByDependencies();        // topological sort
var shuffled = list.Shuffle();

// Object / Reflect
var session = obj.As<ISession>();
var to = obj.To<double>();                     // Convert.ChangeType based conversion
bool hot = temp.IsIn(30, 40);                  // membership check
bool has = type.HasAttribute<MyAttribute>();
string fq = typeof(Foo).GetFullNameWithAssemblyName();

// Exception / Event
string full = ex.GetFullMessage();
string root = ex.GetRootMessage();
handler.CheckAndInvoke(this, e);               // null-safe raise

// Enum
bool valid = someEnum.IsValid();
string desc = DayOfWeek.Monday.GetDescription();

// Threading / Task
await someTask.WaitAsync(ct);
Task ignored = someTask.Ignore();              // observe and swallow exceptions
SynchronizationContextSwitcher.NoContext(() => { });

// Claims / User
Guid uid = user.GetUserIdOfGuid();
user.EnsureInRoles(new[] { "Admin" }, "Administrators only");
await user.EnsureInRolesAsync(new[] { "Admin" }, ct);
```

There are also extension methods for observable grouped collections (`Nerosoft.Euonia.Collections`):

```csharp
grouped.AddItem("A", item);
grouped.RemoveGroup("A");
var first = grouped.FirstOrDefault("A");
```

# Documentation

For more detailed information and advanced usage, refer to the official documentation on the [Euonia GitHub repository](https://github.com/NerooftDev/Euonia).