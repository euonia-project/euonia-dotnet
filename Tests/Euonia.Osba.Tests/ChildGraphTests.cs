using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 父子对象图：子对象属性变更通知父对象；父对象的规则绕过穿透到子对象。
/// </summary>
public class ChildGraphTests
{
	/// <summary>
	/// 验证子对象属性变更会把父对象标记为已更改，并发出父对象对应属性的变更通知。
	/// </summary>
	[Fact]
	public void Child_Change_Should_Mark_Parent_Changed_And_Notify_Parent_Property()
	{
		var parent = new ParentProbe();
		var child = new ChildProbe();
		parent.Child = child;
		parent.AcceptChanges();

		string? observed = null;
		parent.PropertyChanged += (_, args) => observed = args.PropertyName;

		child.Name = "changed";

		Assert.Equal(nameof(ParentProbe.Child), observed);
		Assert.Contains(parent.ChangedProperties, property => ReferenceEquals(property, ParentProbe.ChildProperty));
		Assert.True(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证子对象的变更会经由父对象逐级传播到祖父对象。
	/// </summary>
	[Fact]
	public void Child_Change_Should_Propagate_To_GrandParent()
	{
		var grand = new GrandParentProbe();
		var parent = new ParentProbe();
		var child = new ChildProbe();

		grand.Child = parent;
		parent.Child = child;
		grand.AcceptChanges();
		parent.AcceptChanges();

		child.Name = "changed";

		Assert.True(parent.HasChangedProperties);
		Assert.True(grand.HasChangedProperties);
		Assert.Contains(grand.ChangedProperties, property => ReferenceEquals(property, GrandParentProbe.ChildProperty));
	}

	/// <summary>
	/// 验证子对象被替换后，旧子对象不再通知父对象，而新子对象仍会通知。
	/// </summary>
	[Fact]
	public void Replaced_Child_Should_Stop_Notifying_Parent()
	{
		var parent = new ParentProbe();
		var oldChild = new ChildProbe();
		var newChild = new ChildProbe();

		parent.Child = oldChild;
		parent.Child = newChild;
		parent.AcceptChanges();

		oldChild.Name = "old";
		Assert.False(parent.HasChangedProperties);

		newChild.Name = "new";
		Assert.True(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证同一子对象被多个父对象持有时，变更会通知到每一个父对象。
	/// </summary>
	[Fact]
	public void Child_Change_Should_Notify_All_Parents()
	{
		var first = new ParentProbe();
		var second = new ParentProbe();
		var child = new ChildProbe();

		first.Child = child;
		second.Child = child;
		first.AcceptChanges();
		second.AcceptChanges();

		child.Name = "changed";

		Assert.True(first.HasChangedProperties);
		Assert.True(second.HasChangedProperties);
	}

	/// <summary>
	/// 验证父对象的规则绕过会传导到子对象，绕过期间的子对象写入不进入变更追踪。
	/// </summary>
	[Fact]
	public void Parent_Bypass_Should_Reach_Child()
	{
		var parent = new ParentProbe();
		var child = new ChildProbe();
		parent.Child = child;
		parent.AcceptChanges();

		using (parent.Bypass())
		{
			Assert.True(child.IsBypassing);

			// 绕过期间子对象写入不进入变更追踪，也不通知父对象。
			child.Name = "bypassed";
		}

		Assert.False(child.IsBypassing);
		Assert.False(parent.HasChangedProperties);
		Assert.Equal("bypassed", child.Name);
	}

	/// <summary>
	/// 验证绕过状态可跨多代传播，内层作用域退出后仍保持外层绕过，全部退出后恢复。
	/// </summary>
	[Fact]
	public void Bypass_Should_Propagate_Across_Generations_And_Restore()
	{
		var grand = new GrandParentProbe();
		var parent = new ParentProbe();
		var child = new ChildProbe();

		grand.Child = parent;
		parent.Child = child;

		using (grand.Bypass())
		{
			Assert.True(child.IsBypassing);

			using (parent.Bypass())
			{
				Assert.True(child.IsBypassing);
			}

			// 内层退出后仍应看到祖父层的绕过。
			Assert.True(child.IsBypassing);
		}

		Assert.False(child.IsBypassing);
	}

	/// <summary>
	/// 验证子对象开启的绕过不会反向传导到父对象。
	/// </summary>
	[Fact]
	public void Child_Bypass_Should_Not_Leak_To_Parent()
	{
		var parent = new ParentProbe();
		var child = new ChildProbe();
		parent.Child = child;

		using (child.Bypass())
		{
			Assert.True(child.IsBypassing);
			Assert.False(parent.IsBypassing);
		}
	}

	/// <summary>
	/// 验证列表内元素的属性变更会通知持有该列表的父对象，并标记其集合属性。
	/// </summary>
	[Fact]
	public void List_Item_Change_Should_Notify_Parent()
	{
		var parent = new ParentListProbe();
		var list = new ObservableList<ChildProbe>();
		var child = new ChildProbe();

		parent.Children = list;
		list.Add(child);
		parent.AcceptChanges();

		child.Name = "changed";

		Assert.True(parent.HasChangedProperties);
		Assert.Contains(parent.ChangedProperties, property => ReferenceEquals(property, ParentListProbe.ChildrenProperty));
	}

	/// <summary>
	/// 验证父对象开启绕过时，赋值给父对象之前已存在于列表中的元素同样进入绕过状态。
	/// </summary>
	[Fact]
	public void Parent_Bypass_Should_Reach_Items_Already_In_List()
	{
		var parent = new ParentListProbe();
		var list = new ObservableList<ChildProbe>();
		var child = new ChildProbe();

		list.Add(child);
		parent.Children = list;
		parent.AcceptChanges();

		using (parent.Bypass())
		{
			Assert.True(child.IsBypassing);
			child.Name = "bypassed";
		}

		Assert.False(child.IsBypassing);
		Assert.False(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证列表的集合结构变更（新增元素）会标记父对象为已更改。
	/// </summary>
	[Fact]
	public void List_Collection_Change_Should_Notify_Parent()
	{
		var parent = new ParentListProbe { Children = new ObservableList<ChildProbe>() };
		parent.AcceptChanges();

		parent.Children.Add(new ChildProbe());

		Assert.True(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证字典值的属性变更会通知持有该字典的父对象，并标记其集合属性。
	/// </summary>
	[Fact]
	public void Dictionary_Value_Change_Should_Notify_Parent()
	{
		var parent = new ParentDictionaryProbe();
		var dictionary = new ObservableDictionary<string, ChildProbe>();
		var child = new ChildProbe();

		parent.Children = dictionary;
		dictionary.Add("a", child);
		parent.AcceptChanges();

		child.Name = "changed";

		Assert.True(parent.HasChangedProperties);
		Assert.Contains(parent.ChangedProperties, property => ReferenceEquals(property, ParentDictionaryProbe.ChildrenProperty));
	}

	/// <summary>
	/// 验证在赋给父对象之前已加入字典的值也会被挂接，因而能受父对象绕过影响。
	/// </summary>
	[Fact]
	public void Dictionary_Value_Added_Before_Parent_Should_Be_Attached()
	{
		var parent = new ParentDictionaryProbe();
		var dictionary = new ObservableDictionary<string, ChildProbe>();
		var child = new ChildProbe();
		dictionary.Add("a", child);

		parent.Children = dictionary;

		using (parent.Bypass())
		{
			Assert.True(child.IsBypassing);
		}
	}

	/// <summary>
	/// 验证从字典移除键后，对应值不再向父对象传播变更。
	/// </summary>
	[Fact]
	public void Dictionary_Remove_Should_Detach_Value()
	{
		var parent = new ParentDictionaryProbe();
		var dictionary = new ObservableDictionary<string, ChildProbe>();
		var child = new ChildProbe();
		parent.Children = dictionary;
		dictionary.Add("a", child);
		parent.AcceptChanges();

		dictionary.Remove("a");
		parent.AcceptChanges();

		child.Name = "after-remove";

		Assert.False(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证同一值被多个键引用时，只有最后一个键被移除才会解除与父对象的挂接。
	/// </summary>
	[Fact]
	public void Dictionary_Duplicate_Value_Should_Detach_Only_When_Last_Key_Removed()
	{
		var parent = new ParentDictionaryProbe();
		var dictionary = new ObservableDictionary<string, ChildProbe>();
		var child = new ChildProbe();
		parent.Children = dictionary;
		dictionary.Add("a", child);
		dictionary.Add("b", child);
		parent.AcceptChanges();

		dictionary.Remove("a");
		parent.AcceptChanges();

		child.Name = "still-linked";
		Assert.True(parent.HasChangedProperties);

		parent.AcceptChanges();
		dictionary.Remove("b");
		parent.AcceptChanges();

		child.Name = "detached";
		Assert.False(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证字典的集合结构变更（新增键值）会标记父对象为已更改。
	/// </summary>
	[Fact]
	public void Dictionary_Collection_Change_Should_Notify_Parent()
	{
		var parent = new ParentDictionaryProbe { Children = new ObservableDictionary<string, ChildProbe>() };
		parent.AcceptChanges();

		parent.Children.Add("a", new ChildProbe());

		Assert.True(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证父子互相引用时变更通知不会无限递归，父对象仍被正确标记为已更改。
	/// </summary>
	[Fact]
	public void Bidirectional_References_Should_Not_Recurse_Notifications()
	{
		var parent = new CyclicParentProbe();
		var child = new CyclicChildProbe();

		parent.Child = child;
		child.Parent = parent;
		parent.AcceptChanges();

		child.Name = "changed";

		Assert.True(parent.HasChangedProperties);
	}

	/// <summary>
	/// 验证父对象释放后不再接收子对象的变更通知。
	/// </summary>
	[Fact]
	public void Parent_Dispose_Should_Detach_Child_Notifications()
	{
		var parent = new ParentProbe();
		var child = new ChildProbe();
		parent.Child = child;
		parent.AcceptChanges();

		parent.Dispose();
		child.Name = "after-dispose";

		Assert.False(parent.HasChangedProperties);
	}
}

/// <summary>子对象探针。</summary>
public sealed class ChildProbe : ObservableObject<ChildProbe>
{
	/// <summary>子对象名称的属性元数据。</summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>子对象的名称，写入时触发变更追踪与父对象通知。</summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>供测试观察当前流是否处于绕过状态。</summary>
	public bool IsBypassing => IsBypassingRuleChecks;

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}

/// <summary>父对象探针。</summary>
public sealed class ParentProbe : ObservableObject<ParentProbe>
{
	/// <summary>父对象所持有的子对象属性元数据。</summary>
	public static readonly PropertyInfo<ChildProbe> ChildProperty = RegisterProperty<ChildProbe>(p => p.Child);

	/// <summary>父对象持有的子对象，赋值时建立子图变更通知关系。</summary>
	public ChildProbe Child
	{
		get => GetProperty(ChildProperty);
		set => SetProperty(ChildProperty, value);
	}

	/// <summary>供测试观察当前流是否处于绕过状态。</summary>
	public bool IsBypassing => IsBypassingRuleChecks;

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}

/// <summary>双向引用父对象探针。</summary>
public sealed class CyclicParentProbe : ObservableObject<CyclicParentProbe>
{
	/// <summary>双向引用场景中父对象持有的子对象属性元数据。</summary>
	public static readonly PropertyInfo<CyclicChildProbe> ChildProperty = RegisterProperty<CyclicChildProbe>(p => p.Child);

	/// <summary>双向引用场景中父对象持有的子对象。</summary>
	public CyclicChildProbe Child
	{
		get => GetProperty(ChildProperty);
		set => SetProperty(ChildProperty, value);
	}
}

/// <summary>双向引用子对象探针。</summary>
public sealed class CyclicChildProbe : ObservableObject<CyclicChildProbe>
{
	/// <summary>双向引用场景中子对象名称的属性元数据。</summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
	/// <summary>双向引用场景中子对象指回父对象的属性元数据。</summary>
	public static readonly PropertyInfo<CyclicParentProbe> ParentProperty = RegisterProperty<CyclicParentProbe>(p => p.Parent);

	/// <summary>双向引用场景中子对象的名称，写入时用于验证通知不会递归。</summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>双向引用场景中子对象指回的父对象。</summary>
	public CyclicParentProbe Parent
	{
		get => GetProperty(ParentProperty);
		set => SetProperty(ParentProperty, value);
	}
}

/// <summary>持有子集合的父对象探针。</summary>
public sealed class ParentListProbe : ObservableObject<ParentListProbe>
{
	/// <summary>父对象所持有的子对象列表属性元数据。</summary>
	public static readonly PropertyInfo<ObservableList<ChildProbe>> ChildrenProperty =
		RegisterProperty<ObservableList<ChildProbe>>(p => p.Children);

	/// <summary>父对象持有的可观察子对象列表，元素与集合变更均会传播。</summary>
	public ObservableList<ChildProbe> Children
	{
		get => GetProperty(ChildrenProperty);
		set => SetProperty(ChildrenProperty, value);
	}

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}

/// <summary>持有子字典的父对象探针。</summary>
public sealed class ParentDictionaryProbe : ObservableObject<ParentDictionaryProbe>
{
	/// <summary>父对象所持有的子对象字典属性元数据。</summary>
	public static readonly PropertyInfo<ObservableDictionary<string, ChildProbe>> ChildrenProperty =
		RegisterProperty<ObservableDictionary<string, ChildProbe>>(p => p.Children);

	/// <summary>父对象持有的可观察子对象字典，值变更与集合变更均会传播。</summary>
	public ObservableDictionary<string, ChildProbe> Children
	{
		get => GetProperty(ChildrenProperty);
		set => SetProperty(ChildrenProperty, value);
	}

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}

/// <summary>祖父对象探针。</summary>
public sealed class GrandParentProbe : ObservableObject<GrandParentProbe>
{
	/// <summary>祖父对象所持有的父对象属性元数据。</summary>
	public static readonly PropertyInfo<ParentProbe> ChildProperty = RegisterProperty<ParentProbe>(p => p.Child);

	/// <summary>祖父对象持有的父对象，用于验证跨代变更传播。</summary>
	public ParentProbe Child
	{
		get => GetProperty(ChildProperty);
		set => SetProperty(ChildProperty, value);
	}

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}
