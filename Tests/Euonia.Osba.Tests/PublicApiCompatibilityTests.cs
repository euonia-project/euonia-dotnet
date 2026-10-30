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
	[Fact]
	public void LegacyRegisterPropertyOverload_ShouldStillWork()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<LegacyPropertyObject>(provider);

		target.Name = "legacy";

		Assert.Equal("legacy", target.Name);
		Assert.Equal(nameof(LegacyPropertyObject.Name), LegacyPropertyObject.NameProperty.Name);
	}

	[Fact]
	public void LegacyFieldDataImplementation_ShouldGetDefaultOriginalValue()
	{
		IFieldData field = new LegacyFieldData { Value = "current" };

		Assert.Equal("current", field.OriginalValue);
	}

	[Fact]
	public void LegacyRuleContextImplementation_ShouldGetDefaultServiceMembers()
	{
		IRuleContext context = new LegacyRuleContext();

		Assert.Null(context.ServiceProvider);
		Assert.Null(context.GetService<string>());
	}

	[Fact]
	public void LegacyBusinessObjectFactoryConstructor_ShouldStillWork()
	{
		using var provider = new ServiceCollection().BuildServiceProvider();
		var factory = new BusinessObjectFactory(provider, new DefaultObjectActivator());

		Assert.NotNull(factory);
	}

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

	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(NameExpression);

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
	public string Name => "legacy";

	public object Value { get; set; }

	public bool IsValid => true;

	public bool IsChanged => false;

	public bool IsDeleted => false;

	public bool IsNew => true;

	public bool IsSavable => false;

	public bool IsBusy => false;

	public bool IsSelfBusy => false;

	public event BusyChangedEventHandler BusyChanged
	{
		add { }
		remove { }
	}

	public void MarkAsUnchanged()
	{
	}

	public void Undo()
	{
	}
}

/// <summary>
/// 只实现旧 <see cref="IRuleContext"/> 成员的规则上下文。
/// </summary>
public sealed class LegacyRuleContext : IRuleContext
{
	public IRuleBase Rule => null;

	public object Target => null;

	public IReadOnlyList<RuleResult> Results => Array.Empty<RuleResult>();

	public void AddErrorResult(string description)
	{
	}

	public void AddWarningResult(string description)
	{
	}

	public void AddInformationResult(string description)
	{
	}

	public void AddSuccessResult()
	{
	}

	public void Complete()
	{
	}
}

/// <summary>
/// 验证自定义管道仍被执行的读取对象。
/// </summary>
public sealed class LegacyActuatorObject : EditableObject<LegacyActuatorObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

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
