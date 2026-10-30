using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证派生类对领域对象原始值、回滚与状态迁移的控制能力。
/// </summary>
public class DomainControlTests
{
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
}

/// <summary>
/// 带严格状态迁移规则的可编辑对象。
/// </summary>
public sealed class DomainControlEditable : EditableObject<DomainControlEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
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
