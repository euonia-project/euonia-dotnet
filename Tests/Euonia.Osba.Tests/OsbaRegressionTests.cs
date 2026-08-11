using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 FieldData 撤销/变更跟踪及 PropertyInfo.FriendlyName 的修复行为。
/// </summary>
public class OsbaRegressionTests
{
	[Fact]
	public void NewFieldData_ShouldNotBeChanged()
	{
		var field = new FieldData<string>("Name");

		Assert.False(field.IsChanged);
	}

	[Fact]
	public void SettingValue_ShouldMarkAsChanged()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";

		Assert.True(field.IsChanged);
	}

	[Fact]
	public void Undo_ShouldRestorePreviousValue_AndNotRepushHistory()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";
		field.Value = "second";

		field.Undo();

		// Undo 恢复上一次的值，且不会把该值重新压入历史栈
		Assert.Equal("first", field.Value);
		Assert.True(field.IsChanged);

		field.Undo();

		// 连续撤销应回到原始值，且 IsChanged 变为 false
		Assert.Null(field.Value);
		Assert.False(field.IsChanged);
	}

	[Fact]
	public void MarkAsUnchanged_ShouldClearHistory()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";
		field.MarkAsUnchanged();

		Assert.False(field.IsChanged);

		// 无历史时撤销不应改变当前值
		field.Undo();
		Assert.Equal("first", field.Value);
	}

	[Fact]
	public void FriendlyName_ShouldReturnPropertyName_WhenNotProvided()
	{
		var property = new PropertyInfo<string>("Name");

		Assert.Equal("Name", property.FriendlyName);
	}

	[Fact]
	public void FriendlyName_ShouldReturnExplicitValue_WhenProvided()
	{
		var property = new PropertyInfo<string>("Name", "Full Name", default(string));

		Assert.Equal("Full Name", property.FriendlyName);
	}

	[Fact]
	public void FieldData_BusyChangedEvent_ShouldNotThrow()
	{
		var field = new FieldData<string>("Name");
		BusyChangedEventArgs raised = null;

		((INotifyBusy)field).BusyChanged += (_, args) => raised = args;

		// 事件订阅本身不应抛出 NotImplementedException
		Assert.Null(raised);
	}

	[Fact]
	public void RegisterProperty_WithMutableDefault_ShouldNotShareInstanceAcrossObjects()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		first.Tags.Add("shared?");

		// 修改第一个对象的集合不应影响第二个对象
		Assert.Empty(second.Tags);
	}

	[Fact]
	public void RegisterProperty_WithFactory_ShouldCreateIndependentInstancePerObject()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		Assert.NotSame(first.FactoryTags, second.FactoryTags);

		first.FactoryTags.Add("x");

		Assert.Empty(second.FactoryTags);
	}

	[Fact]
	public void RegisterProperty_WithCloneableDefault_ShouldCreateIndependentInstancePerObject()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		Assert.NotSame(first.Child, second.Child);

		first.Child.Name = "changed";
		Assert.Equal("initial", second.Child.Name);
	}

	[Fact]
	public void RegisterProperty_WithValueTypeDefault_ShouldReturnDefaultValue()
	{
		var obj = new DefaultValueBusinessObject();

		Assert.Equal(0, obj.Number);
	}
}

/// <summary>
/// 用于验证默认值隔离的测试业务对象。
/// </summary>
public class DefaultValueBusinessObject : BusinessObject<DefaultValueBusinessObject>
{
	/// <summary>
	/// 使用可变引用类型（集合）作为静态默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<List<string>> TagsProperty =
		RegisterProperty<List<string>>(nameof(Tags), null, new List<string>());

	/// <summary>
	/// 使用工厂生成默认值的属性。
	/// </summary>
	public static readonly PropertyInfo<List<string>> FactoryTagsProperty =
		RegisterProperty<List<string>>(nameof(FactoryTags), null, () => new List<string>());

	/// <summary>
	/// 使用实现了 <see cref="ICloneable"/> 的引用类型默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<DefaultValueChild> ChildProperty =
		RegisterProperty<DefaultValueChild>(nameof(Child), null, new DefaultValueChild("initial"));

	/// <summary>
	/// 使用值类型默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<int> NumberProperty =
		RegisterProperty<int>(nameof(Number));

	public List<string> Tags => ReadProperty(TagsProperty);

	public List<string> FactoryTags => ReadProperty(FactoryTagsProperty);

	public DefaultValueChild Child => ReadProperty(ChildProperty);

	public int Number => ReadProperty(NumberProperty);
}

/// <summary>
/// 可克隆的测试子对象。
/// </summary>
public class DefaultValueChild : ICloneable
{
	public DefaultValueChild(string name)
	{
		Name = name;
	}

	public string Name { get; set; }

	public object Clone()
	{
		return new DefaultValueChild(Name);
	}
}
