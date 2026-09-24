using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
// DataAnnotations 下也有 ValidationException，这里要的是框架自己的那个
using ValidationException = Nerosoft.Euonia.Validation.ValidationException;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证属性级规则的触发时机：<b>属性变更时</b>检查，而不是保存时。
/// </summary>
/// <remarks>
/// <para>
/// 这是属性级规则的设计原则，也决定了保存语义：只有真正被修改过的属性会被校验，
/// 未修改（乃至未装载）的属性不参与校验，因此不会因为「读出来是默认值」而被凭空拦下。
/// 与之对应，持久化只应回写 <c>ChangedProperties</c>。
/// </para>
/// <para>
/// 校验产生的违规按属性归因落进 <c>BrokenRules</c>，所以保存时仍会经 <c>IsValid</c> 生效。
/// </para>
/// </remarks>
public class PropertyRuleTriggerTests
{
	#region 变更即检查

	[Fact]
	public void SetProperty_WithFailingRule_ShouldMarkInvalidImmediately()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		Assert.True(editable.IsValid);          // 尚未修改：未校验

		editable.Name = "   ";                  // 空白值：违反 Required

		Assert.False(editable.IsValid);
		var broken = Assert.Single(editable.GetBrokenRules());
		Assert.Equal(RequiredNameObject.NameProperty.Name, broken.Property);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void SetProperty_FixingTheValue_ShouldClearTheError()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.Name = "   ";
		Assert.False(editable.IsValid);

		editable.Name = "repo-a1";

		Assert.True(editable.IsValid);
		Assert.Empty(editable.GetBrokenRules());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void SetProperty_ToCurrentValue_ShouldBeANoOp()
	{
		// 赋同值不是变更：既不标脏、也不触发检查——与「未修改的属性不回写数据库」一致
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.Name = string.Empty;           // 与默认值相同

		Assert.True(editable.IsValid);
		Assert.Empty(editable.ChangedProperties);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void SetProperty_WithMultipleRules_ShouldRunAll()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<MultiRuleNameObject>(provider);

		editable.Name = "   ";

		// 特性转换出的规则与内置 Required 规则都应命中
		Assert.Equal(2, editable.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void SetProperty_WithInstanceRuleBoundToProperty_ShouldAlsoTrigger()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RulePropertyBoundEditable>(provider);
		editable.PublicRules.AddInstanceRule(new PropertyBoundFailRule(RulePropertyBoundEditable.NameProperty));
		editable.GetBrokenRules().Clear();

		editable.Name = "changed";

		Assert.False(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void LoadProperty_ShouldNotTriggerRuleCheck()
	{
		// 装载不是用户修改：不应触发检查、不应标脏——否则装载一条历史数据就会报出校验错误
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.LoadProperty(RequiredNameObject.NameProperty, "   ");

		Assert.True(editable.IsValid);
		Assert.Empty(editable.GetBrokenRules());
		Assert.Empty(editable.ChangedProperties);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 保存语义

	[Fact]
	public async Task SaveAsync_ShouldBeBlockedByPropertyRuleError()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.Name = "   ";             // 变更即报错
		editable.MarkAsNew();

		// 属性级规则的违规已按属性归因落进违规集合，保存时经 IsValid 生效
		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(
			exception.Errors,
			error => error.PropertyName == RequiredNameObject.NameProperty.Name);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ShouldNotValidatePropertiesThatWereNeverModified()
	{
		// 核心保证：未修改的属性不参与校验——否则「只装载了部分字段」的对象
		// 会因为未装载字段读出默认值（空字符串）而被 [Required] 凭空拦下。
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.MarkAsNew();                       // 触发保存流程，但没有任何属性被修改

		var exception = await Record.ExceptionAsync(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Null(exception);
		Assert.Empty(editable.ChangedProperties);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 变更通知不受影响

	[Fact]
	public void PropertyChanged_ShouldFire_ForPropertyWithoutRules()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<NotificationObject>(provider);

		var names = new List<string>();
		editable.PropertyChanged += (_, args) => names.Add(args.PropertyName);

		editable.Plain = "value";

		Assert.Contains(NotificationObject.PlainProperty.Name, names);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void PropertyChanged_ShouldFire_WhenRulesExist()
	{
		// 走规则分支不能丢掉变更通知：绑定依赖它
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<NotificationObject>(provider);

		var names = new List<string>();
		editable.PropertyChanged += (_, args) => names.Add(args.PropertyName);

		editable.Checked = "   ";

		Assert.Contains(NotificationObject.CheckedProperty.Name, names);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 可退出

	[Fact]
	public void CheckRuleOnPropertyChanged_OptOut_ShouldSkipRuleCheckOnSet()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<OptOutObject>(provider);

		editable.Name = "   ";

		// 覆写为 false 后不在 setter 上检查
		Assert.True(editable.IsValid);
		Assert.Empty(editable.GetBrokenRules());

		// 规则本身仍在，只是改由调用方显式驱动（例如在保存前自行 CheckPropertyRules）
		editable.CheckNameRules();

		Assert.False(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region DataAnnotations 桥接

	[Fact]
	public async Task DataAnnotationRule_Message_ShouldUseFriendlyName()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RequiredNameObject>(provider);

		editable.Name = "   ";

		// {0} 应填字段的友好名，而不是业务对象的类型名
		var broken = editable.GetBrokenRules().First(rule => rule.Description.Contains("不能为空"));
		Assert.Equal("仓库名 不能为空。", broken.Description);

		await Task.CompletedTask;
		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 推迟到保存时检查

	[Fact]
	public async Task SaveAsync_OnOptOutType_ShouldCheckChangedPropertyRules()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<DeferredRuleObject>(provider);

		editable.Name = "   ";                  // setter 上不检查（该类型已推迟）

		Assert.True(editable.IsValid);
		Assert.Empty(editable.GetBrokenRules());

		editable.MarkAsNew();

		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(
			exception.Errors,
			error => error.PropertyName == DeferredRuleObject.NameProperty.Name);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_OnOptOutType_ShouldNotCheckUnchangedProperties()
	{
		// 推迟检查不等于「保存时全量校验」：仍然只校验本次变更过的属性
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<DeferredRuleObject>(provider);

		editable.MarkAsNew();

		var exception = await Record.ExceptionAsync(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Null(exception);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_OnOptOutType_ShouldPass_WhenTheChangedValueIsValid()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<DeferredRuleObject>(provider);

		editable.Name = "repo-a1";
		editable.MarkAsNew();

		var exception = await Record.ExceptionAsync(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Null(exception);
		Assert.True(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ShouldReportPropertyErrors_BeforeObjectErrors()
	{
		// 顺序：先属性级（字段级），再对象级（整体）——对象级规则可能依赖一个或多个属性值。
		// 违规按这个顺序落进违规集合，验证异常里的错误列表也就天然是「先字段、后整体」。
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<DeferredOrderObject>(provider);

		editable.Name = "   ";
		editable.MarkAsNew();

		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		var errors = exception.Errors.ToList();
		Assert.Equal(2, errors.Count);
		Assert.Equal(DeferredOrderObject.NameProperty.Name, errors[0].PropertyName);
		Assert.Null(errors[1].PropertyName);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_OnOptOutType_ShouldSupportAsyncPropertyRules()
	{
		// 推迟到保存时才跑的好处之一：走的是异步流程，属性级规则可以放心 await（I/O 校验），
		// 不必像 setter 上那样同步阻塞
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<DeferredAsyncRuleObject>(provider);

		editable.Name = "   ";
		editable.MarkAsNew();

		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(
			exception.Errors,
			error => error.PropertyName == DeferredAsyncRuleObject.NameProperty.Name);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 同步上下文下的死锁防护

	[Fact]
	public void SetProperty_WithAsyncRule_ShouldNotDeadlock_WhenSynchronizationContextIsPresent()
	{
		// 模拟 UI / Blazor Server：装了「续体要回到本线程」的同步上下文。
		// 若规则在调用线程上启动后再阻塞等待，规则内部 await 的续体会被 Post 回这条已被阻塞的线程
		// ——互等死锁。实现必须在启动规则之前就换到线程池。
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<AsyncRuleObject>(provider);

		var completed = new ManualResetEventSlim(false);
		Exception failure = null;

		var thread = new Thread(() =>
		{
			try
			{
				SynchronizationContext.SetSynchronizationContext(new NonPumpingContext());
				editable.Name = "   ";
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				completed.Set();
			}
		})
		{
			IsBackground = true
		};

		thread.Start();

		Assert.True(
			completed.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
			"属性 setter 在同步上下文下死锁了");
		Assert.Null(failure);
		Assert.False(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 只收 Post 却永不泵送的同步上下文——最坏情况，用于暴露「阻塞自等」的死锁。
	/// </summary>
	private sealed class NonPumpingContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback d, object state)
		{
			// 排入队列但永不执行：等待方将永远等不到续体
		}
	}

	#endregion

	#region ExecuteOnState 空声明

	[Fact]
	public async Task ExecuteOnState_WithNoStates_ShouldNotDisableTheRule()
	{
		// [ExecuteOnState] 没填参数视为「不限制」，而不是「全都不匹配」——
		// 后者会让一个漏填的特性静默把规则彻底关掉
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<EmptyStateObject>(provider);

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.False(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	#endregion
}

/// <summary>
/// 属性级规则的最简场景：Name 有友好名「仓库名」，带 <see cref="RequiredAttribute"/>。
/// </summary>
public class RequiredNameObject : EditableObject<RequiredNameObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	[Required(ErrorMessage = "{0} 不能为空。")]
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 同一属性上同时有特性规则与内置规则。
/// </summary>
public class MultiRuleNameObject : EditableObject<MultiRuleNameObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	[Required(ErrorMessage = "{0} 不能为空。")]
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "{0} 是必填的。"));
	}
}

/// <summary>
/// 一个属性无规则、另一个有规则，用于验证变更通知在两条分支上都照常发出。
/// </summary>
public class NotificationObject : EditableObject<NotificationObject>
{
	public static readonly PropertyInfo<string> PlainProperty = RegisterProperty<string>(p => p.Plain);
	public static readonly PropertyInfo<string> CheckedProperty = RegisterProperty<string>(p => p.Checked);

	public string Plain
	{
		get => GetProperty(PlainProperty);
		set => SetProperty(PlainProperty, value);
	}

	public string Checked
	{
		get => GetProperty(CheckedProperty);
		set => SetProperty(CheckedProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(CheckedProperty, "{0} 是必填的。"));
	}
}

/// <summary>
/// 覆写 <c>CheckRuleOnPropertyChanged</c> 退回到「只在显式检查/保存时校验」。
/// </summary>
public class OptOutObject : EditableObject<OptOutObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override bool CheckRuleOnPropertyChanged => false;

	/// <summary>
	/// 显式驱动 Name 的属性级规则检查。
	/// </summary>
	public void CheckNameRules()
	{
		CheckPropertyRules(NameProperty);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "{0} 是必填的。"));
	}
}

/// <summary>
/// 属性级规则内部会真正 await（异步校验），用于验证同步上下文下的死锁防护。
/// </summary>
public class AsyncRuleObject : EditableObject<AsyncRuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule<AsyncRuleObject>(
			NameProperty,
			async @object =>
			{
				// 真 await：续体是否要被排回调用方上下文，是死锁能否发生的关键
				await Task.Yield();
				return !string.IsNullOrWhiteSpace(@object.Name);
			},
			"{0} 是必填的。");
	}
}

/// <summary>
/// 把属性级检查推迟到保存的类型：只有 Name 的属性级规则，无对象级规则。
/// </summary>
public class DeferredRuleObject : EditableObject<DeferredRuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override bool CheckRuleOnPropertyChanged => false;

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "{0} 是必填的。"));
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 推迟检查 + 同时有属性级与对象级规则，用于断言两类规则的执行/记录顺序。
/// </summary>
public class DeferredOrderObject : EditableObject<DeferredOrderObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override bool CheckRuleOnPropertyChanged => false;

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "{0} 是必填的。"));
		Rules.AddRule<AlwaysFailObjectRule>();
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 推迟检查 + 异步属性级规则（内部真正 await）。
/// </summary>
public class DeferredAsyncRuleObject : EditableObject<DeferredAsyncRuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>("Name", "仓库名");

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override bool CheckRuleOnPropertyChanged => false;

	protected override void AddRules()
	{
		Rules.AddRule<DeferredAsyncRuleObject>(
			NameProperty,
			async @object =>
			{
				await Task.Delay(1);
				return !string.IsNullOrWhiteSpace(@object.Name);
			},
			"{0} 是必填的。");
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 对象级规则带空的 <c>[ExecuteOnState]</c> 声明。
/// </summary>
public class EmptyStateObject : EditableObject<EmptyStateObject>
{
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	protected override void AddRules()
	{
		Rules.AddRule<EmptyStateRule>();
	}
}

/// <summary>
/// 未填状态的规则：应视为不限制状态。
/// </summary>
[ExecuteOnState]
public sealed class EmptyStateRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("空状态声明的规则已执行");
		return Task.CompletedTask;
	}
}
