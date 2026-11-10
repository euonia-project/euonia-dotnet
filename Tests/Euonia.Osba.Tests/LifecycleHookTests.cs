using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证保存与执行的生命周期回调在正确边界触发。
/// </summary>
public class LifecycleHookTests
{
	/// <summary>
	/// 验证保存流程按「保存前回调 → 插入工厂方法 → 保存后回调」的顺序触发，并在成功后清除脏状态。
	/// </summary>
	[Fact]
	public async Task Save_ShouldInvokeHooksAroundFactoryMethod()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<LifecycleEditable>(provider);

		target.MarkAsNew();
		target.Name = "saved";

		var result = await target.SaveAsync(false, TestContext.Current.CancellationToken);

		Assert.Same(target, result);
		Assert.Equal(["saving", "inserting", "saved"], target.Steps);
		Assert.False(target.HasChangedProperties);
		Assert.Equal(1, target.SavingCount);
		Assert.Equal(1, target.SavedCount);
	}

	/// <summary>
	/// 验证执行流程按「执行前回调 → 命令体 → 执行后回调」的顺序触发，且各回调恰好执行一次。
	/// </summary>
	[Fact]
	public async Task Execute_ShouldInvokeHooksAroundExecuteBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<LifecycleService>(provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		await factory.ExecuteAsync(target, TestContext.Current.CancellationToken);

		Assert.Equal(["executing", "executed-body", "executed"], target.Steps);
		Assert.Equal(1, target.ExecutingCount);
		Assert.Equal(1, target.ExecutedCount);
	}
}

/// <summary>
/// 记录保存回调顺序的可编辑对象。
/// </summary>
public sealed class LifecycleEditable : EditableObject<LifecycleEditable>
{
	/// <summary>
	/// 可编辑对象名称的属性元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	/// <summary>
	/// 按触发顺序记录生命周期步骤的名称。
	/// </summary>
	public List<string> Steps { get; } = [];

	/// <summary>
	/// <see cref="OnSavingAsync(CancellationToken)"/> 被调用的次数。
	/// </summary>
	public int SavingCount { get; private set; }

	/// <summary>
	/// <see cref="OnSavedAsync(object, CancellationToken)"/> 被调用的次数。
	/// </summary>
	public int SavedCount { get; private set; }

	/// <summary>
	/// 可编辑对象的名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 保存前回调：累加调用次数并记录 <c>saving</c> 步骤。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示回调过程的任务。</returns>
	protected override Task OnSavingAsync(CancellationToken cancellationToken = default)
	{
		SavingCount++;
		Steps.Add("saving");
		return Task.CompletedTask;
	}

	/// <summary>
	/// 保存后回调：累加调用次数并记录 <c>saved</c> 步骤。
	/// </summary>
	/// <param name="result">保存操作返回的结果对象。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示回调过程的任务。</returns>
	protected override Task OnSavedAsync(object result, CancellationToken cancellationToken = default)
	{
		SavedCount++;
		Steps.Add("saved");
		return Task.CompletedTask;
	}

	/// <summary>
	/// 插入工厂方法：记录 <c>inserting</c> 步骤，用于观察其在保存回调之间的位置。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示插入过程的任务。</returns>
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		Steps.Add("inserting");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 记录执行回调顺序的服务对象。
/// </summary>
public sealed class LifecycleService : ServiceObject<LifecycleService>
{
	/// <summary>
	/// 按触发顺序记录生命周期步骤的名称。
	/// </summary>
	public List<string> Steps { get; } = [];

	/// <summary>
	/// <see cref="OnExecutingAsync(CancellationToken)"/> 被调用的次数。
	/// </summary>
	public int ExecutingCount { get; private set; }

	/// <summary>
	/// <see cref="OnExecutedAsync(CancellationToken)"/> 被调用的次数。
	/// </summary>
	public int ExecutedCount { get; private set; }

	/// <summary>
	/// 执行前回调：累加调用次数并记录 <c>executing</c> 步骤。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示回调过程的任务。</returns>
	protected override Task OnExecutingAsync(CancellationToken cancellationToken = default)
	{
		ExecutingCount++;
		Steps.Add("executing");
		return Task.CompletedTask;
	}

	/// <summary>
	/// 执行后回调：累加调用次数并记录 <c>executed</c> 步骤。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示回调过程的任务。</returns>
	protected override Task OnExecutedAsync(CancellationToken cancellationToken = default)
	{
		ExecutedCount++;
		Steps.Add("executed");
		return Task.CompletedTask;
	}

	/// <summary>
	/// 命令体：记录 <c>executed-body</c> 步骤，用于观察其位于前后回调之间。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示执行过程的任务。</returns>
	[FactoryExecute]
	protected internal override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Steps.Add("executed-body");
		return Task.CompletedTask;
	}
}
