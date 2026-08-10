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
}

