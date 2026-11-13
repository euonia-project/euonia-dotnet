using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 覆盖新增能力之外仍需保持可用的旧公共 API 形态。
/// </summary>
public class PublicApiCompatibilityTests
{
	/// <summary>
	/// 旧的表达式重载仍能注册属性，且读写结果与属性名保持正确。
	/// </summary>
	[Fact]
	public void LegacyRegisterPropertyOverload_ShouldStillWork()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<LegacyPropertyObject>(provider);

		target.Name = "legacy";

		Assert.Equal("legacy", target.Name);
		Assert.Equal(nameof(LegacyPropertyObject.Name), LegacyPropertyObject.NameProperty.Name);
	}

	/// <summary>
	/// 仅实现旧接口成员的字段数据仍能通过默认实现，把当前值作为原始值返回。
	/// </summary>
	[Fact]
	public void LegacyFieldDataImplementation_ShouldGetDefaultOriginalValue()
	{
		IFieldData field = new LegacyFieldData { Value = "current" };

		Assert.Equal("current", field.OriginalValue);
	}

	/// <summary>
	/// 仅实现旧接口成员的规则上下文通过默认实现返回空的依赖与服务。
	/// </summary>
	[Fact]
	public void LegacyRuleContextImplementation_ShouldGetDefaultServiceMembers()
	{
		IRuleContext context = new LegacyRuleContext();

		Assert.Null(context.ServiceProvider);
		Assert.Null(context.GetService<string>());
	}

	/// <summary>
	/// 旧的 <see cref="BusinessObjectFactory"/> 构造函数签名仍可创建工厂实例。
	/// </summary>
	[Fact]
	public void LegacyBusinessObjectFactoryConstructor_ShouldStillWork()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var factory = new BusinessObjectFactory(provider, new DefaultObjectActivator());

		Assert.NotNull(factory);
	}

	/// <summary>
	/// 通过依赖注入注册的自定义管道仍会在读取流程中执行一次。
	/// </summary>
	[Fact]
	public async Task RegisteredCustomPipeline_ShouldStillRun()
	{
		var pipelineRuns = 0;
		using var scope = RuleTestHarness.CreateScope(
			out var provider,
			services => services.AddScoped<IPipeline<LegacyActuatorObject, LegacyActuatorObject>>(sp =>
			{
				var pipeline = new DefaultPipelineProvider<LegacyActuatorObject, LegacyActuatorObject>(sp);
				pipeline.Use((context, next) =>
				{
					pipelineRuns++;
					return next(context);
				});
				return pipeline;
			}));

		var actuator = provider.GetRequiredService<IActuator>();
		var result = await actuator.For<LegacyActuatorObject>()
		                           .Fetch("legacy")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("legacy", result.Name);
		Assert.Equal(1, pipelineRuns);
	}
}

/// <summary>
/// 使用旧 <c>Expression&lt;Func&lt;T, object&gt;&gt;</c> 重载注册属性的对象。
/// </summary>
public sealed class LegacyPropertyObject : EditableObject<LegacyPropertyObject>
{
	private static readonly Expression<Func<LegacyPropertyObject, object>> NameExpression = x => x.Name;

	/// <summary>
	/// Name 属性的元数据，使用旧表达式重载注册。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(NameExpression);

	/// <summary>
	/// 通过旧表达式重载注册的名称属性。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}

/// <summary>
/// 只实现旧 <see cref="IFieldData"/> 成员的字段数据。
/// </summary>
public sealed class LegacyFieldData : IFieldData
{
	/// <summary>
	/// 字段名，固定返回 <c>legacy</c> 以验证旧契约。
	/// </summary>
	public string Name => "legacy";

	/// <summary>
	/// 字段的当前值。
	/// </summary>
	public object Value { get; set; } = null!;

	/// <summary>
	/// 字段是否有效，桩实现恒为 <c>true</c>。
	/// </summary>
	public bool IsValid => true;

	/// <summary>
	/// 字段是否已变更，桩实现恒为 <c>false</c>。
	/// </summary>
	public bool IsChanged => false;

	/// <summary>
	/// 字段是否已删除，桩实现恒为 <c>false</c>。
	/// </summary>
	public bool IsDeleted => false;

	/// <summary>
	/// 字段是否为新记录，桩实现恒为 <c>true</c>。
	/// </summary>
	public bool IsNew => true;

	/// <summary>
	/// 字段是否可保存，桩实现恒为 <c>false</c>。
	/// </summary>
	public bool IsSavable => false;

	/// <summary>
	/// 字段是否处于忙碌状态，桩实现恒为 <c>false</c>。
	/// </summary>
	public bool IsBusy => false;

	/// <summary>
	/// 字段自身是否处于忙碌状态，桩实现恒为 <c>false</c>。
	/// </summary>
	public bool IsSelfBusy => false;

	/// <summary>
	/// 忙碌状态变更事件，桩实现不触发任何处理。
	/// </summary>
	public event BusyChangedEventHandler BusyChanged
	{
		add { }
		remove { }
	}

	/// <summary>
	/// 将字段标记为未变更，桩实现为空操作。
	/// </summary>
	public void MarkAsUnchanged()
	{
	}

	/// <summary>
	/// 撤销字段变更，桩实现为空操作。
	/// </summary>
	public void Undo()
	{
	}
}

/// <summary>
/// 只实现旧 <see cref="IRuleContext"/> 成员的规则上下文。
/// </summary>
public sealed class LegacyRuleContext : IRuleContext
{
	/// <summary>
	/// 当前执行的规则实例，桩实现返回 <c>null</c>。
	/// </summary>
	public IRuleBase Rule => null!;

	/// <summary>
	/// 规则的目标对象，桩实现返回 <c>null</c>。
	/// </summary>
	public object Target => null!;

	/// <summary>
	/// 规则执行结果集合，桩实现返回空集合。
	/// </summary>
	public IReadOnlyList<RuleResult> Results => Array.Empty<RuleResult>();

	/// <summary>
	/// 添加错误结果，桩实现忽略该调用。
	/// </summary>
	/// <param name="description">结果描述，可为 <c>null</c>。</param>
	public void AddErrorResult(string? description)
	{
	}

	/// <summary>
	/// 添加警告结果，桩实现忽略该调用。
	/// </summary>
	/// <param name="description">结果描述，可为 <c>null</c>。</param>
	public void AddWarningResult(string? description)
	{
	}

	/// <summary>
	/// 添加信息结果，桩实现忽略该调用。
	/// </summary>
	/// <param name="description">结果描述，可为 <c>null</c>。</param>
	public void AddInformationResult(string? description)
	{
	}

	/// <summary>
	/// 添加成功结果，桩实现忽略该调用。
	/// </summary>
	public void AddSuccessResult()
	{
	}

	/// <summary>
	/// 结束规则执行，桩实现为空操作。
	/// </summary>
	public void Complete()
	{
	}
}

/// <summary>
/// 验证自定义管道仍被执行的读取对象。
/// </summary>
public sealed class LegacyActuatorObject : EditableObject<LegacyActuatorObject>
{
	/// <summary>
	/// Name 属性的元数据，由旧的重载推断属性名。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

	/// <summary>
	/// 用于验证自定义管道执行结果的名称属性。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		LoadProperty(NameProperty, id);
		AcceptChanges();
	}
}
