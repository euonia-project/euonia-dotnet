using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 父子对象图：子对象属性变更通知父对象；父对象的规则绕过穿透到子对象。
/// </summary>
public class ChildGraphTests
{
	[Fact]
	public void Child_Change_Should_Mark_Parent_Changed_And_Notify_Parent_Property()
	{
		var parent = new ParentProbe();
		var child = new ChildProbe();
		parent.Child = child;
		parent.AcceptChanges();

		string observed = null;
		parent.PropertyChanged += (_, args) => observed = args.PropertyName;

		child.Name = "changed";

		Assert.Equal(nameof(ParentProbe.Child), observed);
		Assert.Contains(parent.ChangedProperties, property => ReferenceEquals(property, ParentProbe.ChildProperty));
		Assert.True(parent.HasChangedProperties);
	}

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

	[Fact]
	public void List_Collection_Change_Should_Notify_Parent()
	{
		var parent = new ParentListProbe { Children = new ObservableList<ChildProbe>() };
		parent.AcceptChanges();

		parent.Children.Add(new ChildProbe());

		Assert.True(parent.HasChangedProperties);
	}

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

	[Fact]
	public void Dictionary_Collection_Change_Should_Notify_Parent()
	{
		var parent = new ParentDictionaryProbe { Children = new ObservableDictionary<string, ChildProbe>() };
		parent.AcceptChanges();

		parent.Children.Add("a", new ChildProbe());

		Assert.True(parent.HasChangedProperties);
	}

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
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

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
	public static readonly PropertyInfo<ChildProbe> ChildProperty = RegisterProperty<ChildProbe>(p => p.Child);

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

/// <summary>持有子集合的父对象探针。</summary>
public sealed class ParentListProbe : ObservableObject<ParentListProbe>
{
	public static readonly PropertyInfo<ObservableList<ChildProbe>> ChildrenProperty =
		RegisterProperty<ObservableList<ChildProbe>>(p => p.Children);

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
	public static readonly PropertyInfo<ObservableDictionary<string, ChildProbe>> ChildrenProperty =
		RegisterProperty<ObservableDictionary<string, ChildProbe>>(p => p.Children);

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
	public static readonly PropertyInfo<ParentProbe> ChildProperty = RegisterProperty<ParentProbe>(p => p.Child);

	public ParentProbe Child
	{
		get => GetProperty(ChildProperty);
		set => SetProperty(ChildProperty, value);
	}

	/// <summary>供测试开启绕过。</summary>
	public IDisposable Bypass() => BypassRuleChecks;
}
