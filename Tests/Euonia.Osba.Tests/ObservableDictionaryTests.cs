using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

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

	[Fact]
	public void Indexer_SameValue_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();
		dict["key"] = 1;
		events.Clear();

		dict["key"] = 1;

		Assert.Empty(events);
	}

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

	[Fact]
	public void Add_DuplicateKey_ShouldThrowAndNotRaiseEvent()
	{
		var (dict, events) = Create();
		dict.Add("key", 1);
		events.Clear();

		Assert.Throws<ArgumentException>(() => dict.Add("key", 2));
		Assert.Empty(events);
	}

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

	[Fact]
	public void Remove_MissingKey_ShouldReturnFalseAndNotRaiseEvent()
	{
		var (dict, events) = Create();

		var result = dict.Remove("key");

		Assert.False(result);
		Assert.Empty(events);
	}

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

	[Fact]
	public void RemoveWithOut_MissingKey_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();

		var result = dict.Remove("key", out var value);

		Assert.False(result);
		Assert.Equal(0, value);
		Assert.Empty(events);
	}

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

	[Fact]
	public void Clear_Empty_ShouldNotRaiseEvent()
	{
		var (dict, events) = Create();

		dict.Clear();

		Assert.Empty(events);
	}

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
