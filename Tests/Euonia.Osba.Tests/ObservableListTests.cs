using System.Collections.Specialized;
using System.ComponentModel;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 ObservableList 的子项事件钩子管理与更改通知逻辑。
/// </summary>
public class ObservableListTests
{
	/// <summary>
	/// 同时实现 INotifyBusy 和 INotifyPropertyChanged 的测试项。
	/// </summary>
	private class TestItem : INotifyBusy, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event BusyChangedEventHandler BusyChanged;

		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
				}
			}
		}

		private bool _isBusy;

		public bool IsBusy
		{
			get => _isBusy;
			set
			{
				if (_isBusy != value)
				{
					_isBusy = value;
					BusyChanged?.Invoke(this, new BusyChangedEventArgs(nameof(IsBusy), value));
				}
			}
		}

		public bool IsSelfBusy => IsBusy;
	}

	private static (ObservableList<TestItem> list, List<ObjectChangedEventArgs> childEvents, List<NotifyCollectionChangedEventArgs> collectionEvents, List<PropertyChangedEventArgs> propertyEvents, List<BusyChangedEventArgs> busyEvents) Create()
	{
		var list = new ObservableList<TestItem>();
		var childEvents = new List<ObjectChangedEventArgs>();
		var collectionEvents = new List<NotifyCollectionChangedEventArgs>();
		var propertyEvents = new List<PropertyChangedEventArgs>();
		var busyEvents = new List<BusyChangedEventArgs>();

		list.ChildChanged += (_, e) => childEvents.Add(e);
		list.CollectionChanged += (_, e) => collectionEvents.Add(e);
		((INotifyPropertyChanged)list).PropertyChanged += (_, e) => propertyEvents.Add(e);
		list.BusyChanged += (_, e) => busyEvents.Add(e);

		return (list, childEvents, collectionEvents, propertyEvents, busyEvents);
	}

	[Fact]
	public void InsertItem_ShouldHookChildPropertyChanges()
	{
		var (list, childEvents, _, _, _) = Create();
		var item = new TestItem();
		list.Add(item);

		item.Name = "changed";

		var e = Assert.Single(childEvents);
		Assert.Same(item, e.ChangedObject);
		Assert.Equal(nameof(TestItem.Name), e.PropertyChangedArgs.PropertyName);
	}

	[Fact]
	public void InsertItem_ShouldHookChildBusyChanges()
	{
		var (list, _, _, _, busyEvents) = Create();
		var item = new TestItem();
		list.Add(item);

		item.IsBusy = true;

		var e = Assert.Single(busyEvents);
		Assert.True(e.IsBusy);
	}

	[Fact]
	public void SetItem_ShouldTransferHooksFromOldToNewItem()
	{
		var (list, childEvents, _, _, _) = Create();
		var oldItem = new TestItem { Name = "old" };
		var newItem = new TestItem { Name = "new" };
		list.Add(oldItem);

		list[0] = newItem;
		childEvents.Clear();

		// 被替换掉的旧项不应再引发通知（修复前会泄漏钩子）
		oldItem.Name = "changed";
		Assert.Empty(childEvents);

		// 新项应正确挂上钩子
		newItem.Name = "changed";
		var e = Assert.Single(childEvents);
		Assert.Same(newItem, e.ChangedObject);
	}

	[Fact]
	public void RemoveItem_ShouldRemoveHooks()
	{
		var (list, childEvents, _, _, _) = Create();
		var item = new TestItem();
		list.Add(item);

		list.Remove(item);
		childEvents.Clear();

		item.Name = "changed";

		Assert.Empty(childEvents);
	}

	[Fact]
	public void Clear_ShouldRemoveHooksOfAllItems()
	{
		var (list, childEvents, _, _, _) = Create();
		var item1 = new TestItem();
		var item2 = new TestItem();
		list.Add(item1);
		list.Add(item2);

		list.Clear();
		childEvents.Clear();

		// 修复前 Clear 不会分离钩子，被清空的元素仍会误触发 ChildChanged
		item1.Name = "changed";
		item2.Name = "changed";

		Assert.Empty(childEvents);
	}

	[Fact]
	public void Add_ShouldRaiseCollectionAndPropertyChanged()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();

		list.Add(new TestItem());

		Assert.Single(collectionEvents);
		Assert.Contains(propertyEvents, e => e.PropertyName == "Count");
		Assert.Contains(propertyEvents, e => e.PropertyName == "Item[]");
	}

	[Fact]
	public void RaiseListChangedEvents_False_ShouldSuppressAllNotifications()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();
		list.RaiseListChangedEvents = false;

		list.Add(new TestItem());

		// 修复前 PropertyChanged（Count/Item[]）仍会被引发
		Assert.Empty(collectionEvents);
		Assert.Empty(propertyEvents);
	}

	[Fact]
	public void SuppressListChangedEvents_ShouldSuppressAndRestore()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();

		using (list.SuppressListChangedEvents)
		{
			list.Add(new TestItem());
			Assert.Empty(collectionEvents);
			Assert.Empty(propertyEvents);
		}

		list.Add(new TestItem());

		Assert.Single(collectionEvents);
		Assert.Contains(propertyEvents, e => e.PropertyName == "Count");
	}

	[Fact]
	public void SuppressListChangedEvents_Nested_ShouldRestoreInitialState()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();
		list.RaiseListChangedEvents = false;

		using (list.SuppressListChangedEvents)
		{
			using (list.SuppressListChangedEvents)
			{
				list.Add(new TestItem());
			}

			// 内层释放后仍处于外层抑制状态
			list.Add(new TestItem());
			Assert.Empty(collectionEvents);
			Assert.Empty(propertyEvents);
		}

		// 外层释放后应恢复为最初的 false，而不是错误地恢复为 true
		Assert.False(list.RaiseListChangedEvents);
		Assert.Empty(collectionEvents);
		Assert.Empty(propertyEvents);
	}
}
