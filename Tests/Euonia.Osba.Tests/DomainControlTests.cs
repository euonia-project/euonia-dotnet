using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证派生类对领域对象原始值、回滚与状态迁移的控制能力。
/// </summary>
public class DomainControlTests
{
	[Fact]
	public void FieldStorage_ShouldNotBePubliclyAccessible()
	{
		const System.Reflection.BindingFlags publicInstance =
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

		Assert.DoesNotContain(typeof(FieldDataManager).GetMethods(publicInstance), method => method.Name == "GetFieldData");
		Assert.DoesNotContain(typeof(FieldDataManager).GetMethods(publicInstance), method => method.Name == "MarkAllAsUnchanged");
	}

	[Fact]
	public void OriginalValues_ShouldKeepFirstValueAndRejectChanges()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.Name = "first";
		target.Name = "second";

		Assert.Equal(string.Empty, target.GetOriginalValue(DomainControlEditable.NameProperty));
		Assert.Equal("second", target.Name);
		Assert.Equal("second", target.GetChangedValues()[DomainControlEditable.NameProperty]);
		Assert.Equal(string.Empty, target.GetOriginalValues()[DomainControlEditable.NameProperty]);
		Assert.True(target.IsDirty);

		target.RejectChanges();

		Assert.Equal(string.Empty, target.Name);
		Assert.False(target.HasChangedProperties);
		Assert.False(target.IsDirty);
	}

	[Fact]
	public void RevertingProperty_ShouldClearDirtyState()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.Name = "changed";
		Assert.True(target.HasChangedProperties);

		target.Name = string.Empty;

		Assert.False(target.HasChangedProperties);
		Assert.Empty(target.ChangedProperties);
		Assert.False(target.IsDirty);
		Assert.False(target.FieldManager.GetFieldData(DomainControlEditable.NameProperty)!.IsChanged);
	}

	[Fact]
	public void RevertingOneProperty_ShouldKeepOtherPropertiesDirty()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.Name = "changed";
		target.Code = "A";

		target.Name = string.Empty;

		Assert.True(target.HasChangedProperties);
		Assert.Equal([DomainControlEditable.CodeProperty], target.ChangedProperties);

		target.Code = string.Empty;

		Assert.False(target.HasChangedProperties);
		Assert.Empty(target.ChangedProperties);
	}

	[Fact]
	public void BypassWrite_ShouldStillTrackDirtyState()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		using (target.BypassRuleChecks)
		{
			target.Name = "changed";
		}

		Assert.True(target.HasChangedProperties);

		using (target.BypassRuleChecks)
		{
			target.Name = string.Empty;
		}

		Assert.False(target.HasChangedProperties);
		Assert.False(target.IsDirty);

		// 非泛型 SetProperty 的绕过路径也必须保留变更追踪。
		using (target.BypassRuleChecks)
		{
			target.SetProperty(DomainControlEditable.NameProperty, "via-set");
		}

		Assert.True(target.HasChangedProperties);

		using (target.BypassRuleChecks)
		{
			target.SetProperty(DomainControlEditable.NameProperty, string.Empty);
		}

		Assert.False(target.HasChangedProperties);
	}

	[Fact]
	public void SetPropertyConvert_Reverting_ShouldClearDirtyState()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.SetCodeConverted("A");
		Assert.True(target.HasChangedProperties);

		target.SetCodeConverted(string.Empty);

		Assert.False(target.HasChangedProperties);
		Assert.Empty(target.ChangedProperties);
	}

	[Fact]
	public void RefreshChangedProperties_ShouldReconcileLowLevelUndo()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.Name = "changed";
		var field = target.FieldManager.GetFieldData(DomainControlEditable.NameProperty)!;
		field.Undo();

		Assert.True(target.HasChangedProperties);

		target.RefreshChangedProperties();

		Assert.False(target.HasChangedProperties);
		Assert.False(field.IsChanged);

		// 低层写入同样可以被 Refresh 重新纳入脏状态，再 Undo 后重新同步。
		field.Value = "direct";
		Assert.False(target.HasChangedProperties);

		target.RefreshChangedProperties();
		Assert.True(target.HasChangedProperties);

		field.Undo();
		target.RefreshChangedProperties();
		Assert.False(target.HasChangedProperties);
	}

	[Fact]
	public void RejectChanges_ShouldRestoreLoadedBaseline()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.LoadProperty(DomainControlEditable.NameProperty, "loaded");
		target.AcceptChanges();

		target.Name = "edited";

		Assert.Equal("loaded", target.GetOriginalValue(DomainControlEditable.NameProperty));
		Assert.Equal("edited", target.Name);

		target.RejectChanges();

		Assert.Equal("loaded", target.Name);
		Assert.Equal("loaded", target.GetOriginalValue(DomainControlEditable.NameProperty));
	}

	[Fact]
	public void FieldData_ShouldTrackChangeWhenHistoryDepthIsZero()
	{
		var field = new FieldData<int>("Count")
		{
			HistoryDepth = 0
		};

		field.Value = 1;
		Assert.True(field.IsChanged);
		Assert.Equal(0, field.OriginalValue);

		field.Value = 2;
		Assert.Equal(0, field.OriginalValue);

		field.Value = 0;
		Assert.False(field.IsChanged);

		field.Value = 2;
		field.MarkAsUnchanged();
		Assert.False(field.IsChanged);
		Assert.Equal(2, field.OriginalValue);
	}

	[Fact]
	public void StateTransitions_ShouldBeControllable()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);
		var transitions = new List<(ObjectEditState OldState, ObjectEditState NewState)>();

		target.StateChanged += (_, args) => transitions.Add((args.OldState, args.NewState));

		target.MarkAsNew();
		target.MarkAsChanged();

		Assert.Throws<InvalidOperationException>(() => target.MarkAsNew());

		target.MarkAsDeleted();

		Assert.Throws<InvalidOperationException>(() => target.MarkAsNew());

		target.AcceptChanges();

		Assert.Equal(ObjectEditState.None, target.State);
		Assert.Equal(
			new (ObjectEditState OldState, ObjectEditState NewState)[]
			{
				(ObjectEditState.None, ObjectEditState.New),
				(ObjectEditState.New, ObjectEditState.Changed),
				(ObjectEditState.Changed, ObjectEditState.Deleted),
				(ObjectEditState.Deleted, ObjectEditState.None)
			}, transitions);
	}

	[Fact]
	public void RejectChanges_ShouldCascadeToEditedChildObject()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var parent = RuleTestHarness.Create<ParentEditable>(provider);
		var child = RuleTestHarness.Create<ChildEditable>(provider);

		parent.Child = child;
		child.Title = "original";
		parent.AcceptChanges();

		// 就地编辑子对象：父对象通过子图传播变为脏。
		child.Title = "edited";
		Assert.True(parent.HasChangedProperties);

		parent.RejectChanges();

		Assert.Equal("original", child.Title);
		Assert.Equal("original", parent.Child.Title);
		Assert.False(parent.HasChangedProperties);
		Assert.False(child.HasChangedProperties);
	}

	[Fact]
	public void RejectChanges_ShouldRaisePropertyChangedForRestoredValues()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		var notifications = new List<string>();
		target.PropertyChanged += (_, args) => notifications.Add(args.PropertyName!);

		target.Name = "edited";
		notifications.Clear();

		target.RejectChanges();

		// 回滚装载不经过 setter，必须补发通知，否则绑定停留在编辑后的值。
		Assert.Contains(nameof(DomainControlEditable.Name), notifications);
		Assert.Equal(string.Empty, target.Name);
	}

	[Fact]
	public void RejectChanges_ShouldNotNotifyUnchangedScalar()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<DomainControlEditable>(provider);

		target.Name = "kept";
		target.AcceptChanges();

		var notifications = new List<string>();
		target.PropertyChanged += (_, args) => notifications.Add(args.PropertyName!);

		// 子对象内容变化会把父对象标脏（子对象属性在 ChangedProperties 里），
		// 但标量属性本身没有变化：回滚时不应为它补发通知。
		Assert.False(target.HasChangedProperties);
		Assert.Empty(notifications);
	}
}

/// <summary>
/// 带严格状态迁移规则的可编辑对象。
/// </summary>
public sealed class DomainControlEditable : EditableObject<DomainControlEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	public static readonly PropertyInfo<string> CodeProperty = RegisterProperty(p => p.Code);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public string Code
	{
		get => GetProperty(CodeProperty);
		set => SetProperty(CodeProperty, value);
	}

	public void SetCodeConverted(string code)
	{
		SetPropertyConvert(CodeProperty, code);
	}

	protected override bool CanTransitionTo(ObjectEditState target)
	{
		return State switch
		{
			ObjectEditState.None => target is ObjectEditState.New or ObjectEditState.Changed or ObjectEditState.Deleted,
			ObjectEditState.New => target is ObjectEditState.Changed or ObjectEditState.Deleted,
			ObjectEditState.Changed => target is ObjectEditState.Deleted,
			ObjectEditState.Deleted => false,
			_ => true
		};
	}
}

/// <summary>
/// 持有可编辑子对象的父对象，用于验证回滚级联。
/// </summary>
public sealed class ParentEditable : EditableObject<ParentEditable>
{
	public static readonly PropertyInfo<ChildEditable> ChildProperty = RegisterProperty(p => p.Child);

	public ChildEditable Child
	{
		get => GetProperty(ChildProperty);
		set => SetProperty(ChildProperty, value);
	}
}

/// <summary>
/// 被父对象持有的可编辑子对象。
/// </summary>
public sealed class ChildEditable : EditableObject<ChildEditable>
{
	public static readonly PropertyInfo<string> TitleProperty = RegisterProperty(p => p.Title);

	public string Title
	{
		get => GetProperty(TitleProperty);
		set => SetProperty(TitleProperty, value);
	}
}
