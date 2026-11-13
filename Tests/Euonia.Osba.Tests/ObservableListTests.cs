using System.Collections.Specialized;
using System.ComponentModel;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

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
		public event PropertyChangedEventHandler? PropertyChanged;
		public event BusyChangedEventHandler? BusyChanged;

		private string _name = null!;

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

	/// <summary>
	/// 验证向列表插入实现 <see cref="INotifyPropertyChanged"/> 的项后，子项属性变更会以一次
	/// <see cref="ObservableList{TItem}.ChildChanged"/> 事件上报，且事件携带该子项与属性名。
	/// </summary>
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

	/// <summary>
	/// 验证插入实现 <see cref="INotifyBusy"/> 的项后，子项忙碌状态变化会透传为列表的一次
	/// <see cref="ObservableList{TItem}.BusyChanged"/> 事件，且事件中忙碌标志为 <see langword="true"/>。
	/// </summary>
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

	/// <summary>
	/// 验证以索引赋值替换元素时事件钩子随之迁移：旧项不再触发 <see cref="ObservableList{TItem}.ChildChanged"/>，
	/// 新项的属性变更能够被正常上报。
	/// </summary>
	[Fact]
	public void SetItem_ShouldTransferHooksFromOldToNewItem()
	{
		var (list, childEvents, _, _, _) = Create();
		var oldItem = new TestItem { Name = "old" };
		var newItem = new TestItem { Name = "new" };
		list.Add(oldItem);

		list[0] = newItem;
		childEvents.Clear();

		// 被替换掉的旧项不再引发通知：事件钩子随替换分离
		oldItem.Name = "changed";
		Assert.Empty(childEvents);

		// 新项应正确挂上钩子
		newItem.Name = "changed";
		var e = Assert.Single(childEvents);
		Assert.Same(newItem, e.ChangedObject);
	}

	/// <summary>
	/// 验证移除元素时会解除其事件钩子：被移除项此后的属性变更不再引发
	/// <see cref="ObservableList{TItem}.ChildChanged"/> 事件。
	/// </summary>
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

	/// <summary>
	/// 验证清空列表会解除所有元素的钩子：原各项此后的属性变更均不再引发
	/// <see cref="ObservableList{TItem}.ChildChanged"/> 事件。
	/// </summary>
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

		// Clear 要分离每个被清空元素的事件钩子
		item1.Name = "changed";
		item2.Name = "changed";

		Assert.Empty(childEvents);
	}

	/// <summary>
	/// 验证添加元素会引发一次集合变更通知，并同时发出 <c>Count</c> 与 <c>Item[]</c> 的属性变更通知。
	/// </summary>
	[Fact]
	public void Add_ShouldRaiseCollectionAndPropertyChanged()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();

		list.Add(new TestItem());

		Assert.Single(collectionEvents);
		Assert.Contains(propertyEvents, e => e.PropertyName == "Count");
		Assert.Contains(propertyEvents, e => e.PropertyName == "Item[]");
	}

	/// <summary>
	/// 验证将 <see cref="ObservableList{TItem}.RaiseListChangedEvents"/> 设为 <see langword="false"/> 后，
	/// 添加元素不再引发集合变更与属性变更通知。
	/// </summary>
	[Fact]
	public void RaiseListChangedEvents_False_ShouldSuppressAllNotifications()
	{
		var (list, _, collectionEvents, propertyEvents, _) = Create();
		list.RaiseListChangedEvents = false;

		list.Add(new TestItem());

		// Clear 之后不应再引发 Count / Item[] 的变更通知
		Assert.Empty(collectionEvents);
		Assert.Empty(propertyEvents);
	}

	/// <summary>
	/// 验证 <see cref="ObservableList{TItem}.SuppressListChangedEvents"/> 作用域内抑制通知，
	/// 释放后恢复通知能力并继续发出集合与属性变更事件。
	/// </summary>
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

	/// <summary>
	/// 验证嵌套抑制作用域逐层恢复：内层释放后仍保持外层抑制，全部释放后回到进入前的初始状态。
	/// </summary>
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
