using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证 ObservableDictionary 的更改通知逻辑。
/// </summary>
public class ObservableDictionaryTests
{
	private static (ObservableDictionary<string, int> dict, List<DictionaryChangedEventArgs<string, int>> events) Create()
	{
		var dict = new ObservableDictionary<string, int>();
		var events = new List<DictionaryChangedEventArgs<string, int>>();
		dict.ItemChanged += (_, e) => events.Add(e);
		return (dict, events);
	}

	/// <summary>
	/// 验证索引器写入新键时发出 <see cref="DictionaryChangedAction.Add"/> 事件，旧值为默认值。
	/// </summary>
	[Fact]
	public void Indexer_NewKey_ShouldRaiseAddEvent()
	{
		var (dict, events) = Create();

		dict["key"] = 1;

		var e = Assert.Single(events);
		Assert.Equal("key", e.Key);
		Assert.Equal(DictionaryChangedAction.Add, e.Action);
		Assert.Equal(0, e.OldValue);
		Assert.Equal(1, e.NewValue);
	}

	/// <summary>
	/// 验证索引器覆盖已有键时发出 <see cref="DictionaryChangedAction.Update"/> 事件，并携带新旧值。
	/// </summary>
	[Fact]
	public void Indexer_ExistingKey_ShouldRaiseUpdateEvent()
	{
		var (dict, events) = Create();
		dict["key"] = 1;
		events.Clear();

		dict["key"] = 2;

		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Update, e.Action);
		Assert.Equal(1, e.OldValue);
		Assert.Equal(2, e.NewValue);
	}

	/// <summary>
	/// 验证索引器写入与当前值相同的值时不发出任何变更事件。
	/// </summary>
	[Fact]
	public void Indexer_SameValue_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();
		dict["key"] = 1;
		events.Clear();

		dict["key"] = 1;

		Assert.Empty(events);
	}

	/// <summary>
	/// 验证 <see cref="ObservableDictionary{TKey, TValue}.Add(TKey, TValue)"/> 添加新键时发出新增事件。
	/// </summary>
	[Fact]
	public void Add_ShouldRaiseAddEvent()
	{
		var (dict, events) = Create();

		dict.Add("key", 1);

		var e = Assert.Single(events);
		Assert.Equal("key", e.Key);
		Assert.Equal(DictionaryChangedAction.Add, e.Action);
		Assert.Equal(0, e.OldValue);
		Assert.Equal(1, e.NewValue);
	}

	/// <summary>
	/// 验证添加重复键时抛出 <see cref="ArgumentException"/>，且不发出任何变更事件。
	/// </summary>
	[Fact]
	public void Add_DuplicateKey_ShouldThrowAndNotRaiseEvent()
	{
		var (dict, events) = Create();
		dict.Add("key", 1);
		events.Clear();

		Assert.Throws<ArgumentException>(() => dict.Add("key", 2));
		Assert.Empty(events);
	}

	/// <summary>
	/// 验证 <see cref="ObservableDictionary{TKey, TValue}.TryAdd(TKey, TValue)"/> 添加新键时返回 <see langword="true"/> 并发出新增事件。
	/// </summary>
	[Fact]
	public void TryAdd_NewKey_ShouldRaiseAddEventAndReturnTrue()
	{
		var (dict, events) = Create();

		var result = dict.TryAdd("key", 1);

		Assert.True(result);
		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Add, e.Action);
		Assert.Equal(1, e.NewValue);
	}

	/// <summary>
	/// 验证 <c>TryAdd</c> 遇到已有键时返回 <see langword="false"/>，保留原值且不发出事件。
	/// </summary>
	[Fact]
	public void TryAdd_ExistingKey_ShouldReturnFalseAndNotRaiseEvent()
	{
		var (dict, events) = Create();
		dict.Add("key", 1);
		events.Clear();

		var result = dict.TryAdd("key", 2);

		Assert.False(result);
		Assert.Empty(events);
		Assert.Equal(1, dict["key"]);
	}

	/// <summary>
	/// 验证移除已有键时返回 <see langword="true"/>，并发出携带旧值的移除事件。
	/// </summary>
	[Fact]
	public void Remove_ExistingKey_ShouldRaiseRemoveEventWithOldValue()
	{
		var (dict, events) = Create();
		dict.Add("key", 1);
		events.Clear();

		var result = dict.Remove("key");

		Assert.True(result);
		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Remove, e.Action);
		Assert.Equal(1, e.OldValue);
		Assert.Equal(0, e.NewValue);
	}

	/// <summary>
	/// 验证移除不存在的键时返回 <see langword="false"/>，且不发出变更事件。
	/// </summary>
	[Fact]
	public void Remove_MissingKey_ShouldReturnFalseAndNotRaiseEvent()
	{
		var (dict, events) = Create();

		var result = dict.Remove("key");

		Assert.False(result);
		Assert.Empty(events);
	}

	/// <summary>
	/// 验证带输出参数的移除在键存在时返回 <see langword="true"/>、输出旧值并发出移除事件。
	/// </summary>
	[Fact]
	public void RemoveWithOut_ExistingKey_ShouldRaiseRemoveEventAndReturnValue()
	{
		var (dict, events) = Create();
		dict.Add("key", 1);
		events.Clear();

		var result = dict.Remove("key", out var value);

		Assert.True(result);
		Assert.Equal(1, value);
		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Remove, e.Action);
		Assert.Equal(1, e.OldValue);
	}

	/// <summary>
	/// 验证带输出参数的移除在键不存在时返回 <see langword="false"/>、输出默认值且不发事件。
	/// </summary>
	[Fact]
	public void RemoveWithOut_MissingKey_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();

		var result = dict.Remove("key", out var value);

		Assert.False(result);
		Assert.Equal(0, value);
		Assert.Empty(events);
	}

	/// <summary>
	/// 验证字典包含元素时清空会发出一次清空事件，并清空全部条目。
	/// </summary>
	[Fact]
	public void Clear_WithItems_ShouldRaiseClearEventAndEmptyDictionary()
	{
		var (dict, events) = Create();
		dict.Add("a", 1);
		dict.Add("b", 2);
		events.Clear();

		dict.Clear();

		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Clear, e.Action);
		Assert.Empty(dict);
	}

	/// <summary>
	/// 验证对空字典调用清空不会发出任何变更事件。
	/// </summary>
	[Fact]
	public void Clear_Empty_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();

		dict.Clear();

		Assert.Empty(events);
	}

	/// <summary>
	/// 验证 <c>RaiseItemChangedEvents</c> 为 <see langword="false"/> 时所有写操作都不发出事件，但数据变更照常生效。
	/// </summary>
	[Fact]
	public void RaiseItemChangedEvents_False_ShouldNotRaiseEvents()
	{
		var (dict, events) = Create();
		dict.RaiseItemChangedEvents = false;

		dict["a"] = 1;
		dict.Add("b", 2);
		dict.Remove("a");
		dict.Clear();

		Assert.Empty(events);
		Assert.Empty(dict);
	}

	/// <summary>
	/// 验证抑制作用域内不发出事件，作用域释放后通知恢复。
	/// </summary>
	[Fact]
	public void SuppressItemChangedEvents_ShouldSuppressAndRestore()
	{
		var (dict, events) = Create();

		using (dict.SuppressItemChangedEvents)
		{
			dict["a"] = 1;
			Assert.Empty(events);
		}

		dict["b"] = 2;

		var e = Assert.Single(events);
		Assert.Equal("b", e.Key);
		Assert.Equal(DictionaryChangedAction.Add, e.Action);
	}

	/// <summary>
	/// 验证嵌套抑制作用域逐层释放后恢复到最初的抑制状态，而不是统一恢复为允许通知。
	/// </summary>
	[Fact]
	public void SuppressItemChangedEvents_Nested_ShouldRestoreInitialState()
	{
		var (dict, events) = Create();
		dict.RaiseItemChangedEvents = false;

		using (dict.SuppressItemChangedEvents)
		{
			using (dict.SuppressItemChangedEvents)
			{
				dict["a"] = 1;
			}

			// 内层释放后仍处于外层抑制状态
			dict["b"] = 2;
			Assert.Empty(events);
		}

		// 外层释放后应恢复为最初的 false，而不是错误地恢复为 true
		Assert.False(dict.RaiseItemChangedEvents);
		Assert.Empty(events);
	}
}
