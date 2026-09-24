using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 规则体系测试的公共脚手架与测试用业务对象。
/// </summary>
/// <remarks>
/// <para>
/// <b>按类型共享的规则是进程级静态状态</b>（<see cref="RuleManager"/>），且 xunit 会并行执行不同的测试类。
/// 因此凡是指望 <c>AddRules()</c> 注册的类型级规则的场景，都必须使用该场景<b>专属</b>的业务对象类型；
/// 反过来，只用到实例级/操作级规则（<c>WithRule</c>、<c>AddInstanceRule</c>）的场景可以共用同一个类型，
/// 因为那些规则不进入共享集合——这本身就是本次改造要保证的性质之一。
/// </para>
/// </remarks>
internal static class RuleTestHarness
{
	/// <summary>
	/// 创建一个最小可用的服务作用域并设为当前业务上下文。
	/// </summary>
	/// <param name="provider">解析出的服务提供程序。</param>
	/// <param name="configure">可选的额外服务注册。</param>
	/// <returns>服务作用域；调用方负责释放。</returns>
	internal static IServiceScope CreateScope(out IServiceProvider provider, Action<IServiceCollection> configure = null)
	{
		var services = new ServiceCollection();
		services.AddBusinessObject();
		configure?.Invoke(services);

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		provider = scope.ServiceProvider;
		BusinessContextAccessor.SetCurrent(provider);
		return scope;
	}

	/// <summary>
	/// 创建指定类型的业务对象并接入业务上下文。
	/// </summary>
	/// <typeparam name="T">业务对象类型。</typeparam>
	/// <param name="provider">服务提供程序。</param>
	/// <returns>已接线的业务对象。</returns>
	internal static T Create<T>(IServiceProvider provider)
		where T : BusinessObject, new()
	{
		return new T { BusinessContext = provider.GetRequiredService<BusinessContext>() };
	}
}

/// <summary>
/// 断言 <c>Executed</c> 标记与调用次数的基础命令对象。
/// </summary>
/// <typeparam name="T">命令对象的具体类型。</typeparam>
public abstract class RuleTestCommand<T> : CommandObject<T>
	where T : RuleTestCommand<T>
{
	/// <summary>
	/// 获取一个值，指示命令体是否已执行。
	/// </summary>
	public bool Executed { get; private set; }

	/// <summary>
	/// 供外部读取 <c>Rules</c>（它在本框架里是 <c>protected</c>）。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	[FactoryCreate]
	private Task FactoryCreateAsync()
	{
		return Task.CompletedTask;
	}

	[FactoryExecute]
	protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = true;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 带失败对象级规则的命令对象：命令体必须被拦下。
/// </summary>
public class RuleFailCommand : RuleTestCommand<RuleFailCommand>
{
	protected override void AddRules()
	{
		Rules.AddRule<AlwaysFailObjectRule>();
	}
}

/// <summary>
/// 带通过对象级规则的命令对象：命令体正常执行。
/// </summary>
public class RulePassCommand : RuleTestCommand<RulePassCommand>
{
	protected override void AddRules()
	{
		Rules.AddRule<AlwaysPassObjectRule>();
	}
}

/// <summary>
/// 只产生警告的命令对象：警告不阻断执行。
/// </summary>
public class RuleWarnCommand : RuleTestCommand<RuleWarnCommand>
{
	protected override void AddRules()
	{
		Rules.AddRule<WarnOnlyObjectRule>();
	}
}

/// <summary>
/// 无任何类型级规则的可编辑对象，供实例级/操作级规则场景复用。
/// </summary>
public class RuleCleanEditable : EditableObject<RuleCleanEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 更新方法的调用次数。
	/// </summary>
	public int UpdateCount { get; private set; }

	/// <summary>
	/// 删除方法的调用次数。
	/// </summary>
	public int DeleteCount { get; private set; }

	/// <summary>
	/// 对象的标识。
	/// </summary>
	public string Id { get; private set; }

	/// <summary>
	/// 供测试观察的字段值。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		Id = id;
		Name = "initial";
		// 加载不算「用户已更改」，否则每个未改动的对象都会走更新路径
		AcceptChanges();
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		UpdateCount++;
		return Task.CompletedTask;
	}

	[FactoryDelete]
	protected override Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		DeleteCount++;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 带失败对象级规则的可编辑对象，供类型级规则相关场景专用。
/// </summary>
public class RuleFailEditable : EditableObject<RuleFailEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 更新方法的调用次数。
	/// </summary>
	public int UpdateCount { get; private set; }

	/// <summary>
	/// 供测试观察的字段值。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule<AlwaysFailObjectRule>();
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		Name = "initial";
		AcceptChanges();
	}

	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		UpdateCount++;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 无类型级规则的可编辑对象，用于验证绑定到属性的实例级规则会被对象级检查纳入并按属性归因。
/// </summary>
public class RulePropertyBoundEditable : EditableObject<RulePropertyBoundEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 供测试观察的字段值。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		Name = "initial";
		AcceptChanges();
	}

	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 带「报错但不给消息」规则的可编辑对象，用于验证空白描述不会被当成通过。
/// </summary>
public class RuleBlankMessageEditable : EditableObject<RuleBlankMessageEditable>
{
	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	protected override void AddRules()
	{
		Rules.AddRule<NullMessageRule>();
		Rules.AddRule<WhitespaceMessageRule>();
	}
}

/// <summary>
/// 带失败对象级规则的可编辑对象，用于验证违规集合的计数一致性。
/// </summary>
public class RuleBrokenRulesEditable : EditableObject<RuleBrokenRulesEditable>
{
	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	protected override void AddRules()
	{
		Rules.AddRule<AlwaysFailObjectRule>();
	}
}

/// <summary>
/// 带失败对象级规则的可编辑对象，用于验证删除时的规则开关。
/// </summary>
public class RuleDeleteEditable : EditableObject<RuleDeleteEditable>
{
	/// <summary>
	/// 供外部读取 <c>Rules</c>。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 删除方法的调用次数。
	/// </summary>
	public int DeleteCount { get; private set; }

	protected override void AddRules()
	{
		Rules.AddRule<AlwaysFailObjectRule>();
	}

	[FactoryFetch]
	private void Fetch(string id)
	{
		AcceptChanges();
	}

	[FactoryDelete]
	protected override Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		DeleteCount++;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 依赖注入的共享单例，用于验证规则可以从服务提供程序解析依赖。
/// </summary>
public sealed class RuleTestDependency
{
	/// <summary>
	/// 供规则引用并断言的值。
	/// </summary>
	public string Marker => "from-di";
}

/// <summary>
/// 需要从容器解析依赖的失败规则。
/// </summary>
public sealed class DependentFailRule : RuleBase
{
	private readonly RuleTestDependency _dependency;

	/// <summary>
	/// 初始化 <see cref="DependentFailRule"/>。
	/// </summary>
	/// <param name="dependency">注入的依赖。</param>
	public DependentFailRule(RuleTestDependency dependency)
	{
		_dependency = dependency;
	}

	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult($"dependency={_dependency?.Marker ?? "missing"}");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 恒失败的对象级规则。
/// </summary>
public sealed class AlwaysFailObjectRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("对象级规则失败");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 恒通过的对象级规则。
/// </summary>
public sealed class AlwaysPassObjectRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddSuccessResult();
		return Task.CompletedTask;
	}
}

/// <summary>
/// 只产生警告的对象级规则。
/// </summary>
/// <remarks>
/// 复用 <see cref="OsbaFeatureTests"/> 中已有的同名规则（规则本身无状态，可安全共享）。
/// </remarks>
public sealed class WarnOnlyObjectRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddWarningResult("只是警告");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 记录被检查时目标对象当前 <c>Name</c> 值的失败规则，用于验证规则看到的是 Handle 之后的状态。
/// </summary>
public sealed class NameSnapshotFailRule : RuleBase
{
	/// <summary>
	/// 最近一次执行时观察到的名称。
	/// </summary>
	public string Observed { get; private set; }

	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		if (context.Target is RuleCleanEditable target)
		{
			Observed = target.Name;
		}

		context.AddErrorResult("名称快照");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 绑定到属性的恒失败规则（属性级）。
/// </summary>
public sealed class PropertyBoundFailRule : RuleBase
{
	/// <summary>
	/// 初始化 <see cref="PropertyBoundFailRule"/>。
	/// </summary>
	/// <param name="property">受规则影响的属性。</param>
	public PropertyBoundFailRule(IPropertyInfo property)
		: base(property)
	{
	}

	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("属性级规则失败");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 用 <c>null</c> 描述报告错误的规则。
/// </summary>
public sealed class NullMessageRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult(null);
		return Task.CompletedTask;
	}
}

/// <summary>
/// 用空白描述报告错误的规则。
/// </summary>
public sealed class WhitespaceMessageRule : RuleBase
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("   ");
		return Task.CompletedTask;
	}
}
