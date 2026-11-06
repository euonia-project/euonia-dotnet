using System.ComponentModel;
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
	/// <summary>
	/// 返回 <see cref="ValueTask"/> 的工厂方法必须被等待：保存完成后 <c>Persisted</c> 应为 1。
	/// </summary>
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

	/// <summary>
	/// 缺少插入工厂方法时，保存应抛出 <see cref="NotSupportedException"/>，而不是静默地什么都不做。
	/// </summary>
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

	/// <summary>
	/// 工厂方法抛出的业务异常应原样传播，不应被包装为 <c>TargetInvocationException</c>。
	/// </summary>
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

	/// <summary>
	/// 缺少执行工厂方法时，命令执行应抛出 <see cref="NotSupportedException"/>，而不是静默地什么都不做。
	/// </summary>
	[Fact]
	public async Task Missing_Execute_Factory_Method_Should_Fail_Instead_Of_Silently_Doing_Nothing()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => factory.ExecuteAsync(new NoExecuteCommand(), TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 由 <c>ref</c> 字段承载的属性值应能被规则与 DataAnnotations 正确读取，合法值应判定为有效。
	/// </summary>
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

	/// <summary>
	/// 非泛型 <c>SetProperty</c> 写入与当前值相同的值时，不应把属性标记为已更改。
	/// </summary>
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

	/// <summary>
	/// 只读对象在工厂装载作用域之外调用 <c>LoadProperty</c> 应抛出 <see cref="SecurityException"/>。
	/// </summary>
	[Fact]
	public async Task ReadOnly_Object_Should_Reject_LoadProperty_Outside_Factory_Load_Scope()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new LoadableReadOnlyObject { BusinessContext = context };

		Assert.Throws<SecurityException>(() =>
			((IBusinessObject)target).LoadProperty(LoadableReadOnlyObject.NameProperty, "external"));
	}

	/// <summary>
	/// 只读对象在工厂装载作用域内允许调用 <c>LoadProperty</c>，装载后的值应可读。
	/// </summary>
	[Fact]
	public async Task ReadOnly_Object_Should_Allow_LoadProperty_Inside_Factory_Load_Scope()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var loaded = await factory.FetchAsync<LoadableReadOnlyObject>("loaded");

		Assert.Equal("loaded", loaded.Name);
	}

	/// <summary>
	/// 嵌套的操作规则作用域中先结束内层时，外层规则必须仍然有效并拦下校验。
	/// </summary>
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

	/// <summary>
	/// 服务对象（命令）不应带有编辑状态或变更追踪成员，也不实现相关的业务对象接口。
	/// </summary>
	[Fact]
	public void ServiceObject_Should_Not_Carry_Edit_State_Or_Change_Tracking()
	{
		// ServiceObject 直接继承 ObjectBase<T>，不进入 BusinessObject<T>（变更追踪）层级。
		Assert.Equal(typeof(ServiceObject<TrackingCommand>), typeof(TrackingCommand).BaseType);
		Assert.Equal(typeof(ObjectBase<TrackingCommand>), typeof(ServiceObject<TrackingCommand>).BaseType);

		var command = new TrackingCommand { Name = "changed" };

		// 命令对象没有变更追踪成员，而不是“有一个恒为空的列表”。
		Assert.Null(typeof(TrackingCommand).GetProperty("HasChangedProperties"));
		Assert.Null(typeof(TrackingCommand).GetProperty("ChangedProperties"));
		Assert.True((object)command is IServiceObject);
		Assert.True((object)command is IOsbaObject);
		Assert.False((object)command is IBusinessObject);
		Assert.False((object)command is INotifyPropertyChanged);
		Assert.False((object)command is IEditableObject);
	}

	/// <summary>
	/// 验证状态应依次跟踪未验证、有效与无效三种状态，并在值变化后回到未验证。
	/// </summary>
	[Fact]
	public async Task ValidationState_Should_Track_NotValidated_Valid_Invalid()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var context = provider.GetRequiredService<BusinessContext>();

		var target = new NonGenericSetObject { BusinessContext = context };
		Assert.Equal(ValidationState.NotValidated, target.ValidationState);

		Assert.True(await target.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal(ValidationState.Valid, target.ValidationState);

		target.Name = "changed";
		Assert.Equal(ValidationState.NotValidated, target.ValidationState);

		var invalid = new RefBackedRuleObject { BusinessContext = context };
		invalid.Name = "abc";

		Assert.False(invalid.IsValid);
		Assert.Equal(ValidationState.Invalid, invalid.ValidationState);
	}

	/// <summary>
	/// 规则注册表应按容器隔离，且不与进程级的 <see cref="RuleManager"/> 共享同一实例。
	/// </summary>
	[Fact]
	public void RuleRegistry_Should_Be_Isolated_Per_Container()
	{
		var servicesA = new ServiceCollection();
		servicesA.AddBusinessObject();
		using var providerA = servicesA.BuildServiceProvider();

		var servicesB = new ServiceCollection();
		servicesB.AddBusinessObject();
		using var providerB = servicesB.BuildServiceProvider();

		var registryA = providerA.GetRequiredService<IRuleRegistry>();
		var registryB = providerB.GetRequiredService<IRuleRegistry>();

		Assert.NotSame(registryA.GetRules(typeof(NonGenericSetObject)), registryB.GetRules(typeof(NonGenericSetObject)));
		Assert.NotSame(registryA.GetRules(typeof(NonGenericSetObject)), RuleManager.GetRules(typeof(NonGenericSetObject)));
	}

	/// <summary>
	/// <c>AddBusinessObject</c> 应返回原服务集合并使传入的选项生效。
	/// </summary>
	[Fact]
	public void AddBusinessObject_Should_Return_ServiceCollection_And_Register_Options()
	{
		var services = new ServiceCollection();

		var returned = services.AddBusinessObject(options => options.StrictFactoryMethods = false);

		Assert.Same(services, returned);
		using var provider = services.BuildServiceProvider();
		Assert.False(provider.GetRequiredService<OsbaOptions>().StrictFactoryMethods);
	}

	/// <summary>
	/// 工厂创建对象时应使用构造函数注入，依赖应被正确赋值。
	/// </summary>
	[Fact]
	public async Task Factory_Should_Use_Constructor_Injection()
	{
		var services = new ServiceCollection();
		services.AddSingleton<SampleDependency>();
		services.AddBusinessObject();
		using var provider = services.BuildServiceProvider();

		var factory = provider.GetRequiredService<IObjectFactory>();
		var created = await factory.CreateAsync<ConstructorInjectedObject>();

		Assert.NotNull(created.Dependency);
	}

	/// <summary>
	/// 启动期校验应报告无法解析的 <c>[Inject]</c> 属性，诊断码为 <c>OSBA-I001</c>。
	/// </summary>
	[Fact]
	public void StartupValidator_Should_Report_Unresolvable_Inject_Property()
	{
		var diagnostics = OsbaStartupValidator.Validate([typeof(BadInjectObject).Assembly]);

		Assert.Contains(diagnostics, diagnostic =>
			diagnostic.Code == "OSBA-I001" && diagnostic.Type == typeof(BadInjectObject));
	}

	/// <summary>
	/// 启动期校验应报告返回 <c>Task&lt;IAsyncEnumerable&lt;T&gt;&gt;</c> 的工厂方法，诊断码为 <c>OSBA-F002</c>。
	/// </summary>
	[Fact]
	public void StartupValidator_Should_Report_Awaited_AsyncEnumerable_Factory()
	{
		var diagnostics = OsbaStartupValidator.Validate([typeof(WrappedAsyncEnumerableObject).Assembly]);

		Assert.Contains(diagnostics, diagnostic =>
			diagnostic.Code == "OSBA-F002" && diagnostic.Type == typeof(WrappedAsyncEnumerableObject));
	}

	/// <summary>
	/// <c>ObservableValue</c> 订阅时应先发布当前值，随后发布订阅期间的变更；取消订阅后不再发布。
	/// </summary>
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

	/// <summary>
	/// <c>LoadProperty</c> 应写入属性值并将对应字段标记为未更改。
	/// </summary>
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

	/// <summary>
	/// 缩小历史深度后应保留最近的历史，撤销顺序仍从最新值向前回退。
	/// </summary>
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

	/// <summary>
	/// 模型规则应看到 <c>ref</c> 字段写入后的实际值，并据此报告格式错误而非必填错误。
	/// </summary>
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
	/// <summary>
	/// 插入工厂方法的累计执行次数。
	/// </summary>
	public int Persisted { get; private set; }

	/// <summary>
	/// 插入工厂方法：先异步让出，再累加 <see cref="Persisted"/>，用于验证 <c>ValueTask</c> 被等待。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的值任务。</returns>
	[FactoryInsert]
	protected async ValueTask PersistAsync(CancellationToken cancellationToken = default)
	{
		// 强制产生异步续体：旧实现不会等待它。
		await Task.Yield();
		Persisted++;
	}

	/// <summary>
	/// 更新工厂方法：本测试对象不落库，直接完成。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
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
	/// <summary>
	/// 插入工厂方法：抛出业务异常，用于验证异常不被包装、原样传播。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>正常情况下表示异步操作的任务。</returns>
	/// <exception cref="InvalidOperationException">始终抛出，模拟业务失败。</exception>
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		throw new InvalidOperationException("business failure");
	}

	/// <summary>
	/// 更新工厂方法：本测试对象不落库，直接完成。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>没有实现 Execute 的命令：执行必须失败，而不是静默 no-op。</summary>
public class NoExecuteCommand : ServiceObject<NoExecuteCommand>
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
	/// <summary>
	/// <see cref="Name"/> 属性的元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	private string _name = string.Empty;

	/// <summary>
	/// 由 <c>ref</c> 字段承载的属性值，带必填校验与数字格式规则。
	/// </summary>
	[Required(ErrorMessage = "必填")]
	public string Name
	{
		get => GetProperty(NameProperty, _name);
		set => SetProperty(NameProperty, ref _name, value);
	}

	/// <summary>
	/// 注册数字格式规则：值必须匹配 <c>^[0-9]+$</c>。
	/// </summary>
	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Regular(NameProperty, "^[0-9]+$", "只能是数字"));
	}
}

/// <summary>非泛型 SetProperty 探针。</summary>
public class NonGenericSetObject : EditableObject<NonGenericSetObject>
{
	/// <summary>
	/// <see cref="Name"/> 属性的元数据，供非泛型属性读写使用。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 用于验证非泛型 <c>SetProperty</c> 行为的属性值。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}

/// <summary>只在工厂装载作用域内可填充的只读对象。</summary>
public class LoadableReadOnlyObject : ReadOnlyObject<LoadableReadOnlyObject>
{
	/// <summary>
	/// <see cref="Name"/> 属性的元数据，仅在工厂装载作用域内可写。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 只读属性值，由工厂装载方法写入。
	/// </summary>
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
public sealed class AlwaysFailRule : ObjectRuleBase
{
	/// <summary>
	/// 执行规则：始终报告一条错误结果。
	/// </summary>
	/// <param name="context">规则上下文，提供目标对象与结果收集器。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步执行的任务。</returns>
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("always fail");
		return Task.CompletedTask;
	}
}

/// <summary>总是通过的对象级规则。</summary>
public sealed class AlwaysPassRule : ObjectRuleBase
{
	/// <summary>
	/// 执行规则：始终通过，不添加任何结果。
	/// </summary>
	/// <param name="context">规则上下文，提供目标对象与结果收集器。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步执行的任务。</returns>
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>命令对象不跟踪属性变化。</summary>
public sealed class TrackingCommand : ServiceObject<TrackingCommand>
{
	/// <summary>
	/// 命令对象的名称，用于验证命令不跟踪属性变化。
	/// </summary>
	public string Name { get; set; } = null!;
}

/// <summary>构造注入依赖。</summary>
public sealed class SampleDependency
{
}

/// <summary>通过构造函数获取依赖的业务对象。</summary>
public sealed class ConstructorInjectedObject : EditableObject<ConstructorInjectedObject>
{
	/// <summary>
	/// 初始化对象并保存注入的依赖。
	/// </summary>
	/// <param name="dependency">通过构造函数注入的依赖。</param>
	public ConstructorInjectedObject(SampleDependency dependency)
	{
		Dependency = dependency;
	}

	/// <summary>
	/// 通过构造函数注入的依赖实例。
	/// </summary>
	public SampleDependency Dependency { get; }
}

/// <summary>用于启动期校验的非法 [Inject] 属性。</summary>
public class BadInjectObject : BusinessObject<BadInjectObject>
{
	/// <summary>
	/// 无法从容器解析的 <c>[Inject]</c> 属性，用于触发启动期校验诊断。
	/// </summary>
	[Inject]
	public int Number { get; set; }
}

/// <summary>工厂方法返回 Task&lt;IAsyncEnumerable&lt;T&gt;&gt; 的对象，校验器应报 OSBA-F002。</summary>
public class WrappedAsyncEnumerableObject : BusinessObject<WrappedAsyncEnumerableObject>
{
	/// <summary>
	/// 工厂方法：返回包装在 <c>Task&lt;IAsyncEnumerable&lt;T&gt;&gt;</c> 中的异步可枚举对象，用于触发 <c>OSBA-F002</c> 诊断。
	/// </summary>
	/// <param name="id">要装载的对象标识。</param>
	/// <returns>包装在任务中的异步可枚举对象。</returns>
	[FactoryFetch]
	public Task<System.Collections.Generic.IAsyncEnumerable<WrappedAsyncEnumerableObject>> FetchAsync(string id) =>
		Task.FromResult(FetchCore());

	private static System.Collections.Generic.IAsyncEnumerable<WrappedAsyncEnumerableObject> FetchCore()
	{
		return System.Linq.AsyncEnumerable.Empty<WrappedAsyncEnumerableObject>();
	}
}

/// <summary>记录收到值的观察者。</summary>
internal sealed class ListObserver<T> : IObserver<T>
{
	/// <summary>
	/// 获取观察者收到的值，按到达顺序记录。
	/// </summary>
	public List<T> Values { get; } = [];

	/// <inheritdoc />
	public void OnCompleted()
	{
	}

	/// <inheritdoc />
	public void OnError(Exception error)
	{
	}

	/// <inheritdoc />
	public void OnNext(T value)
	{
		Values.Add(value);
	}
}
