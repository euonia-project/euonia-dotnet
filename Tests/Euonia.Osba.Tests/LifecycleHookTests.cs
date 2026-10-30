using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证保存与执行的生命周期回调在正确边界触发。
/// </summary>
public class LifecycleHookTests
{
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
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	public List<string> Steps { get; } = [];

	public int SavingCount { get; private set; }

	public int SavedCount { get; private set; }

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override Task OnSavingAsync(CancellationToken cancellationToken = default)
	{
		SavingCount++;
		Steps.Add("saving");
		return Task.CompletedTask;
	}

	protected override Task OnSavedAsync(object result, CancellationToken cancellationToken = default)
	{
		SavedCount++;
		Steps.Add("saved");
		return Task.CompletedTask;
	}

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
	public List<string> Steps { get; } = [];

	public int ExecutingCount { get; private set; }

	public int ExecutedCount { get; private set; }

	protected override Task OnExecutingAsync(CancellationToken cancellationToken = default)
	{
		ExecutingCount++;
		Steps.Add("executing");
		return Task.CompletedTask;
	}

	protected override Task OnExecutedAsync(CancellationToken cancellationToken = default)
	{
		ExecutedCount++;
		Steps.Add("executed");
		return Task.CompletedTask;
	}

	[FactoryExecute]
	protected internal override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Steps.Add("executed-body");
		return Task.CompletedTask;
	}
}
