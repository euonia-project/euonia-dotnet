using System.ComponentModel.DataAnnotations;
using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using ValidationException = Nerosoft.Euonia.Validation.ValidationException;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// M1 严格性与 M3 存储模型收敛的回归护栏。
/// </summary>
public class StrictnessRegressionTests
{
	[Fact]
	public async Task ValueTask_Factory_Method_Should_Be_Awaited()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new ValueTaskPersistObject { BusinessContext = context };
		target.MarkAsNew();

		await target.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		// 旧实现把 ValueTask 当成同步返回值直接丢弃，SaveAsync 返回时 Persisted 仍为 0。
		Assert.Equal(1, target.Persisted);
	}

	[Fact]
	public async Task Missing_Insert_Factory_Method_Should_Fail_Instead_Of_Silently_Doing_Nothing()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new NoPersistenceObject { BusinessContext = context };
		target.MarkAsNew();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => target.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Factory_Exception_Should_Not_Be_Wrapped_In_TargetInvocationException()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new ThrowingInsertObject { BusinessContext = context };
		target.MarkAsNew();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => target.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal("business failure", exception.Message);
	}

	[Fact]
	public async Task Missing_Execute_Factory_Method_Should_Fail_Instead_Of_Silently_Doing_Nothing()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => factory.ExecuteAsync(new NoExecuteCommand()));
	}

	[Fact]
	public void Ref_Backed_Field_Should_Be_Readable_By_Rules_And_DataAnnotations()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new RefBackedRuleObject { BusinessContext = context };
		target.Name = "123";

		Assert.True(target.IsValid);
		Assert.Empty(target.GetBrokenRules());
	}

	[Fact]
	public void NonGeneric_SetProperty_With_Same_Value_Should_Not_Mark_Changed()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new NonGenericSetObject { BusinessContext = context };
		target.Name = "same";
		target.AcceptChanges();

		((IOperableProperty)target).SetProperty(NonGenericSetObject.NameProperty, "same");

		Assert.False(target.HasChangedProperties);
		Assert.Empty(target.ChangedProperties);
	}

	[Fact]
	public async Task ReadOnly_Object_Should_Reject_LoadProperty_Outside_Factory_Load_Scope()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new LoadableReadOnlyObject { BusinessContext = context };

		Assert.Throws<SecurityException>(() =>
			((IBusinessObject)target).LoadProperty(LoadableReadOnlyObject.NameProperty, "external"));
	}

	[Fact]
	public async Task ReadOnly_Object_Should_Allow_LoadProperty_Inside_Factory_Load_Scope()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var loaded = await factory.FetchAsync<LoadableReadOnlyObject>("loaded");

		Assert.Equal("loaded", loaded.Name);
	}

	[Fact]
	public async Task Nested_Operation_Rule_Scope_Should_Not_Be_Cleared_By_Inner_Detach()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new ScopeProbeObject { BusinessContext = context };
		var rules = target.RuleSet;

		var outer = new RuleScope();
		outer.Add(new AlwaysFailRule());
		rules.AttachOperationRules(outer);

		var inner = new RuleScope();
		inner.Add(new AlwaysPassRule());
		rules.AttachOperationRules(inner);

		// 先结束内层：外层规则范围必须仍然有效。
		rules.DetachOperationRules(inner);

		await Assert.ThrowsAsync<ValidationException>(
			() => target.EnsureValidAsync(TestContext.Current.CancellationToken));

		rules.DetachOperationRules(outer);
	}

	[Fact]
	public void FieldData_ObservableValue_Should_Publish_Current_And_Subsequent_Values()
	{
		var field = new FieldData<int>("Number") { Value = 1 };
		var observer = new ListObserver<int>();

		using (field.ObservableValue.Subscribe(observer))
		{
			field.Value = 2;
		}

		field.Value = 3;

		Assert.Equal([1, 2], observer.Values);
	}

	[Fact]
	public void LoadProperty_Should_Mark_Field_As_Unchanged()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new NonGenericSetObject { BusinessContext = context };
		target.Name = "changed";

		target.LoadProperty(NonGenericSetObject.NameProperty, "loaded");

		Assert.Equal("loaded", target.Name);
		Assert.False(target.FieldManager.GetFieldData(NonGenericSetObject.NameProperty)!.IsChanged);
	}

	[Fact]
	public void Shrinking_HistoryDepth_Should_Preserve_Undo_Order()
	{
		var field = new FieldData<int>("Number");
		field.Value = 1;
		field.Value = 2;
		field.Value = 3;
		field.Value = 4;

		field.HistoryDepth = 2;

		field.Undo();
		Assert.Equal(3, field.Value);
		field.Undo();
		Assert.Equal(2, field.Value);
	}

	[Fact]
	public async Task Model_Rules_Should_See_Ref_Backed_Value_After_Set()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new RefBackedRuleObject { BusinessContext = context };
		target.Name = "abc";

		// 值不符合 Regular：规则必须看到 "abc" 并报错，而不是读到默认空串后报“必填”。
		Assert.False(target.IsValid);
		Assert.Contains(target.GetBrokenRules(), rule => rule.Description.Contains("数字", StringComparison.Ordinal));
	}
}

/// <summary>ValueTask 工厂方法探针。</summary>
public class ValueTaskPersistObject : EditableObject<ValueTaskPersistObject>
{
	public int Persisted { get; private set; }

	[FactoryInsert]
	protected async ValueTask PersistAsync(CancellationToken cancellationToken = default)
	{
		// 强制产生异步续体：旧实现不会等待它。
		await Task.Yield();
		Persisted++;
	}

	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>没有实现 Insert 的对象：保存必须失败，而不是静默 no-op。</summary>
public class NoPersistenceObject : EditableObject<NoPersistenceObject>
{
}

/// <summary>同步工厂方法抛出的业务异常必须原样传播。</summary>
public class ThrowingInsertObject : EditableObject<ThrowingInsertObject>
{
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		throw new InvalidOperationException("business failure");
	}

	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>没有实现 Execute 的命令：执行必须失败，而不是静默 no-op。</summary>
public class NoExecuteCommand : CommandObject<NoExecuteCommand>
{
	[FactoryCreate]
	private Task CreateAsync()
	{
		return Task.CompletedTask;
	}
}

/// <summary>字段模式 + DataAnnotations/CommonRule 探针。</summary>
public class RefBackedRuleObject : EditableObject<RefBackedRuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	private string _name = string.Empty;

	[Required(ErrorMessage = "必填")]
	public string Name
	{
		get => GetProperty(NameProperty, _name);
		set => SetProperty(NameProperty, ref _name, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Regular(NameProperty, "^[0-9]+$", "只能是数字"));
	}
}

/// <summary>非泛型 SetProperty 探针。</summary>
public class NonGenericSetObject : EditableObject<NonGenericSetObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}

/// <summary>只在工厂装载作用域内可填充的只读对象。</summary>
public class LoadableReadOnlyObject : ReadOnlyObject<LoadableReadOnlyObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public string Name => GetProperty(NameProperty);

	[FactoryFetch]
	private Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		LoadProperty(NameProperty, id);
		return Task.CompletedTask;
	}
}

/// <summary>规则范围栈探针。</summary>
public class ScopeProbeObject : EditableObject<ScopeProbeObject>
{
}

/// <summary>总是失败的对象级规则。</summary>
public sealed class AlwaysFailRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("always fail");
		return Task.CompletedTask;
	}
}

/// <summary>总是通过的对象级规则。</summary>
public sealed class AlwaysPassRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>记录收到值的观察者。</summary>
internal sealed class ListObserver<T> : IObserver<T>
{
	public List<T> Values { get; } = [];

	public void OnCompleted()
	{
	}

	public void OnError(Exception error)
	{
	}

	public void OnNext(T value)
	{
		Values.Add(value);
	}
}
