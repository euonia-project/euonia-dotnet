# Euonia.Core 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Core |
| 公共类型 | 161 |
| 命名空间 | 15 |

## 命名空间 (global)

### Check

**类型**：static class

- static System.Boolean Ensure(Func<System.Boolean> condition, System.String message, System.Object[] args)
- static System.Boolean Ensure(System.Boolean condition, System.String message, System.Object[] args)
- static T Ensure<T>(T value, Func<T, System.Boolean> action, System.String message)
- static void Ensure<T>(T value, Func<T, System.Boolean> action, Action<T> failsAction)
- static CheckResult<T> Ensure<T>(T value, Func<T, System.Boolean> action)
- static System.Type EnsureAssignableTo<TBaseType>(System.Type type, System.String parameter)
- static System.String EnsureIsMatch(System.String value, System.String parameter, System.String pattern, System.Text.RegularExpressions.RegexOptions options = 0)
- static void EnsureIsMatch(System.String value, System.String parameter, System.String pattern, Action<System.String> failsAction, System.Text.RegularExpressions.RegexOptions options = 0)
- static System.String EnsureLengthInRange(System.String value, System.String parameter, System.Int32 maxLength, System.Int32 minLength = 0)
- static T EnsureNotNull<T>(T value, System.String parameter)
- static T EnsureNotNull<T>(T value, System.String parameter, System.String message)
- static System.String EnsureNotNull(System.String value, System.String parameter, System.Int32 maxLength = 2147483647, System.Int32 minLength = 0)
- static System.String EnsureNotNullOrEmpty(System.String value, System.String parameter)
- static void EnsureNotNullOrEmpty(System.String value, System.String parameter, Action<System.String> failsAction)
- static IEnumerable<T> EnsureNotNullOrEmpty<T>(IEnumerable<T> collection, System.String parameter)
- static System.String EnsureNotNullOrWhiteSpace(System.String value, System.String parameter)
- static void EnsureNotNullOrWhiteSpace(System.String value, System.String parameter, Action<System.String> failsAction)

### CheckResult<TValue>

**类型**：class

- System.Boolean IsValid { get; }
- TValue Value { get; }
- System.Boolean Equals(System.Object obj)
- CheckResult<TValue> Failure(Action<TValue> action)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(CheckResult<TValue> left, CheckResult<TValue> right)
- static CheckResult<TValue> operator Implicit(TValue value)
- static TValue operator Implicit(CheckResult<TValue> result)
- static System.Boolean operator Inequality(CheckResult<TValue> left, CheckResult<TValue> right)
- CheckResult<TValue> Success(Action<TValue> action)
- void Then(Action<TValue, System.Boolean> action)

### Extensions

**类型**：static class

- static System.String AbbreviatedDayName(System.DayOfWeek dayOfWeek)
- static System.String AbbreviatedDayName(System.DayOfWeek dayOfWeek, System.Globalization.CultureInfo cultureInfo)
- static void AddFirst<T>(IList<T> source, T item)
- static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue value)
- static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue[] collection)
- static ObservableGroup<TKey, TValue> AddGroup<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, IEnumerable<TValue> collection)
- static System.Boolean AddIfNotContains<T>(ICollection<T> source, T item)
- static IEnumerable<T> AddIfNotContains<T>(ICollection<T> source, IEnumerable<T> items)
- static System.Boolean AddIfNotContains<T>(ICollection<T> source, Func<T, System.Boolean> predicate, Func<T> itemFactory)
- static ObservableGroup<TKey, TValue> AddItem<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item)
- static void AddLast<T>(IList<T> source, T item)
- static T As<T>(System.Object obj)
- static System.String AsFormat(System.String source, System.Object[] args)
- static ValueTask<T> AsValueTask<T>(Task<T> task)
- static System.Threading.Tasks.ValueTask AsValueTask(System.Threading.Tasks.Task task)
- static ValueTask<T> AsValueTask<T>(T value)
- static void CheckAndInvoke(System.EventHandler handler, System.Object sender)
- static void CheckAndInvoke(System.EventHandler handler, System.Object sender, System.EventArgs e)
- static void CheckAndInvoke<TEventArgs>(EventHandler<TEventArgs> handler, System.Object sender, TEventArgs e)
- static System.String Collapse(System.String[] parts)
- static System.Boolean Contains(IEnumerable<System.String> source, System.String value, System.StringComparison comparison)
- static PageableCollection<T> Convert<T>(PageableCollection<T> source, System.Int32 index, System.Int32 size)
- static System.Threading.Tasks.ValueTask ConvertToVoid<TResult>(ValueTask<TResult> task)
- static System.String DecodeHtml(System.String source)
- static System.String DefaultIfNullOrWhiteSpace(System.String source, System.String default)
- static TValue ElementAt<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, System.Int32 index)
- static TValue ElementAtOrDefault<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, System.Int32 index)
- static void EnsureAuthenticated(Nerosoft.Euonia.Security.UserPrincipal user)
- static System.String EnsureEndsWith(System.String source, System.Char c, System.StringComparison comparisonType = 4)
- static void EnsureInRoles(Nerosoft.Euonia.Security.UserPrincipal user, IEnumerable<System.String> roles, System.String message)
- static void EnsureInRoles(Nerosoft.Euonia.Security.UserPrincipal user, IEnumerable<System.String> roles, System.Action successCallback = null, System.Action failureCallback = null)
- static System.Threading.Tasks.Task EnsureInRolesAsync(Nerosoft.Euonia.Security.UserPrincipal user, IEnumerable<System.String> roles, Func<System.Threading.Tasks.Task> successCallback = null, Func<System.Threading.Tasks.Task> failureCallback = null)
- static System.String EnsureStartsWith(System.String source, System.Char c, System.StringComparison comparisonType = 4)
- static System.Boolean Equals<T>(IEnumerable<T> source, IEnumerable<T> dest)
- static System.Int32 FindIndex<T>(IList<T> source, Predicate<T> selector)
- static ObservableGroup<TKey, TValue> First<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key)
- static ObservableGroup<TKey, TValue> FirstOrDefault<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key)
- static System.String FixHtml(System.String source)
- static void ForEach<T>(IEnumerable<T> source, Action<T> action)
- static T GetAttribute<T>(System.Enum enum)
- static System.Type[] GetBaseClasses(System.Type type, System.Boolean includeObject = true)
- static System.Type[] GetBaseClasses(System.Type type, System.Type stoppingType, System.Boolean includeObject = true)
- static System.Byte[] GetBytes(System.String source, System.Text.Encoding encoding)
- static System.String GetDescription(System.Enum enum)
- static System.String GetDescription(System.Enum enum, System.Resources.ResourceManager resourceManager, System.Globalization.CultureInfo resourceCulture = null)
- static System.String GetDescription(System.Enum enum, System.Type resourceType)
- static System.String GetDisplayName(System.Enum enum)
- static IEnumerable<TEnum> GetFlags<TEnum>(System.Enum enum)
- static IEnumerable<System.Enum> GetFlags(System.Enum enum)
- static System.String GetFullMessage(System.Exception exception, System.Int32 maxDepths = 3)
- static System.String GetFullNameWithAssemblyName(System.Type type)
- static IEnumerable<System.Enum> GetIndividualFlags(System.Enum enum)
- static T GetOrAdd<T>(IList<T> source, Func<T, System.Boolean> selector, Func<T> factory)
- static TValue GetOrAdd<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
- static TValue GetOrAdd<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
- static TValue GetOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
- static TValue GetOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
- static TValue GetOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
- static TValue GetOrDefault<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
- static System.Type GetPropertyType(System.Type propertyType)
- static System.String GetRootMessage(System.Exception exception)
- static System.Guid GetUserIdOfGuid(Nerosoft.Euonia.Security.UserPrincipal user)
- static System.Int32 GetUserIdOfInt32(Nerosoft.Euonia.Security.UserPrincipal user)
- static System.Int64 GetUserIdOfInt64(Nerosoft.Euonia.Security.UserPrincipal user)
- static System.Byte[] GetUtf8Bytes(System.String source)
- static TValue GetValue<TValue>(IDictionary<System.String, TValue> source, System.String key, System.StringComparison comparison)
- static TValue GetValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key)
- static System.Boolean HasAttribute<TAttribute>(System.Type type, System.Boolean inherit = true)
- static System.Boolean HasAttribute<TAttribute>(System.Reflection.MethodInfo method, System.Boolean inherit = true)
- static System.Boolean HasAttribute<TAttribute>(System.Object source, out TAttribute attribute, System.Boolean inherit = true)
- static System.Boolean HasAttribute<TAttribute>(System.Object source, out IEnumerable<TAttribute> attributes, System.Boolean inherit = true)
- static T If<T>(T obj, System.Boolean condition, Func<T, T> func)
- static T If<T>(T obj, System.Boolean condition, Action<T> action)
- static void Ignore(System.Threading.Tasks.Task this)
- static void Ignore<T>(Task<T> this)
- static System.Int32 IndexOf<T>(IEnumerable<T> enumerable, T item)
- static System.Int32 IndexOf<T>(IEnumerable<T> enumerable, Func<T, System.Boolean> predicate)
- static void InsertAfter<T>(IList<T> source, T existingItem, T item)
- static void InsertAfter<T>(IList<T> source, Predicate<T> selector, T item)
- static void InsertBefore<T>(IList<T> source, T existingItem, T item)
- static void InsertBefore<T>(IList<T> source, Predicate<T> selector, T item)
- static ObservableGroup<TKey, TValue> InsertItem<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, System.Int32 index, TValue item)
- static void InsertRange<T>(IList<T> source, System.Int32 index, IEnumerable<T> items)
- static System.Boolean IsAnonymousType(System.Type type)
- static System.Boolean IsAssignableTo<TTarget>(System.Type type)
- static System.Boolean IsAssignableTo(System.Type type, System.Type targetType)
- static System.Boolean IsAssignableToGeneric(System.Type type, System.Type genericType)
- static System.Boolean IsAsync(System.Reflection.MethodInfo method)
- static System.Boolean IsBetween<T>(T value, T minValue, T maxValue)
- static System.Boolean IsCharacterString(System.String source)
- static System.Boolean IsDecimal(System.String source)
- static System.Boolean IsEmail(System.String source)
- static System.Boolean IsExtends<T>(System.Type type)
- static System.Boolean IsExtends(System.Type type, System.Type targetType)
- static System.Boolean IsImplements<T>(System.Type type)
- static System.Boolean IsImplements(System.Type type, System.Type targetType)
- static System.Boolean IsImplementsGeneric(System.Type type, System.Type targetType)
- static System.Boolean IsIn<T>(T item, T[] list)
- static System.Boolean IsIn<T>(T item, IEnumerable<T> items)
- static System.Boolean IsIn<T>(T item, IEnumerable<T> items, IEqualityComparer<T> comparer)
- static System.Boolean IsNotInRange<T>(T value, T minValue, T maxValue)
- static System.Boolean IsNullOrEmpty(System.Collections.IEnumerable source)
- static System.Boolean IsNullOrEmpty<T>(IEnumerable<T> source)
- static System.Boolean IsNullOrEmpty(System.String source)
- static System.Boolean IsNullOrWhiteSpace(System.String source)
- static System.Boolean IsNumeric(System.String source)
- static System.Boolean IsPhoneNumber(System.String source)
- static System.Boolean IsPrimitiveType(System.Type type)
- static System.Boolean IsValid<TEnum>(TEnum enum)
- static System.String Join<T>(IEnumerable<T> values, System.String separator)
- static System.String Join<T>(IEnumerable<T> values, System.String separator, System.Int32 startIndex, System.Int32 count)
- static System.String JoinAsString(IEnumerable<System.String> source, System.String separator)
- static System.String JoinAsString<T>(IEnumerable<T> source, System.String separator)
- static System.String Left(System.String source, System.Int32 length)
- static System.String Mask(System.String source, System.Int32 start, System.Int32 length, System.Char maskChar = *)
- static void MoveItem<T>(List<T> source, Predicate<T> selector, System.Int32 targetIndex)
- static System.String Normalize(System.String source, System.TextCaseType caseType)
- static System.String NormalizeLineEndings(System.String source)
- static System.Security.Claims.ClaimsPrincipal NormalizeScopeClaims(System.Security.Claims.ClaimsPrincipal principal)
- static System.Int32 NthIndexOf(System.String source, System.Char c, System.Int32 n)
- static List<Task<T>> OrderByCompletion<T>(IEnumerable<Task<T>> this)
- static List<System.Threading.Tasks.Task> OrderByCompletion(IEnumerable<System.Threading.Tasks.Task> this)
- static PageableCollection<T> Paginate<T>(IList<T> source, System.Int64 totalCount, System.Int32 index, System.Int32 size)
- static System.Threading.Tasks.Task PostAsync(System.Threading.SynchronizationContext context, System.Action action)
- static Task<T> PostAsync<T>(System.Threading.SynchronizationContext context, Func<T> action)
- static System.Threading.Tasks.Task PostAsync(System.Threading.SynchronizationContext context, Func<System.Threading.Tasks.Task> action)
- static Task<T> PostAsync<T>(System.Threading.SynchronizationContext context, Func<Task<T>> action)
- static System.Exception PrepareForRethrow(System.Exception exception)
- static IReadOnlyCollection<T> Reify<T>(IEnumerable<T> source)
- static IList<T> RemoveAll<T>(ICollection<T> source, Func<T, System.Boolean> predicate)
- static void RemoveAll<T>(ICollection<T> source, IEnumerable<T> items)
- static System.Boolean RemoveGroup<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key)
- static void RemoveItem<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, TValue item, System.Boolean removeGroupIfEmpty = true)
- static void RemoveItemAt<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, System.Int32 index, System.Boolean removeGroupIfEmpty = true)
- static System.String RemovePostFix(System.String source, System.String[] postFixes)
- static System.String RemovePostFix(System.String source, System.StringComparison comparisonType, System.String[] postFixes)
- static System.String RemovePreFix(System.String source, System.String[] preFixes)
- static System.String RemovePreFix(System.String source, System.StringComparison comparisonType, System.String[] preFixes)
- static System.String ReplaceFirst(System.String source, System.String search, System.String replace, System.StringComparison comparisonType = 4)
- static void ReplaceOne<T>(IList<T> source, Predicate<T> selector, T item)
- static void ReplaceOne<T>(IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
- static void ReplaceOne<T>(IList<T> source, T item, T replaceWith)
- static void ReplaceWhile<T>(IList<T> source, Predicate<T> selector, T item)
- static void ReplaceWhile<T>(IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
- static System.String Right(System.String source, System.Int32 length)
- static System.Threading.Tasks.Task Run(System.Threading.Tasks.TaskFactory this, System.Action action)
- static Task<TResult> Run<TResult>(System.Threading.Tasks.TaskFactory this, Func<TResult> action)
- static System.Threading.Tasks.Task Run(System.Threading.Tasks.TaskFactory this, Func<System.Threading.Tasks.Task> action)
- static Task<TResult> Run<TResult>(System.Threading.Tasks.TaskFactory this, Func<Task<TResult>> action)
- static System.String SafeSubstring(System.String source, System.Int32 index, System.Int32 length = 0)
- static void Send(System.Threading.SynchronizationContext context, System.Action action)
- static T Send<T>(System.Threading.SynchronizationContext context, Func<T> action)
- static IDictionary<TKey, TValue> Set<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key, TValue value)
- static ObservableGroup<TKey, TValue> SetItem<TKey, TValue>(ObservableGroupedCollection<TKey, TValue> source, TKey key, System.Int32 index, TValue item)
- static IEnumerable<T> Shuffle<T>(IEnumerable<T> enumerable)
- static List<T> SortByDependencies<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, IEqualityComparer<T> comparer = null)
- static System.String[] Split(System.String source, System.String separator)
- static System.String[] Split(System.String source, System.String separator, System.StringSplitOptions options)
- static System.String[] SplitToLines(System.String source)
- static System.String[] SplitToLines(System.String source, System.StringSplitOptions options)
- static T To<T>(System.Object obj)
- static System.String ToBase64(System.String source)
- static System.String ToCamelCase(System.String source, System.Boolean useCurrentCulture = false)
- static T ToEnum<T>(System.String source)
- static T ToEnum<T>(System.String source, System.Boolean ignoreCase)
- static System.String ToKebabCase(System.String source, System.Boolean useCurrentCulture = false)
- static System.String ToMd5(System.String source)
- static ObservableCollection<T> ToObservable<T>(IEnumerable<T> source)
- static System.String ToPascalCase(System.String source, System.Boolean useCurrentCulture = false)
- static System.String ToSentenceCase(System.String source, System.Boolean useCurrentCulture = false)
- static System.String ToSnakeCase(System.String source)
- static ViewCollection<T> ToView<T>(PageableCollection<T> source)
- static System.String Trim(System.String source, System.TextTrimType type)
- static System.String Truncate(System.String source, System.Int32 length)
- static System.String Truncate(System.String source, System.Int32 length, System.Boolean ellipsis)
- static System.String TruncateFromBeginning(System.String source, System.Int32 maxLength)
- static System.String TruncateWithPostfix(System.String source, System.Int32 maxLength)
- static System.String TruncateWithPostfix(System.String source, System.Int32 maxLength, System.String postfix)
- static System.Boolean TryCompleteFromCompletedTask<TResult, TSourceResult>(TaskCompletionSource<TResult> this, Task<TSourceResult> task)
- static System.Boolean TryCompleteFromCompletedTask<TResult>(TaskCompletionSource<TResult> this, System.Threading.Tasks.Task task, Func<TResult> resultFunc)
- static TValue TryGetOrSetValue<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key)
- static void TryGetValue<TKey, TRef>(IDictionary<TKey, System.Object> source, TKey key, Action<TRef> func)
- static TValue TryGetValue<TValue>(IDictionary<System.String, TValue> source, System.String key, TValue defaultValue, System.StringComparison comparison)
- static void TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key, Action<TValue> func)
- static void TryGetValue<TKey, TValue, TRef>(IDictionary<TKey, TValue> source, TKey key, Action<TRef> func)
- static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key)
- static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> source, TKey key, TValue defaultValue)
- static void WaitAndUnwrapException(System.Threading.Tasks.Task task)
- static void WaitAndUnwrapException(System.Threading.Tasks.Task task, System.Threading.CancellationToken cancellationToken)
- static TResult WaitAndUnwrapException<TResult>(Task<TResult> task)
- static TResult WaitAndUnwrapException<TResult>(Task<TResult> task, System.Threading.CancellationToken cancellationToken)
- static System.Threading.Tasks.Task WaitAsync(System.Threading.Tasks.Task this, System.Threading.CancellationToken cancellationToken)
- static Task<TResult> WaitAsync<TResult>(Task<TResult> this, System.Threading.CancellationToken cancellationToken)
- static void WaitWithoutException(System.Threading.Tasks.Task task)
- static void WaitWithoutException(System.Threading.Tasks.Task task, System.Threading.CancellationToken cancellationToken)
- static System.Threading.Tasks.Task WhenAll(IEnumerable<System.Threading.Tasks.Task> this)
- static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> this)
- static Task<System.Threading.Tasks.Task> WhenAny(IEnumerable<System.Threading.Tasks.Task> this, System.Threading.CancellationToken cancellationToken)
- static Task<System.Threading.Tasks.Task> WhenAny(IEnumerable<System.Threading.Tasks.Task> this)
- static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> this, System.Threading.CancellationToken cancellationToken)
- static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> this)
- static IEnumerable<T> WhereIf<T>(IEnumerable<T> source, System.Boolean condition, Func<T, System.Boolean> predicate)
- static IEnumerable<T> WhereIf<T>(IEnumerable<T> source, System.Boolean condition, Func<T, System.Int32, System.Boolean> predicate)

### ObjectId

**类型**：struct

- new ObjectId(System.Int32 value)
- new ObjectId(System.Int64 value)
- new ObjectId(System.Guid value)
- new ObjectId(System.String value)
- System.Object Value { get; }
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- static ObjectId Guid(System.GuidType type)
- static System.Guid NewGuid(System.GuidType type)
- static System.String NewRandomId(System.Int64 seed)
- static System.Int64 NewSnowflake()
- static System.String NewUlid()
- static System.Boolean operator Equality(ObjectId id1, ObjectId id2)
- static ObjectId operator Implicit(System.Int64 id)
- static System.Int64 operator Implicit(ObjectId id)
- static ObjectId operator Implicit(System.Int32 id)
- static System.Int32 operator Implicit(ObjectId id)
- static ObjectId operator Implicit(System.String id)
- static System.String operator Implicit(ObjectId id)
- static ObjectId operator Implicit(System.Guid id)
- static System.Guid operator Implicit(ObjectId id)
- static System.Boolean operator Inequality(ObjectId id1, ObjectId id2)
- static ObjectId Random()
- static ObjectId Snowflake()
- static ObjectId Ulid()

### ObjectId<T>

**类型**：struct

- new ObjectId<T>(T value)
- T Value { get; }
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(ObjectId<T> id1, ObjectId<T> id2)
- static ObjectId<T> operator Implicit(T id)
- static T operator Implicit(ObjectId<T> id)
- static System.Boolean operator Inequality(ObjectId<T> id1, ObjectId<T> id2)

## 命名空间 Nerosoft.Euonia

### Nerosoft.Euonia.IKeyTransformer

**类型**：interface

- System.String Transform(System.String key)

### Nerosoft.Euonia.IKeyTransformSuppressor

**类型**：interface

- Nerosoft.Euonia.IKeyTransformSuppressScope Suppress()

### Nerosoft.Euonia.IKeyTransformSuppressScope

**类型**：interface
接口 System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.KeyTransformSuppression

**类型**：static class

- static System.Boolean IsSuppressed { get; }
- static Nerosoft.Euonia.IKeyTransformSuppressScope Suppress()

### Nerosoft.Euonia.NullKeyTransformer

**类型**：class
接口 Nerosoft.Euonia.IKeyTransformer

- new Nerosoft.Euonia.NullKeyTransformer()
- static readonly Nerosoft.Euonia.NullKeyTransformer Instance
- System.String Transform(System.String key)

## 命名空间 Nerosoft.Euonia.Collections

### DequeCollection<T>

**类型**：class
接口 IList<T>、ICollection<T>、IEnumerable<T>、System.Collections.IEnumerable、IReadOnlyList<T>、IReadOnlyCollection<T>、System.Collections.IList、System.Collections.ICollection

- new DequeCollection<T>(System.Int32 capacity)
- new DequeCollection<T>(IEnumerable<T> collection)
- new DequeCollection<T>()
- System.Int32 Capacity { get; set; }
- System.Int32 Count { get; set; }
- T this[System.Int32 index] { get; set; }
- void AddToBack(T value)
- void AddToFront(T value)
- void Clear()
- IEnumerator<T> GetEnumerator()
- System.Int32 IndexOf(T item)
- void Insert(System.Int32 index, T item)
- void InsertRange(System.Int32 index, IEnumerable<T> collection)
- System.Boolean Remove(T item)
- void RemoveAt(System.Int32 index)
- T RemoveFromBack()
- T RemoveFromFront()
- void RemoveRange(System.Int32 offset, System.Int32 count)
- T[] ToArray()

### EquatableReadOnlyList<T>

**类型**：struct
接口 IReadOnlyList<T>、IEnumerable<T>、System.Collections.IEnumerable、IReadOnlyCollection<T>、IEquatable<EquatableReadOnlyList<T>>

- new EquatableReadOnlyList<T>(IEnumerable<T> items)
- System.Int32 Count { get; }
- T this[System.Int32 index] { get; }
- System.Boolean Equals(EquatableReadOnlyList<T> other)
- System.Boolean Equals(System.Object obj)
- IEnumerator<T> GetEnumerator()
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
- static System.Boolean operator Inequality(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)

### Nerosoft.Euonia.Collections.IReadOnlyObservableGroup

**类型**：interface
接口 System.ComponentModel.INotifyPropertyChanged

- System.Int32 Count { get; }
- System.Object Key { get; }

### Nerosoft.Euonia.Collections.ITypeList

**类型**：interface
接口 ITypeList<System.Object>、IList<System.Type>、ICollection<System.Type>、IEnumerable<System.Type>、System.Collections.IEnumerable

_无公共成员。_

### ITypeList<TBaseType>

**类型**：interface
接口 IList<System.Type>、ICollection<System.Type>、IEnumerable<System.Type>、System.Collections.IEnumerable

- void Add<T>()
- System.Boolean Contains<T>()
- void Remove<T>()
- System.Boolean TryAdd<T>()

### ObservableGroup<TKey, TValue>

**类型**：class
基类 ObservableCollection<TValue>；接口 IList<TValue>、ICollection<TValue>、IEnumerable<TValue>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<TValue>、IReadOnlyCollection<TValue>、System.Collections.Specialized.INotifyCollectionChanged、System.ComponentModel.INotifyPropertyChanged、IGrouping<TKey, TValue>、Nerosoft.Euonia.Collections.IReadOnlyObservableGroup

- new ObservableGroup<TKey, TValue>(TKey key)
- new ObservableGroup<TKey, TValue>(IGrouping<TKey, TValue> grouping)
- new ObservableGroup<TKey, TValue>(TKey key, IEnumerable<TValue> collection)
- TKey Key { get; }

### ObservableGroupedCollection<TKey, TValue>

**类型**：class
基类 ObservableCollection<ObservableGroup<TKey, TValue>>；接口 IList<ObservableGroup<TKey, TValue>>、ICollection<ObservableGroup<TKey, TValue>>、IEnumerable<ObservableGroup<TKey, TValue>>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<ObservableGroup<TKey, TValue>>、IReadOnlyCollection<ObservableGroup<TKey, TValue>>、System.Collections.Specialized.INotifyCollectionChanged、System.ComponentModel.INotifyPropertyChanged

- new ObservableGroupedCollection<TKey, TValue>()
- new ObservableGroupedCollection<TKey, TValue>(IEnumerable<IGrouping<TKey, TValue>> collection)

### PageableCollection<T>

**类型**：class
基类 List<T>；接口 IList<T>、ICollection<T>、IEnumerable<T>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<T>、IReadOnlyCollection<T>

- new PageableCollection<T>(IEnumerable<T> items)
- new PageableCollection<T>(T[] items)
- System.Int64 EndPosition { get; }
- System.Int64 PageCount { get; }
- System.Int64 PageNumber { get; set; }
- System.Int64 PageSize { get; set; }
- System.Int64 StartPosition { get; }
- System.Int64 TotalCount { get; set; }

### ReadOnlyObservableGroup<TKey, TValue>

**类型**：class
基类 ReadOnlyObservableCollection<TValue>；接口 IList<TValue>、ICollection<TValue>、IEnumerable<TValue>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<TValue>、IReadOnlyCollection<TValue>、System.Collections.Specialized.INotifyCollectionChanged、System.ComponentModel.INotifyPropertyChanged、IGrouping<TKey, TValue>、Nerosoft.Euonia.Collections.IReadOnlyObservableGroup

- new ReadOnlyObservableGroup<TKey, TValue>(TKey key, ObservableCollection<TValue> collection)
- new ReadOnlyObservableGroup<TKey, TValue>(ObservableGroup<TKey, TValue> group)
- new ReadOnlyObservableGroup<TKey, TValue>(TKey key, IEnumerable<TValue> collection)
- TKey Key { get; }

### TreeView<TEntity>

**类型**：class

- new TreeView<TEntity>()
- ICollection<TreeView<TEntity>> Children { get; set; }
- TEntity Entity { get; set; }
- System.Object this[System.String key] { get; set; }
- IDictionary<System.String, System.Object> Properties { get; set; }

### Nerosoft.Euonia.Collections.TypeList

**类型**：class
基类 TypeList<System.Object>；接口 ITypeList<System.Object>、IList<System.Type>、ICollection<System.Type>、IEnumerable<System.Type>、System.Collections.IEnumerable、Nerosoft.Euonia.Collections.ITypeList

- new Nerosoft.Euonia.Collections.TypeList()

### TypeList<TBaseType>

**类型**：class
接口 ITypeList<TBaseType>、IList<System.Type>、ICollection<System.Type>、IEnumerable<System.Type>、System.Collections.IEnumerable

- new TypeList<TBaseType>()
- System.Int32 Count { get; }
- System.Boolean IsReadOnly { get; }
- System.Type this[System.Int32 index] { get; set; }
- void Add<T>()
- void Add(System.Type item)
- void Clear()
- System.Boolean Contains<T>()
- System.Boolean Contains(System.Type item)
- void CopyTo(System.Type[] array, System.Int32 arrayIndex)
- IEnumerator<System.Type> GetEnumerator()
- System.Int32 IndexOf(System.Type item)
- void Insert(System.Int32 index, System.Type item)
- void Remove<T>()
- System.Boolean Remove(System.Type item)
- void RemoveAt(System.Int32 index)
- System.Boolean TryAdd<T>()

### ViewCollection<T>

**类型**：class

- new ViewCollection<T>()
- new ViewCollection<T>(IList<T> items)
- new ViewCollection<T>(IList<T> items, System.Int64 totalCount)
- ICollection<T> Items { get; }
- System.Int64 TotalCount { get; set; }

## 命名空间 Nerosoft.Euonia.Disposing

### Nerosoft.Euonia.Disposing.AnonymousDisposable

**类型**：class
基类 SingleDisposable<System.Action>；接口 System.IDisposable

- new Nerosoft.Euonia.Disposing.AnonymousDisposable(System.Action dispose)
- void Add(System.Action dispose)
- static Nerosoft.Euonia.Disposing.AnonymousDisposable Create(System.Action dispose)

### Nerosoft.Euonia.Disposing.AsyncAnonymousDisposable

**类型**：class
基类 AsyncSingleDisposable<Func<System.Threading.Tasks.ValueTask>>；接口 System.IAsyncDisposable

- new Nerosoft.Euonia.Disposing.AsyncAnonymousDisposable(Func<System.Threading.Tasks.ValueTask> dispose, Nerosoft.Euonia.Disposing.DisposeFlags flags = 1)
- System.Threading.Tasks.ValueTask AddAsync(Func<System.Threading.Tasks.ValueTask> dispose)
- static Nerosoft.Euonia.Disposing.AsyncAnonymousDisposable Create(Func<System.Threading.Tasks.ValueTask> dispose)

### AsyncBoundActionField<T>

**类型**：class

- new AsyncBoundActionField<T>(Func<T, System.Threading.Tasks.ValueTask> action, T context)
- System.Boolean IsEmpty { get; }
- IBoundAction<T> TryGetAndUnset()
- System.Boolean TryUpdateContext(Func<T, T> contextUpdater)

### Nerosoft.Euonia.Disposing.AsyncCollectionDisposable

**类型**：class
基类 AsyncSingleDisposable<ImmutableQueue<System.IAsyncDisposable>>；接口 System.IAsyncDisposable

- new Nerosoft.Euonia.Disposing.AsyncCollectionDisposable(System.IAsyncDisposable[] disposables)
- new Nerosoft.Euonia.Disposing.AsyncCollectionDisposable(IEnumerable<System.IAsyncDisposable> disposables)
- new Nerosoft.Euonia.Disposing.AsyncCollectionDisposable(IEnumerable<System.IAsyncDisposable> disposables, Nerosoft.Euonia.Disposing.DisposeFlags flags)
- System.Threading.Tasks.ValueTask AddAsync(System.IAsyncDisposable disposable)
- static Nerosoft.Euonia.Disposing.AsyncCollectionDisposable Create(System.IAsyncDisposable[] disposables)
- static Nerosoft.Euonia.Disposing.AsyncCollectionDisposable Create(IEnumerable<System.IAsyncDisposable> disposables)

### Nerosoft.Euonia.Disposing.AsyncNoopDisposable

**类型**：class
接口 System.IAsyncDisposable

- new Nerosoft.Euonia.Disposing.AsyncNoopDisposable()
- System.Threading.Tasks.ValueTask DisposeAsync()

### AsyncSingleDisposable<T>

**类型**：class
接口 System.IAsyncDisposable

- System.Boolean IsDisposed { get; }
- System.Boolean IsDisposeStarted { get; }
- System.Boolean IsDisposing { get; }
- System.Threading.Tasks.ValueTask DisposeAsync()

### AsyncSingleNonblockingDisposable<T>

**类型**：class
接口 System.IAsyncDisposable

- System.Boolean IsDisposed { get; }
- System.Threading.Tasks.ValueTask DisposeAsync()

### BoundActionField<T>

**类型**：class

- new BoundActionField<T>(Action<T> action, T context)
- System.Boolean IsEmpty { get; }
- IBoundAction<T> TryGetAndUnset()
- System.Boolean TryUpdateContext(Func<T, T> contextUpdater)

### Nerosoft.Euonia.Disposing.CollectionDisposable

**类型**：class
基类 SingleDisposable<ImmutableQueue<System.IDisposable>>；接口 System.IDisposable

- new Nerosoft.Euonia.Disposing.CollectionDisposable(System.IDisposable[] disposables)
- new Nerosoft.Euonia.Disposing.CollectionDisposable(IEnumerable<System.IDisposable> disposables)
- void Add(System.IDisposable disposable)
- static Nerosoft.Euonia.Disposing.CollectionDisposable Create(System.IDisposable[] disposables)
- static Nerosoft.Euonia.Disposing.CollectionDisposable Create(IEnumerable<System.IDisposable> disposables)

### Nerosoft.Euonia.Disposing.DisposeFlags

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| ExecuteConcurrently |
| ExecuteSerially |


### IBoundAction<T>

**类型**：interface

- System.Threading.Tasks.ValueTask InvokeAsync()

### IBoundAction<T>

**类型**：interface

- void Invoke()

### Nerosoft.Euonia.Disposing.NoopDisposable

**类型**：class
接口 System.IDisposable

- static Nerosoft.Euonia.Disposing.NoopDisposable Instance { get; }
- void Dispose()

### SingleDisposable<T>

**类型**：class
接口 System.IDisposable

- System.Boolean IsDisposed { get; }
- System.Boolean IsDisposeStarted { get; }
- System.Boolean IsDisposing { get; }
- void Dispose()

### SingleNonblockingDisposable<T>

**类型**：class
接口 System.IDisposable

- System.Boolean IsDisposed { get; }
- void Dispose()

## 命名空间 Nerosoft.Euonia.Execution

### Nerosoft.Euonia.Execution.AmbientExecutionContext

**类型**：static class

- static Nerosoft.Euonia.Execution.ExecutionContextValues Current { get; }
- static System.IDisposable Push(Action<Nerosoft.Euonia.Execution.ExecutionContextValues> configure)
- static System.IDisposable PushNew(Action<Nerosoft.Euonia.Execution.ExecutionContextValues> configure)
- static void Reset()

### Nerosoft.Euonia.Execution.ExecutionContextAccessor

**类型**：class
接口 Nerosoft.Euonia.Execution.IExecutionContextAccessor

- new Nerosoft.Euonia.Execution.ExecutionContextAccessor()
- Nerosoft.Euonia.Execution.ExecutionContextValues Current { get; }

### Nerosoft.Euonia.Execution.ExecutionContextScopeFactory

**类型**：class
接口 Nerosoft.Euonia.Execution.IExecutionContextScopeFactory

- new Nerosoft.Euonia.Execution.ExecutionContextScopeFactory()
- Nerosoft.Euonia.Execution.IExecutionContextScope Begin(Action<Nerosoft.Euonia.Execution.ExecutionContextValues> configure)

### Nerosoft.Euonia.Execution.ExecutionContextValues

**类型**：class

- new Nerosoft.Euonia.Execution.ExecutionContextValues()
- System.String ActorTenantId { get; set; }
- System.String CorrelationId { get; set; }
- System.String CrossTenantReason { get; set; }
- System.Boolean IsCrossTenant { get; set; }
- IDictionary<System.String, System.Object> Items { get; set; }
- System.String RequestTraceId { get; set; }
- System.String TargetTenantId { get; set; }
- System.String TenantId { get; set; }
- System.String TenantName { get; set; }
- System.String UserId { get; set; }
- System.String UserName { get; set; }
- Nerosoft.Euonia.Execution.ExecutionContextValues Clone()

### Nerosoft.Euonia.Execution.IExecutionContextAccessor

**类型**：interface

- Nerosoft.Euonia.Execution.ExecutionContextValues Current { get; }

### Nerosoft.Euonia.Execution.IExecutionContextScope

**类型**：interface
接口 System.IDisposable、System.IAsyncDisposable

- Nerosoft.Euonia.Execution.ExecutionContextValues Context { get; }

### Nerosoft.Euonia.Execution.IExecutionContextScopeFactory

**类型**：interface

- Nerosoft.Euonia.Execution.IExecutionContextScope Begin(Action<Nerosoft.Euonia.Execution.ExecutionContextValues> configure)

## 命名空间 Nerosoft.Euonia.Messaging

### Nerosoft.Euonia.Messaging.MessageMetadataKeys

**类型**：static class

- static const System.String ActorTenantId = $nerosoft:tenant.actor
- static const System.String CrossTenantReason = $nerosoft:tenant.reason
- static const System.String IsCrossTenant = $nerosoft:tenant.cross
- static const System.String LegacyUserTenant = $nerosoft:user.tenant
- static const System.String LinkTraceParent = trace.link
- static const System.String TargetTenantId = $nerosoft:tenant.target
- static const System.String TenantId = $nerosoft:tenant.id
- static const System.String TenantName = $nerosoft:tenant.name
- static const System.String UserId = $nerosoft:user.id

### Nerosoft.Euonia.Messaging.TraceHeaders

**类型**：static class

- static const System.String Baggage = baggage
- static const System.String TraceParent = traceparent
- static const System.String TraceState = tracestate

## 命名空间 Nerosoft.Euonia.MultiTenancy

### Nerosoft.Euonia.MultiTenancy.IMultiTenant

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.MultiTenancy.IMultiTenantEntity

**类型**：interface
接口 Nerosoft.Euonia.MultiTenancy.IMultiTenant

- System.String TenantId { get; set; }

## 命名空间 Nerosoft.Euonia.Reflection

### Nerosoft.Euonia.Reflection.AssemblyHelper

**类型**：static class

- static IEnumerable<System.String> GetAssemblyFiles(System.String directory, System.IO.SearchOption searchOption)
- static IReadOnlyList<System.Reflection.TypeInfo> GetDefinedTypes(System.Reflection.Assembly assembly)
- static List<System.Reflection.Assembly> LoadAssemblies(System.String directory, System.IO.SearchOption searchOption)
- static IReadOnlyList<System.Type> LoadTypes(System.Reflection.Assembly assembly)
- static IReadOnlyList<System.Type> LoadTypes(IEnumerable<System.Reflection.Assembly> assemblies)

### Nerosoft.Euonia.Reflection.AssemblyLoadContextManager

**类型**：static class

- static Tuple<System.String, TValue> CreateCacheInstance<TValue>(System.Type objectType, TValue cachingItem, Action<System.Runtime.Loader.AssemblyLoadContext> unloadAction, System.Boolean excludeNonCollectible = false)
- static void RemoveFromCache<TKey, TValue>(IDictionary<TKey, Tuple<System.String, TValue>> dictionary, System.Runtime.Loader.AssemblyLoadContext context, System.Boolean usingConcurrentDictionary = false)

### Nerosoft.Euonia.Reflection.EnumHelper

**类型**：static class

- static T GetAttribute<T>(System.Enum e)
- static T GetCustomerAttribute<T>(System.Enum value)
- static System.String[] GetEnumNames<TEnum>()
- static TEnum[] GetEnumValues<TEnum>()

### EnumParser<T>

**类型**：static class

- static T Parse(System.String name)
- static System.Boolean TryParse(System.String name, out T value)

### PropertyAccessorCache<T>

**类型**：static class

- static System.Linq.Expressions.LambdaExpression Get(System.String propertyName)

### Nerosoft.Euonia.Reflection.Reflect

**类型**：static class

- static System.Reflection.MethodInfo FindMethod(System.String methodNameToRetrieve, System.Type typeToLookThroughTheMethods, System.Type[] methodParameterTypes)
- static System.Reflection.MethodInfo FindMethod(System.String methodNameToRetrieve, System.Type typeToLookThroughTheMethods, Func<System.Reflection.MethodInfo, System.Boolean> methodSelector)
- static System.Reflection.MethodInfo FindMethod(System.String methodNameToRetrieve, IEnumerable<System.Reflection.MethodInfo> methodsToLookThrough, Func<System.Reflection.MethodInfo, System.Boolean> methodSelector)
- static IEnumerable<TAttribute> GetAttributesOfMemberOrDeclaringType<TAttribute>(System.Reflection.MemberInfo memberInfo, System.Boolean inherit = true)
- static List<System.Type> GetImplementedGenericTypes(System.Type givenType, System.Type genericType)
- static System.Reflection.MemberInfo GetMemberInfo(System.Linq.Expressions.Expression expression)
- static System.Reflection.MethodInfo GetMethodInfo<T>(Expression<Func<T, System.Delegate>> expression)
- static System.Reflection.MethodInfo GetMethodInfo(System.Linq.Expressions.Expression expression)
- static System.Reflection.PropertyInfo GetProperty<T>(Expression<Func<T>> expression)
- static System.Reflection.PropertyInfo GetProperty<T>(Expression<Func<T, System.Object>> expression)
- static System.Reflection.PropertyInfo GetProperty<T, TResult>(Expression<Func<T, TResult>> expression)
- static IEnumerable<System.String> GetPublicConstantsRecursively(System.Type type)
- static TAttribute GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(System.Reflection.MemberInfo memberInfo, TAttribute defaultValue = null, System.Boolean inherit = true)
- static TAttribute GetSingleAttributeOrDefault<TAttribute>(System.Reflection.MemberInfo memberInfo, TAttribute defaultValue = null, System.Boolean inherit = true)
- static System.Object GetValue<T>(T item, Expression<Func<T, System.Object>> property)
- static System.Object GetValue(System.Object obj, System.Type objectType, System.String propertyPath)
- static System.Object InvokeGenericMethod(System.Object obj, System.String methodName, System.Type[] genericTypes, System.Object[] parameters)
- static System.Boolean IsAssignableToGenericType(System.Type givenType, System.Type genericType)
- static void SetValue<T, TValue>(T item, TValue value, Expression<Func<T, TValue>> property)
- static void SetValue(System.Object obj, System.Type objectType, System.String propertyPath, System.Object value)
- static System.Boolean TryGetPropertyValue<T>(System.Object obj, System.String propertyName, out T value)
- static System.Boolean TryGetPropertyValue(System.Object obj, System.String propertyName, out System.Object value)

### Reflect<TTarget>

**类型**：static class

- static System.Reflection.FieldInfo GetField(Expression<Func<TTarget, System.Object>> expression)
- static List<System.Type> GetImplementedGenericTypes(System.Type genericType)
- static System.Reflection.MethodInfo GetMethod(Expression<Action<TTarget>> expression)
- static System.Reflection.MethodInfo GetMethod<T1>(Expression<Action<TTarget, T1>> expression)
- static System.Reflection.MethodInfo GetMethod<T1, T2>(Expression<Action<TTarget, T1, T2>> expression)
- static System.Reflection.MethodInfo GetMethod<T1, T2, T3>(Expression<Action<TTarget, T1, T2, T3>> expression)
- static System.Reflection.PropertyInfo GetProperty(Expression<Func<TTarget, System.Object>> expression)
- static System.Reflection.PropertyInfo GetProperty<TValue>(Expression<Func<TTarget, TValue>> expression)
- static IEnumerable<System.String> GetPublicConstantsRecursively()
- static System.Object GetValue(TTarget item, Expression<Func<TTarget, System.Object>> property)
- static System.Object GetValue(TTarget obj, System.String propertyPath)
- static System.Boolean IsAssignableToGenericType(System.Type genericType)
- static void SetValue<TValue>(TTarget item, TValue value, Expression<Func<TTarget, TValue>> property)
- static void SetValue(TTarget obj, System.String propertyPath, System.Object value)

### Nerosoft.Euonia.Reflection.TypeHelper

**类型**：class

- new Nerosoft.Euonia.Reflection.TypeHelper()
- static System.Object CoerceValue(System.Type desiredType, System.Type valueType, System.Object value)
- static T CoerceValue<T>(System.Type valueType, System.Object value)
- static TOutput CoerceValue<TOutput, TInput>(TInput value)

## 命名空间 Nerosoft.Euonia.Security

### Nerosoft.Euonia.Security.AccountException

**类型**：class
基类 System.Security.Authentication.AuthenticationException；接口 System.Runtime.Serialization.ISerializable

- Dictionary<System.String, System.Object> Details { get; }
- System.String Identity { get; }

### Nerosoft.Euonia.Security.AccountExpiredException

**类型**：class
基类 Nerosoft.Euonia.Security.AccountException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.AccountExpiredException(System.String identity)
- new Nerosoft.Euonia.Security.AccountExpiredException(System.String identity, System.String message)
- new Nerosoft.Euonia.Security.AccountExpiredException(System.String identity, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.AccountLockedException

**类型**：class
基类 Nerosoft.Euonia.Security.AccountException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.AccountLockedException(System.String identity)
- new Nerosoft.Euonia.Security.AccountLockedException(System.String identity, System.String message)
- new Nerosoft.Euonia.Security.AccountLockedException(System.String identity, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.AccountNotFoundException

**类型**：class
基类 Nerosoft.Euonia.Security.AccountException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.AccountNotFoundException(System.String identity)
- new Nerosoft.Euonia.Security.AccountNotFoundException(System.String identity, System.String message)
- new Nerosoft.Euonia.Security.AccountNotFoundException(System.String identity, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.BusinessOperation

**类型**：static class

- static IReadOnlyList<System.String> All { get; }
- static const System.String Create = create
- static const System.String Delete = delete
- static const System.String Execute = execute
- static const System.String Read = read
- static const System.String Update = update

### Nerosoft.Euonia.Security.CredentialException

**类型**：class
基类 System.Security.Authentication.AuthenticationException；接口 System.Runtime.Serialization.ISerializable

- System.Object Credential { get; }
- Dictionary<System.String, System.Object> Details { get; }

### Nerosoft.Euonia.Security.CredentialExpiredException

**类型**：class
基类 Nerosoft.Euonia.Security.CredentialException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.CredentialExpiredException(System.Object credential)
- new Nerosoft.Euonia.Security.CredentialExpiredException(System.Object credential, System.String message)
- new Nerosoft.Euonia.Security.CredentialExpiredException(System.Object credential, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.CredentialIncorrectException

**类型**：class
基类 Nerosoft.Euonia.Security.CredentialException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.CredentialIncorrectException(System.Object credential)
- new Nerosoft.Euonia.Security.CredentialIncorrectException(System.Object credential, System.String message)
- new Nerosoft.Euonia.Security.CredentialIncorrectException(System.Object credential, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.CredentialNotFoundException

**类型**：class
基类 Nerosoft.Euonia.Security.CredentialException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.CredentialNotFoundException(System.Object credential)
- new Nerosoft.Euonia.Security.CredentialNotFoundException(System.Object credential, System.String message)
- new Nerosoft.Euonia.Security.CredentialNotFoundException(System.Object credential, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Security.IObjectOperationResolver

**类型**：interface

- System.Boolean TryResolve(System.Object resource, out System.String operation)

### Nerosoft.Euonia.Security.IObjectScopeAuthorizer

**类型**：interface

- System.Boolean Allows(System.Object target, System.String operation, System.IServiceProvider scope)
- System.Threading.Tasks.ValueTask EnsureResolvedAsync(System.IServiceProvider scope, System.Threading.CancellationToken cancellationToken = default)
- System.String Explain(System.Object target, System.String operation, System.IServiceProvider scope)
- System.Boolean IsConstrained(System.Type resourceType)

### Nerosoft.Euonia.Security.IPermissionChecker

**类型**：interface

- System.Threading.Tasks.ValueTask EnsureResolvedAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Boolean IsGranted(System.String permission)
- ValueTask<System.Boolean> IsGrantedAsync(System.String permission, System.Threading.CancellationToken cancellationToken = default)
- System.Boolean IsInRole(System.String role)

### Nerosoft.Euonia.Security.IPermissionCodeSource

**类型**：interface

- IReadOnlyList<System.String> AllOperations { get; }
- IReadOnlyCollection<System.String> CodesFor(System.Type type, System.String operation)
- IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> RequirementsFor(System.Type type, System.String operation)

### Nerosoft.Euonia.Security.OperationConventions

**类型**：static class

- static System.String[] Names(System.Type attributeType, System.String prefix = null)

### Nerosoft.Euonia.Security.PermissionAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Security.PermissionAttribute(System.String permission)
- new Nerosoft.Euonia.Security.PermissionAttribute(System.String permission, System.String[] roles)
- System.String Message { get; set; }
- System.String Permission { get; }
- System.String[] Roles { get; }

### Nerosoft.Euonia.Security.SensitiveDataAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Security.SensitiveDataAttribute()
- System.String Mask { get; set; }

### Nerosoft.Euonia.Security.SensitiveDataMasker

**类型**：static class

- static System.Object Mask(System.Object instance, System.String mask = "***", System.Int32 maxDepth = 8)

### Nerosoft.Euonia.Security.UserClaimTypes

**类型**：static class

- static const System.String AccessTokenHash = at_hash
- static const System.String Actor = act
- static const System.String Address = address
- static const System.String Audience = aud
- static const System.String AuthenticationContextClassReference = acr
- static const System.String AuthenticationMethod = amr
- static const System.String AuthenticationTime = auth_time
- static const System.String AuthorizationCodeHash = c_hash
- static const System.String AuthorizedParty = azp
- static const System.String BirthDate = birthdate
- static const System.String ClientId = client_id
- static const System.String Code = code
- static const System.String Confirmation = cnf
- static const System.String Email = email
- static const System.String EmailVerified = email_verified
- static const System.String Events = events
- static const System.String Expiration = exp
- static const System.String FamilyName = family_name
- static const System.String Gender = gender
- static const System.String GivenName = given_name
- static const System.String Id = id
- static const System.String IdentityProvider = idp
- static const System.String IssuedAt = iat
- static const System.String Issuer = iss
- static const System.String JwtId = jti
- static const System.String Locale = locale
- static const System.String MayAct = may_act
- static const System.String MiddleName = middle_name
- static const System.String Name = name
- static const System.String NickName = nickname
- static const System.String Nonce = nonce
- static const System.String NotBefore = nbf
- static const System.String PhoneNumber = phone_number
- static const System.String PhoneNumberVerified = phone_number_verified
- static const System.String Picture = picture
- static const System.String PreferredUserName = preferred_username
- static const System.String Profile = profile
- static const System.String ReferenceTokenId = reference_token_id
- static const System.String Role = role
- static const System.String Scheme = scheme
- static const System.String Scope = scope
- static const System.String SessionId = sid
- static const System.String StateHash = s_hash
- static const System.String Subject = sub
- static const System.String Tenant = tenant
- static const System.String UpdatedAt = updated_at
- static const System.String WebSite = website
- static const System.String ZoneInfo = zoneinfo

### Nerosoft.Euonia.Security.UserPrincipal

**类型**：class

- new Nerosoft.Euonia.Security.UserPrincipal(System.Security.Claims.ClaimsPrincipal claims)
- System.Security.Claims.ClaimsPrincipal Claims { get; }
- System.String Code { get; }
- System.Boolean IsAuthenticated { get; }
- IEnumerable<System.String> Roles { get; }
- System.String Tenant { get; }
- System.String UserId { get; }
- System.String Username { get; }
- System.Security.Claims.Claim FindClaim(System.String claimType)
- System.Security.Claims.Claim[] FindClaims(System.String claimType)
- System.Security.Claims.Claim[] GetAllClaims()
- System.Boolean IsInRole(System.String role)
- System.Boolean IsInRoles(IEnumerable<System.String> roles)
- System.Boolean IsInRoles(System.String role, System.String separator = ",")

## 命名空间 Nerosoft.Euonia.Threading

### Nerosoft.Euonia.Threading.AsyncAutoResetEvent

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncAutoResetEvent(System.Boolean set)
- new Nerosoft.Euonia.Threading.AsyncAutoResetEvent()
- System.Int32 Id { get; }
- System.Boolean IsSet { get; }
- void Set()
- void Wait(System.Threading.CancellationToken cancellationToken)
- void Wait()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()

### AsyncCollection<T>

**类型**：class

- new AsyncCollection<T>(IProducerConsumerCollection<T> collection, System.Int32 maxCount)
- new AsyncCollection<T>(IProducerConsumerCollection<T> collection)
- new AsyncCollection<T>(System.Int32 maxCount)
- new AsyncCollection<T>()
- void Add(T item, System.Threading.CancellationToken cancellationToken)
- void Add(T item)
- System.Threading.Tasks.Task AddAsync(T item, System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task AddAsync(T item)
- void CompleteAdding()
- IEnumerable<T> GetConsumingEnumerable(System.Threading.CancellationToken cancellationToken)
- IEnumerable<T> GetConsumingEnumerable()
- System.Boolean OutputAvailable(System.Threading.CancellationToken cancellationToken)
- System.Boolean OutputAvailable()
- Task<System.Boolean> OutputAvailableAsync(System.Threading.CancellationToken cancellationToken)
- Task<System.Boolean> OutputAvailableAsync()
- T Take(System.Threading.CancellationToken cancellationToken)
- T Take()
- Task<T> TakeAsync(System.Threading.CancellationToken cancellationToken)
- Task<T> TakeAsync()

### Nerosoft.Euonia.Threading.AsyncConditionVariable

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncConditionVariable(Nerosoft.Euonia.Threading.AsyncLock asyncLock)
- System.Int32 Id { get; }
- void Notify()
- void NotifyAll()
- void Wait(System.Threading.CancellationToken cancellationToken)
- void Wait()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()

### Nerosoft.Euonia.Threading.AsyncContext

**类型**：class
接口 System.IDisposable

- new Nerosoft.Euonia.Threading.AsyncContext()
- static Nerosoft.Euonia.Threading.AsyncContext Current { get; }
- System.Threading.Tasks.TaskFactory Factory { get; }
- System.Int32 Id { get; }
- System.Threading.Tasks.TaskScheduler Scheduler { get; }
- System.Threading.SynchronizationContext SynchronizationContext { get; }
- void Dispose()
- void Execute()
- static void Run(System.Action action)
- static TResult Run<TResult>(Func<TResult> action)
- static void Run(Func<System.Threading.Tasks.Task> action)
- static TResult Run<TResult>(Func<Task<TResult>> action)

### Nerosoft.Euonia.Threading.AsyncContextThread

**类型**：class
基类 SingleDisposable<Nerosoft.Euonia.Threading.AsyncContext>；接口 System.IDisposable

- new Nerosoft.Euonia.Threading.AsyncContextThread()
- Nerosoft.Euonia.Threading.AsyncContext Context { get; }
- System.Threading.Tasks.TaskFactory Factory { get; }
- void Join()
- System.Threading.Tasks.Task JoinAsync()

### Nerosoft.Euonia.Threading.AsyncCountdownEvent

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncCountdownEvent(System.Int64 count)
- System.Int64 CurrentCount { get; }
- System.Int32 Id { get; }
- void AddCount(System.Int64 addCount)
- void AddCount()
- void Signal(System.Int64 signalCount)
- void Signal()
- void Wait()
- void Wait(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)

### AsyncLazy<T>

**类型**：class

- new AsyncLazy<T>(Func<Task<T>> factory, Nerosoft.Euonia.Threading.AsyncLazyFlags flags = 0)
- System.Int32 Id { get; }
- System.Boolean IsStarted { get; }
- Task<T> Task { get; }
- ConfiguredTaskAwaitable<T> ConfigureAwait(System.Boolean continueOnCapturedContext)
- TaskAwaiter<T> GetAwaiter()
- void Start()

### Nerosoft.Euonia.Threading.AsyncLazyFlags

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| ExecuteOnCallingThread |
| RetryOnFailure |


### Nerosoft.Euonia.Threading.AsyncLock

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncLock()
- System.Int32 Id { get; }
- System.IDisposable Lock(System.Threading.CancellationToken cancellationToken)
- System.IDisposable Lock()
- AwaitableDisposable<System.IDisposable> LockAsync(System.Threading.CancellationToken cancellationToken)
- AwaitableDisposable<System.IDisposable> LockAsync()

### Nerosoft.Euonia.Threading.AsyncManualResetEvent

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncManualResetEvent(System.Boolean set)
- new Nerosoft.Euonia.Threading.AsyncManualResetEvent()
- System.Int32 Id { get; }
- System.Boolean IsSet { get; }
- void Reset()
- void Set()
- void Wait()
- void Wait(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Threading.AsyncMonitor

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncMonitor()
- System.Int32 Id { get; }
- System.IDisposable Enter(System.Threading.CancellationToken cancellationToken)
- System.IDisposable Enter()
- AwaitableDisposable<System.IDisposable> EnterAsync(System.Threading.CancellationToken cancellationToken)
- AwaitableDisposable<System.IDisposable> EnterAsync()
- void Pulse()
- void PulseAll()
- void Wait(System.Threading.CancellationToken cancellationToken)
- void Wait()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()

### AsyncProducerConsumerQueue<T>

**类型**：class

- new AsyncProducerConsumerQueue<T>(IEnumerable<T> collection, System.Int32 maxCount)
- new AsyncProducerConsumerQueue<T>(IEnumerable<T> collection)
- new AsyncProducerConsumerQueue<T>(System.Int32 maxCount)
- new AsyncProducerConsumerQueue<T>()
- void CompleteAdding()
- T Dequeue(System.Threading.CancellationToken cancellationToken)
- T Dequeue()
- Task<T> DequeueAsync(System.Threading.CancellationToken cancellationToken)
- Task<T> DequeueAsync()
- void Enqueue(T item, System.Threading.CancellationToken cancellationToken)
- void Enqueue(T item)
- System.Threading.Tasks.Task EnqueueAsync(T item, System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task EnqueueAsync(T item)
- IEnumerable<T> GetConsumingEnumerable(System.Threading.CancellationToken cancellationToken)
- IEnumerable<T> GetConsumingEnumerable()
- System.Boolean OutputAvailable(System.Threading.CancellationToken cancellationToken)
- System.Boolean OutputAvailable()
- Task<System.Boolean> OutputAvailableAsync(System.Threading.CancellationToken cancellationToken)
- Task<System.Boolean> OutputAvailableAsync()

### Nerosoft.Euonia.Threading.AsyncSemaphore

**类型**：class

- new Nerosoft.Euonia.Threading.AsyncSemaphore(System.Int64 initialCount)
- System.Int64 CurrentCount { get; }
- System.Int32 Id { get; }
- System.IDisposable Lock(System.Threading.CancellationToken cancellationToken)
- System.IDisposable Lock()
- AwaitableDisposable<System.IDisposable> LockAsync(System.Threading.CancellationToken cancellationToken)
- AwaitableDisposable<System.IDisposable> LockAsync()
- void Release(System.Int64 releaseCount)
- void Release()
- void Wait(System.Threading.CancellationToken cancellationToken)
- void Wait()
- System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task WaitAsync()

### AwaitableDisposable<T>

**类型**：struct

- new AwaitableDisposable<T>(Task<T> task)
- Task<T> AsTask()
- ConfiguredTaskAwaitable<T> ConfigureAwait(System.Boolean continueOnCapturedContext)
- TaskAwaiter<T> GetAwaiter()
- static Task<T> operator Implicit(AwaitableDisposable<T> source)

### CancellationTokenTaskSource<T>

**类型**：class
接口 System.IDisposable

- new CancellationTokenTaskSource<T>(System.Threading.CancellationToken cancellationToken)
- Task<T> Task { get; set; }
- void Dispose()

### Nerosoft.Euonia.Threading.StatefulMutex.Context

**类型**：class
接口 System.IDisposable

- void Dispose()

### Nerosoft.Euonia.Threading.DeferralManager

**类型**：class

- new Nerosoft.Euonia.Threading.DeferralManager()
- Nerosoft.Euonia.Threading.IDeferralSource DeferralSource { get; }
- System.Threading.Tasks.Task WaitForDeferralsAsync()

### Nerosoft.Euonia.Threading.IDeferralSource

**类型**：interface

- System.IDisposable GetDeferral()

### Nerosoft.Euonia.Threading.NotifyTaskCompletion

**类型**：class
接口 System.ComponentModel.INotifyPropertyChanged

- new Nerosoft.Euonia.Threading.NotifyTaskCompletion(System.Threading.Tasks.Task task)
- System.String ErrorMessage { get; }
- System.AggregateException Exception { get; }
- System.Exception InnerException { get; }
- System.Boolean IsCanceled { get; }
- System.Boolean IsCompleted { get; }
- System.Boolean IsFaulted { get; }
- System.Boolean IsNotCompleted { get; }
- System.Boolean IsSuccessfullyCompleted { get; }
- System.Threading.Tasks.TaskStatus Status { get; }
- System.Threading.Tasks.Task Task { get; }
- System.Threading.Tasks.Task TaskCompletion { get; }
- event System.ComponentModel.PropertyChangedEventHandler PropertyChanged

### NotifyTaskCompletion<TResult>

**类型**：class
接口 System.ComponentModel.INotifyPropertyChanged

- new NotifyTaskCompletion<TResult>(Task<TResult> task)
- System.String ErrorMessage { get; }
- System.AggregateException Exception { get; }
- System.Exception InnerException { get; }
- System.Boolean IsCanceled { get; }
- System.Boolean IsCompleted { get; }
- System.Boolean IsFaulted { get; }
- System.Boolean IsNotCompleted { get; }
- System.Boolean IsSuccessfullyCompleted { get; }
- TResult Result { get; }
- System.Threading.Tasks.TaskStatus Status { get; }
- Task<TResult> Task { get; }
- System.Threading.Tasks.Task TaskCompletion { get; }
- event System.ComponentModel.PropertyChangedEventHandler PropertyChanged

### Nerosoft.Euonia.Threading.PauseToken

**类型**：struct

- System.Boolean CanBePaused { get; }
- System.Boolean IsPaused { get; }
- void WaitWhilePaused()
- void WaitWhilePaused(System.Threading.CancellationToken token)
- System.Threading.Tasks.Task WaitWhilePausedAsync()
- System.Threading.Tasks.Task WaitWhilePausedAsync(System.Threading.CancellationToken token)

### Nerosoft.Euonia.Threading.PauseTokenSource

**类型**：class

- new Nerosoft.Euonia.Threading.PauseTokenSource()
- System.Boolean IsPaused { get; set; }
- Nerosoft.Euonia.Threading.PauseToken Token { get; }

### Nerosoft.Euonia.Threading.State

**类型**：class

- new Nerosoft.Euonia.Threading.State()
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- Nerosoft.Euonia.Threading.State GetNextState()

### Nerosoft.Euonia.Threading.StatefulMutex

**类型**：class
接口 System.IDisposable

- new Nerosoft.Euonia.Threading.StatefulMutex()
- Nerosoft.Euonia.Threading.State State { get; }
- Nerosoft.Euonia.Threading.StatefulMutex.Context Acquire()
- Nerosoft.Euonia.Threading.StatefulMutex.Context Acquire(Nerosoft.Euonia.Threading.State state)
- Task<Nerosoft.Euonia.Threading.StatefulMutex.Context> AcquireAsync()
- Task<Nerosoft.Euonia.Threading.StatefulMutex.Context> AcquireAsync(Nerosoft.Euonia.Threading.State state)
- void Dispose()
- Nerosoft.Euonia.Threading.State InvalidateState()
- System.Boolean IsCurrent(Nerosoft.Euonia.Threading.State state)

### Nerosoft.Euonia.Threading.SynchronizationContextSwitcher

**类型**：class
基类 SingleDisposable<System.Object>；接口 System.IDisposable

- static void ApplyContext(System.Threading.SynchronizationContext context, System.Action action)
- static T ApplyContext<T>(System.Threading.SynchronizationContext context, Func<T> action)
- static void NoContext(System.Action action)
- static T NoContext<T>(Func<T> action)

### Nerosoft.Euonia.Threading.TaskConstants

**类型**：static class

- static Task<System.Boolean> BooleanFalse { get; }
- static Task<System.Boolean> BooleanTrue { get; }
- static System.Threading.Tasks.Task Canceled { get; }
- static System.Threading.Tasks.Task Completed { get; }
- static Task<System.Int32> Int32NegativeOne { get; }
- static Task<System.Int32> Int32Zero { get; }

### TaskConstants<T>

**类型**：static class

- static Task<T> Canceled { get; }
- static Task<T> Default { get; }

### Nerosoft.Euonia.Threading.TaskHelper

**类型**：static class

- static System.Boolean IsSynchronous { get; set; }
- static ValueTask<TResult> AwaitSyncOverAsync<TResult>(Task<TResult> task)
- static System.Threading.Tasks.ValueTask AwaitSyncOverAsync(System.Threading.Tasks.Task task)
- static System.Threading.Tasks.ValueTask Delay(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)
- static void DisposeSyncViaAsync<TDisposable>(TDisposable disposable)
- static System.Threading.Tasks.Task ExecuteAsTask(System.Action func)
- static Task<T> ExecuteAsTask<T>(Func<T> func)
- static void Run<TState>(Func<TState, System.Threading.Tasks.ValueTask> action, TState state)
- static TResult Run<TState, TResult>(Func<TState, ValueTask<TResult>> action, TState state)

### Nerosoft.Euonia.Threading.TimeoutValue

**类型**：struct
接口 IEquatable<Nerosoft.Euonia.Threading.TimeoutValue>、IComparable<Nerosoft.Euonia.Threading.TimeoutValue>

- new Nerosoft.Euonia.Threading.TimeoutValue(Nullable<System.TimeSpan> timeout)
- System.Int32 InMilliseconds { get; }
- System.Int32 InSeconds { get; }
- System.Boolean IsInfinite { get; }
- System.Boolean IsZero { get; }
- System.TimeSpan TimeSpan { get; }
- System.Int32 CompareTo(Nerosoft.Euonia.Threading.TimeoutValue that)
- System.Boolean Equals(Nerosoft.Euonia.Threading.TimeoutValue that)
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(Nerosoft.Euonia.Threading.TimeoutValue a, Nerosoft.Euonia.Threading.TimeoutValue b)
- static Nerosoft.Euonia.Threading.TimeoutValue operator Implicit(Nullable<System.TimeSpan> timeout)
- static System.Boolean operator Inequality(Nerosoft.Euonia.Threading.TimeoutValue a, Nerosoft.Euonia.Threading.TimeoutValue b)
- System.String ToString()

## 命名空间 Nerosoft.Euonia.Threading.Interop

### EventArguments<TSender, TEventArgs>

**类型**：struct

- TEventArgs EventArgs { get; set; }
- TSender Sender { get; set; }

### Nerosoft.Euonia.Threading.Interop.EventAsyncFactory

**类型**：static class

- static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<EventArguments<TSender, TEventArgs>> FromActionEvent<TSender, TEventArgs>(Action<Action<TSender, TEventArgs>> subscribe, Action<Action<TSender, TEventArgs>> unsubscribe)
- static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe, Action<Action<TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe, Action<Action<TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<TEventArgs> FromActionEvent<TEventArgs>(Action<Action<TEventArgs>> subscribe, Action<Action<TEventArgs>> unsubscribe)
- static System.Threading.Tasks.Task FromActionEvent(Action<System.Action> subscribe, Action<System.Action> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static System.Threading.Tasks.Task FromActionEvent(Action<System.Action> subscribe, Action<System.Action> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static System.Threading.Tasks.Task FromActionEvent(Action<System.Action> subscribe, Action<System.Action> unsubscribe)
- static Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(Func<Action<TEventArguments>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(Func<Action<TEventArguments>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<TEventArguments> FromAnyEvent<TDelegate, TEventArguments>(Func<Action<TEventArguments>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe)
- static Task<EventArguments<System.Object, System.EventArgs>> FromEvent(Action<System.EventHandler> subscribe, Action<System.EventHandler> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<EventArguments<System.Object, System.EventArgs>> FromEvent(Action<System.EventHandler> subscribe, Action<System.EventHandler> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<EventArguments<System.Object, System.EventArgs>> FromEvent(Action<System.EventHandler> subscribe, Action<System.EventHandler> unsubscribe)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TEventArgs>(Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TEventArgs>(Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TEventArgs>(Action<EventHandler<TEventArgs>> subscribe, Action<EventHandler<TEventArgs>> unsubscribe)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, System.Threading.CancellationToken cancellationToken, System.Boolean unsubscribeOnCapturedContext)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe, System.Threading.CancellationToken cancellationToken)
- static Task<EventArguments<System.Object, TEventArgs>> FromEvent<TDelegate, TEventArgs>(Func<EventHandler<TEventArgs>, TDelegate> convert, Action<TDelegate> subscribe, Action<TDelegate> unsubscribe)

### Nerosoft.Euonia.Threading.Interop.WaitHandleAsyncFactory

**类型**：static class

- static System.Threading.Tasks.Task FromWaitHandle(System.Threading.WaitHandle handle)
- static Task<System.Boolean> FromWaitHandle(System.Threading.WaitHandle handle, System.TimeSpan timeout)
- static System.Threading.Tasks.Task FromWaitHandle(System.Threading.WaitHandle handle, System.Threading.CancellationToken token)
- static Task<System.Boolean> FromWaitHandle(System.Threading.WaitHandle handle, System.TimeSpan timeout, System.Threading.CancellationToken token)

## 命名空间 Nerosoft.Euonia.Threading.Redis

### Nerosoft.Euonia.Threading.Redis.TimeoutTask

**类型**：struct
接口 System.IDisposable

- new Nerosoft.Euonia.Threading.Redis.TimeoutTask(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task Task { get; }
- void Dispose()

## 命名空间 Nerosoft.Euonia.Windows

### Nerosoft.Euonia.Windows.DelegateCommand

**类型**：class
接口 System.Windows.Input.ICommand

- new Nerosoft.Euonia.Windows.DelegateCommand(Action<System.Object> executeAction, Func<System.Object, System.Boolean> canExecute = null)
- event System.EventHandler CanExecuteChanged
- System.Boolean CanExecute(System.Object parameter)
- void Execute(System.Object parameter)
- void RaiseCanExecuteChanged()

### DelegateCommand<T>

**类型**：class
接口 System.Windows.Input.ICommand

- new DelegateCommand<T>(Action<T> executeAction, Func<T, System.Boolean> canExecute = null)
- event System.EventHandler CanExecuteChanged
- System.Boolean CanExecute(System.Object parameter)
- void Execute(System.Object parameter)
- void RaiseCanExecuteChanged()

### Nerosoft.Euonia.Windows.GenericCommand

**类型**：class
接口 System.Windows.Input.ICommand

- new Nerosoft.Euonia.Windows.GenericCommand()
- Func<System.Object, System.Boolean> CanExecuteCallback { get; set; }
- Action<System.Object> ExecuteCallback { get; set; }
- event System.EventHandler CanExecuteChanged
- System.Boolean CanExecute(System.Object parameter)
- void Execute(System.Object parameter)
- void RaiseCanExecuteChanged()

### GenericCommand<T>

**类型**：class
接口 System.Windows.Input.ICommand

- new GenericCommand<T>()
- Func<T, System.Boolean> CanExecuteCallback { get; set; }
- Action<T> ExecuteCallback { get; set; }
- event System.EventHandler CanExecuteChanged
- System.Boolean CanExecute(System.Object parameter)
- void Execute(System.Object parameter)
- void RaiseCanExecuteChanged()

## 命名空间 System

### AsyncEventHandler<TEventArgs>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new AsyncEventHandler<TEventArgs>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.Object sender, TEventArgs args, System.AsyncCallback callback, System.Object object)
- System.Threading.Tasks.Task EndInvoke(System.IAsyncResult result)
- System.Threading.Tasks.Task Invoke(System.Object sender, TEventArgs args)

### System.BadGatewayException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.BadGatewayException()
- new System.BadGatewayException(System.String message)
- new System.BadGatewayException(System.String message, System.Exception innerException)
- new System.BadGatewayException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.BadRequestException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.BadRequestException()
- new System.BadRequestException(System.String message)
- new System.BadRequestException(System.String message, System.Exception innerException)
- new System.BadRequestException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.BusinessException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.BusinessException()
- new System.BusinessException(System.String code)
- new System.BusinessException(System.String code, System.String message)
- new System.BusinessException(System.String code, System.String message, System.Exception innerException)
- new System.BusinessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
- System.String Code { get; }
- void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.Clock

**类型**：static class

- static const System.Int64 TicksPerMillisecond = 10000
- static const System.Int64 UnixEpochSeconds = 62135596800
- static const System.Int64 UnixEpochTicks = 621355968000000000
- static System.Int64 GetUnixTimestampMillis()
- static System.Int64 GetUnixTimestampTicks()
- static System.Int64 ToUnixTimestampMillis(System.DateTime date)

### System.ConfigurationException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.ConfigurationException()
- new System.ConfigurationException(System.String message)
- new System.ConfigurationException(System.String message, System.Exception innerException)
- new System.ConfigurationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.ConflictException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.ConflictException()
- new System.ConflictException(System.String message)
- new System.ConflictException(System.String message, System.Exception innerException)
- new System.ConflictException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.DisposableObject

**类型**：class
接口 System.IDisposable

- event EventHandler<System.DisposedEventArgs> Disposed
- void Dispose()

### System.DisposedEventArgs

**类型**：class
基类 System.EventArgs

- new System.DisposedEventArgs()
- new System.DisposedEventArgs(System.Int32 hashCode)
- System.Int32 HashCode { get; }

### System.Empty

**类型**：class
接口 System.Runtime.Serialization.ISerializable

- static readonly System.Empty Value
- void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
- System.String ToString()

### EnumObject<TValue>

**类型**：class

- new EnumObject<TValue>()
- System.String Name { get; set; }
- TValue Value { get; set; }

### System.ExceptionAssert

**类型**：static class

- static void ThrowIf<TException>(System.Boolean condition, System.String message)
- static void ThrowIf<TException>(System.Boolean condition, Func<TException> exceptionFactory)
- static void ThrowIf<TException>(System.Boolean condition)

### System.ExceptionPrompt

**类型**：static class

- static void AddPrompt(System.IExceptionPrompt prompt)
- static System.String GetPrompt(System.Exception exception)

### System.ForbiddenException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.ForbiddenException()
- new System.ForbiddenException(System.String message)
- new System.ForbiddenException(System.String message, System.Exception innerException)
- new System.ForbiddenException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.GatewayTimeoutException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.GatewayTimeoutException()
- new System.GatewayTimeoutException(System.String message)
- new System.GatewayTimeoutException(System.String message, System.Exception innerException)
- new System.GatewayTimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.Gen2GcCallback

**类型**：class
基类 System.Runtime.ConstrainedExecution.CriticalFinalizerObject

- static void Register(Action<System.Object> callback, System.Object target)

### System.GuidType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Empty |
| Simple |
| SequentialAsString |
| SequentialAsBinary |
| SequentialAtEnd |


### System.HttpStatusCodeAttribute

**类型**：class
基类 System.Attribute

- new System.HttpStatusCodeAttribute(System.Net.HttpStatusCode statusCode)
- new System.HttpStatusCodeAttribute(System.Int32 statusCode)
- System.Net.HttpStatusCode StatusCode { get; }

### System.HttpStatusException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.HttpStatusException(System.Net.HttpStatusCode statusCode)
- new System.HttpStatusException(System.Net.HttpStatusCode statusCode, System.String message)
- new System.HttpStatusException(System.Net.HttpStatusCode statusCode, System.String message, System.Exception innerException)
- new System.HttpStatusException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
- System.Net.HttpStatusCode StatusCode { get; }
- void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.IExceptionPrompt

**类型**：interface

- System.String GetPrompt(System.Exception exception)

### System.InternalServerErrorException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.InternalServerErrorException()
- new System.InternalServerErrorException(System.String message)
- new System.InternalServerErrorException(System.String message, System.Exception innerException)
- new System.InternalServerErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.InvalidValueException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.InvalidValueException()
- new System.InvalidValueException(System.String message)
- new System.InvalidValueException(System.String message, System.Exception innerException)
- new System.InvalidValueException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.Invariant

**类型**：static class

- static void Require(System.Boolean condition, System.String message = null)

### IObjectPoolPolicy<T>

**类型**：interface

- T CreateNew()
- System.Boolean Return(T value)

### System.LikeOperator

**类型**：class

- new System.LikeOperator()
- static System.Boolean LikeString(System.String content, System.String pattern, System.Boolean ignoreCase = true, System.Boolean useInvariantCulture = true)
- static System.Boolean LikeString(ReadOnlySpan<System.Char> contentSpan, ReadOnlySpan<System.Char> patternSpan, System.Boolean ignoreCase = true, System.Boolean useInvariantCulture = true)

### System.ManagedFinalizerQueue

**类型**：class

- static readonly System.ManagedFinalizerQueue Instance
- System.IDisposable Register(System.Object resource, System.IAsyncDisposable finalizer)

### System.MethodInvokerBuilder

**类型**：static class

- static Func<System.Object, System.Object[], Task<System.Object>> Build(System.Reflection.MethodInfo method)
- static Func<Task<System.Object>> Build(System.Object target, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression[] arguments)
- static System.Linq.Expressions.Expression BuildCallExpression(System.Object target, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression[] arguments)
- static System.Linq.Expressions.Expression BuildCallExpression(System.Linq.Expressions.Expression target, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression[] arguments)
- static System.Linq.Expressions.Expression WrapToTaskObject(System.Linq.Expressions.Expression callExp, System.Type returnType)

### System.MethodNotAllowedException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.MethodNotAllowedException()
- new System.MethodNotAllowedException(System.String message)
- new System.MethodNotAllowedException(System.String message, System.Exception innerException)
- new System.MethodNotAllowedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.NotFoundException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.NotFoundException()
- new System.NotFoundException(System.String message)
- new System.NotFoundException(System.String message, System.Exception innerException)
- new System.NotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### ObjectPool<T>

**类型**：class

- new ObjectPool<T>(IObjectPoolPolicy<T> policy, Nullable<System.Int32> maxItems = default)
- T Lease()
- void Return(T value)

### Optional<T>

**类型**：class

- static Optional<T> Empty { get; }
- System.Boolean HasValue { get; }
- System.Boolean IsEmpty { get; }
- T Value { get; }
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- T GetOrThrow()
- T GetOrThrow<TException>(Func<TException> exceptionSupplier)
- T GetValueOrDefault()
- T GetValueOrDefault(T defaultValue)
- void IfPresent(Action<T> action)
- void IfPresent(Action<T> action, System.Action emptyAction)
- static Optional<T> Of(T value)
- static Optional<T> OfNullable(T value)
- static System.Boolean operator Equality(Optional<T> left, Optional<T> right)
- static System.Boolean operator Inequality(Optional<T> left, Optional<T> right)
- T Or(T other)
- T Or(Func<T> supplier)
- Optional<TResult> Select<TResult>(Func<T, TResult> selector)
- Optional<TResult> SelectMany<TResult>(Func<T, Optional<TResult>> selector)
- System.String ToString()
- Optional<T> Where(Func<T, System.Boolean> predicate)

### System.PriorityValueFinder

**类型**：static class

- static TValue Find<TValue>(PriorityQueue<Func<TValue>, System.Int32> queue, Func<TValue, System.Boolean> assert, TValue defaultValue = null)
- static TValue Find<TValue>(Action<PriorityQueue<Func<TValue>, System.Int32>> factory, Func<TValue, System.Boolean> assert, TValue defaultValue = null)
- static Task<TValue> FindAsync<TValue>(PriorityQueue<Func<Task<TValue>>, System.Int32> queue, Func<TValue, System.Boolean> assert, TValue defaultValue = null)
- static Task<TValue> FindAsync<TValue>(Action<PriorityQueue<Func<Task<TValue>>, System.Int32>> factory, Func<TValue, System.Boolean> assert, TValue defaultValue = null)

### System.RefBox

**类型**：class

- new System.RefBox()
- static RefBox<T> Create<T>(T value)
- static System.Boolean TryConsume<T>(ref RefBox<T> boxRef, out T value)

### RefBox<T>

**类型**：class

- ref T Value { get; }

### System.RequestContext

**类型**：class

- new System.RequestContext()
- System.String Authorization { get; }
- System.String ConnectionId { get; set; }
- System.String Cookie { get; }
- System.String CorrelationId { get; set; }
- System.String From { get; }
- IDictionary<System.String, System.String> Headers { get; set; }
- System.String Host { get; set; }
- System.Boolean IsWebSocketRequest { get; set; }
- System.String Location { get; }
- System.String Method { get; set; }
- System.String Origin { get; }
- System.String Path { get; set; }
- System.String Protocol { get; set; }
- System.String Referer { get; }
- System.Net.IPAddress RemoteIpAddress { get; set; }
- System.Int32 RemotePort { get; set; }
- System.Threading.CancellationToken RequestAborted { get; set; }
- System.String RequestId { get; }
- System.IServiceProvider RequestServices { get; set; }
- System.String Scheme { get; set; }
- System.String Server { get; }
- System.String TenantId { get; set; }
- System.String TraceIdentifier { get; set; }
- System.Security.Claims.ClaimsPrincipal User { get; set; }
- System.String UserAgent { get; }

### System.RequestTimeoutException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.RequestTimeoutException()
- new System.RequestTimeoutException(System.String message)
- new System.RequestTimeoutException(System.String message, System.Exception innerException)
- new System.RequestTimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.ServiceUnavailableException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.ServiceUnavailableException()
- new System.ServiceUnavailableException(System.String message)
- new System.ServiceUnavailableException(System.String message, System.Exception innerException)
- new System.ServiceUnavailableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.ShortUniqueId

**类型**：class

- new System.ShortUniqueId()
- new System.ShortUniqueId(System.String salt = "", System.Int32 minHashLength = 0, System.String alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890", System.String seps = "cfhistuCFHISTU")
- static System.ShortUniqueId Default { get; }
- System.Int32[] Decode(System.String hash)
- System.String DecodeHex(System.String hash)
- System.Int64[] DecodeInt64(System.String hash)
- System.Int32 DecodeSingle(System.String hash)
- System.Int64 DecodeSingleInt64(System.String hash)
- System.String Encode(System.Int32 number)
- System.String Encode(System.Int32[] numbers)
- System.String Encode(IEnumerable<System.Int32> numbers)
- System.String EncodeHex(System.String hex)
- System.String EncodeInt64(System.Int64 number)
- System.String EncodeInt64(System.Int64[] numbers)
- System.String EncodeInt64(IEnumerable<System.Int64> numbers)
- System.Boolean TryDecodeSingle(System.String hash, out System.Int32 id)
- System.Boolean TryDecodeSingleInt64(System.String hash, out System.Int64 id)

### Singleton<T>

**类型**：class

- new Singleton<T>()
- static T Instance { get; set; }
- static T Get(Func<T> factory)

### System.TextCaseType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| Upper |
| Lower |
| Title |


### System.TextTrimType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| Head |
| Tail |
| Both |
| All |


### System.TooManyRequestsException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.TooManyRequestsException()
- new System.TooManyRequestsException(System.String message)
- new System.TooManyRequestsException(System.String message, System.Exception innerException)
- new System.TooManyRequestsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### System.Unit

**类型**：struct
接口 IEquatable<System.Unit>、IComparable<System.Unit>、System.IComparable

- static Task<System.Unit> Task { get; }
- static ref System.Unit Value { get; }
- System.Int32 CompareTo(System.Unit other)
- System.Boolean Equals(System.Unit other)
- System.Boolean Equals(System.Object obj)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(System.Unit first, System.Unit second)
- static System.Boolean operator Inequality(System.Unit first, System.Unit second)
- System.String ToString()

### System.UpgradeRequiredException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new System.UpgradeRequiredException()
- new System.UpgradeRequiredException(System.String message)
- new System.UpgradeRequiredException(System.String message, System.Exception innerException)
- new System.UpgradeRequiredException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### Weak<T>

**类型**：class

- new Weak<T>(T target)
- new Weak<T>(T target, System.Boolean trackResurrection)
- T Target { get; set; }

### System.WeakEventManager

**类型**：class

- new System.WeakEventManager()
- void AddEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, System.String eventName = null)
- void AddEventHandler(System.Delegate handler, System.String eventName = "")
- void AddEventHandler(System.EventHandler handler, System.String eventName = null)
- void HandleEvent(System.Object sender, System.Object args, System.String eventName)
- void HandleEvent<TEventArgs>(System.Object sender, TEventArgs args, System.String eventName)
- void HandleEventParallel<TEventArgs>(System.Object sender, TEventArgs args, System.String eventName)
- void HandleEventSafely<TEventArgs>(System.Object sender, TEventArgs args, System.String eventName)
- void RemoveEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, System.String eventName = null)
- void RemoveEventHandler(System.Delegate handler, System.String eventName = "")
- void RemoveEventHandler(System.EventHandler handler, System.String eventName = null)
- void RemoveEventHandlers()
- void RemoveEventHandlers(System.String eventName)

## 命名空间 System.ComponentModel.DataAnnotations

### System.ComponentModel.DataAnnotations.CollectionCountAttribute

**类型**：class
基类 System.ComponentModel.DataAnnotations.ValidationAttribute

- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount)
- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount, System.Int32 maximumCount)
- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount, Func<System.String> errorMessageAccessor)
- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount, System.Int32 maximumCount, Func<System.String> errorMessageAccessor)
- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount, System.String errorMessage)
- new System.ComponentModel.DataAnnotations.CollectionCountAttribute(System.Int32 minimumCount, System.Int32 maximumCount, System.String errorMessage)
- System.Boolean AllowNull { get; set; }
- Nullable<System.Int32> MaximumCount { get; }
- System.Int32 MinimumCount { get; }

### System.ComponentModel.DataAnnotations.GuidAttribute

**类型**：class
基类 System.ComponentModel.DataAnnotations.ValidationAttribute

- new System.ComponentModel.DataAnnotations.GuidAttribute()
