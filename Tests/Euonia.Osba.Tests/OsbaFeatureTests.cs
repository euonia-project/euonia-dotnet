using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证本次对 Euonia.Osba 的修复与新增功能：
/// 规则完成事件、保存后状态复位、MarkAsClean、ObservableList 构造器/AddRange、GetOrAdd、
/// 同步 lambda 规则重载、可选参数工厂方法匹配。
/// </summary>
public class OsbaFeatureTests
{
	#region Rules

	[Fact]
	public async Task CheckObjectRulesAsync_ShouldRaiseValidationComplete_Once()
	{
		using var scope = BeginRuleScope<RuleObject>(out var obj);

		var count = 0;
		obj.ValidationComplete += (_, _) => count++;

		await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.Equal(1, count);
		Assert.True(obj.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CheckObjectRules_ShouldRaiseValidationComplete_OnSyncPath()
	{
		using var scope = BeginRuleScope<RuleObject>(out var obj);

		var count = 0;
		obj.ValidationComplete += (_, _) => count++;

		_ = obj.PublicRules.CheckObjectRules(true);

		Assert.Equal(1, count);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void SyncLambdaRule_ShouldEvaluateAndPopulateBrokenRules()
	{
		using var scope = BeginRuleScope<SyncRuleObject>(out var obj);

		// 未赋值时规则失败，对象无效
		obj.PublicRules.CheckRules(SyncRuleObject.NameProperty);
		Assert.False(obj.IsValid);

		// 修复 valid 后再次检查应清除违规，恢复有效状态
		obj.Name = "valid";
		obj.PublicRules.CheckRules(SyncRuleObject.NameProperty);
		Assert.True(obj.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task CheckObjectRulesAsync_ShouldNotDeadlock_WaitingForCompletion()
	{
		// 验证对象级检查会正常完成并引发完成事件（修复前 HasRunningRules 始终为 true，永不触发）
		using var scope = BeginRuleScope<RuleObject>(out var obj);

		var completed = false;
		obj.ValidationComplete += (_, _) => completed = true;

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(completed);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Save / MarkAsClean

	[Fact]
	public async Task SaveAsync_Success_ShouldMarkClean_AndReturnSameInstance()
	{
		using var scope = CreateScope(out var provider);
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		SaveEditableObject.Reset();

		var obj = new SaveEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();
		obj.Name = "new";

		object saved = null;
		obj.Saved += (_, e) => saved = e.NewObject;

		var result = await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Same(obj, result);
		Assert.Same(obj, saved);
		Assert.False(obj.IsBusy);
		Assert.Equal(ObjectEditState.None, obj.State);
		Assert.False(obj.IsChanged);
		Assert.False(obj.HasChangedProperties);
		Assert.False(obj.IsSavable);

		// 复位后再次编辑可继续保存并走更新路径
		obj.Name = "again";
		Assert.True(obj.HasChangedProperties);
		Assert.True(obj.IsSavable);

		var updated = await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.False(updated.IsBusy);
		Assert.Equal(ObjectEditState.None, updated.State);
		Assert.False(updated.HasChangedProperties);
		Assert.True(SaveEditableObject.UpdateCalled);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveDeleted_ShouldNotResetState()
	{
		using var scope = CreateScope(out var provider);
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		SaveEditableObject.Reset();

		var obj = new SaveEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();
		obj.MarkAsDeleted();

		await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		// 已删除对象的保存不应把状态复位为干净
		Assert.Equal(ObjectEditState.Deleted, obj.State);
		Assert.True(SaveEditableObject.DeleteCalled);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void MarkAsClean_ShouldResetAllDirtySignals()
	{
		var obj = new SyncRuleObject();
		obj.Name = "dirty";

		Assert.True(obj.HasChangedProperties);

		obj.MarkAsClean();

		Assert.False(obj.HasChangedProperties);
		Assert.False(obj.IsChanged);
		Assert.Equal(ObjectEditState.None, obj.State);
		Assert.False(obj.IsSavable);
	}

	[Fact]
	public void AcceptChanges_ShouldCommitChangesAndResetState()
	{
		var obj = new SyncRuleObject();
		obj.Name = "dirty";
		obj.MarkAsChanged();

		Assert.True(obj.HasChangedProperties);
		Assert.Equal(ObjectEditState.Changed, obj.State);

		obj.AcceptChanges();

		// 语义与 DataRow.AcceptChanges 一致：提交当前值并清空变更跟踪，状态复位
		Assert.False(obj.HasChangedProperties);
		Assert.False(obj.IsChanged);
		Assert.Equal(ObjectEditState.None, obj.State);
		Assert.False(obj.IsSavable);
	}

	#endregion

	#region ObservableList

	private class TestItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
				}
			}
		}
	}

	[Fact]
	public void Ctor_WithSinglePassEnumerable_ShouldHookChildChanges()
	{
		var list = new ObservableList<TestItem>(GenerateItems());

		var changed = 0;
		list.ChildChanged += (_, _) => changed++;

		list[0].Name = "changed";

		Assert.Equal(1, changed);
		Assert.Equal(2, list.Count);

		static IEnumerable<TestItem> GenerateItems()
		{
			yield return new TestItem();
			yield return new TestItem();
		}
	}

	[Fact]
	public void DuplicateItem_ShouldOnlyHookOnce()
	{
		var list = new ObservableList<TestItem>();
		var item = new TestItem();
		list.Add(item);
		list.Add(item);

		var changed = 0;
		list.ChildChanged += (_, _) => changed++;

		item.Name = "changed";

		Assert.Equal(1, changed);
	}

	[Fact]
	public void AddRange_ShouldRaiseSingleReset()
	{
		var list = new ObservableList<TestItem>();
		var resets = 0;
		var other = 0;
		list.CollectionChanged += (_, e) =>
		{
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				resets++;
			}
			else
			{
				other++;
			}
		};

		list.AddRange(new[] { new TestItem(), new TestItem(), new TestItem() });

		Assert.Equal(3, list.Count);
		Assert.Equal(1, resets);
		Assert.Equal(0, other);
	}

	[Fact]
	public void AddRange_AddedItems_ShouldBeHooked()
	{
		var list = new ObservableList<TestItem>();
		var item = new TestItem();
		list.AddRange([item]);

		var changed = 0;
		list.ChildChanged += (_, _) => changed++;

		item.Name = "changed";

		Assert.Equal(1, changed);
	}

	#endregion

	#region ObservableDictionary

	[Fact]
	public void GetOrAdd_NewKey_ShouldAddAndRaiseEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var events = new List<DictionaryChangedEventArgs<string, int>>();
		dict.ItemChanged += (_, e) => events.Add(e);

		var result = dict.GetOrAdd("key", _ => 42);

		Assert.Equal(42, result);
		Assert.Equal(42, dict["key"]);
		var e = Assert.Single(events);
		Assert.Equal(DictionaryChangedAction.Add, e.Action);
	}

	[Fact]
	public void GetOrAdd_ExistingKey_ShouldReturnExistingWithoutEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		dict.Add("key", 1);
		var events = new List<DictionaryChangedEventArgs<string, int>>();
		dict.ItemChanged += (_, e) => events.Add(e);

		var result = dict.GetOrAdd("key", _ => 42);

		Assert.Equal(1, result);
		Assert.Empty(events);
	}

	[Fact]
	public void GetOrAdd_ValueOverload_ShouldAddNewKey()
	{
		var dict = new ObservableDictionary<string, int>();

		var result = dict.GetOrAdd("key", 7);

		Assert.Equal(7, result);
		Assert.Equal(7, dict["key"]);
	}

	#endregion

	#region Factory optional parameter

	[Fact]
	public async Task CreateAsync_ShouldMatchMethodWithOptionalLastParameter()
	{
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var obj = await factory.CreateAsync<OptionalCriteriaObject>("abc");

		Assert.Equal("abc", obj.Code);
	}

	[Fact]
	public async Task CreateAsync_ShouldPreferExactLengthOverload()
	{
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var obj = await factory.CreateAsync<OverloadedCriteriaObject>("abc");

		// 长度精确匹配的 (string) 重载应优先于 (string, CancellationToken) 可选重载
		Assert.Equal("exact", obj.Which);
	}

	#endregion

	#region Helpers

	private static IServiceScope BeginRuleScope<T>(out T obj)
		where T : BusinessObject, new()
	{
		var scope = CreateScope(out var provider);
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		obj = new T();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		return scope;
	}

	private static IServiceScope CreateScope(out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		services.AddScoped<BusinessContextAccessor>();
		services.AddScoped<BusinessContext>();
		services.AddSingleton<IObjectFactory, BusinessObjectFactory>();
		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		provider = scope.ServiceProvider;
		return scope;
	}

	#endregion
}

/// <summary>
/// 带对象级规则（Property 为 null 的规则）的测试对象，用于验证 ValidationComplete 只触发一次。
/// </summary>
public class RuleObject : ObservableObject<RuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new AlwaysWarnObjectRule());
		Rules.AddRule<RuleObject>(NameProperty, o => !string.IsNullOrWhiteSpace(o.Name), "Name required");
	}
}

/// <summary>
/// 仅产生警告的对象级规则（Property 为 null），不会让对象无效。
/// </summary>
public class AlwaysWarnObjectRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddWarningResult("warning");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 使用同步 lambda 规则重载的测试对象。
/// </summary>
public class SyncRuleObject : ObservableObject<SyncRuleObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule<SyncRuleObject>(NameProperty, o => !string.IsNullOrWhiteSpace(o.Name), "Name required");
	}
}

/// <summary>
/// 覆盖 Insert/Update/Delete 的可编辑测试对象，SaveAsync 成功后会复位状态。
/// </summary>
public class SaveEditableObject : EditableObject<SaveEditableObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public static bool UpdateCalled { get; private set; }
	public static bool DeleteCalled { get; private set; }

	internal static void Reset()
	{
		UpdateCalled = false;
		DeleteCalled = false;
	}

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		UpdateCalled = true;
		await Task.CompletedTask;
	}

	[FactoryDelete]
	protected override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		DeleteCalled = true;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 末尾带可选 CancellationToken 参数的创建方法。
/// </summary>
public class OptionalCriteriaObject : BusinessObject<OptionalCriteriaObject>
{
	public string Code { get; private set; }

	[FactoryCreate]
	public async Task CreateAsync(string code, CancellationToken cancellationToken = default)
	{
		Code = code;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 同时存在精确长度与可选参数两个重载，精确长度应优先。
/// </summary>
public class OverloadedCriteriaObject : BusinessObject<OverloadedCriteriaObject>
{
	public string Which { get; private set; }

	[FactoryCreate]
	public Task CreateAsync(string code)
	{
		Which = "exact";
		return Task.CompletedTask;
	}

	[FactoryCreate]
	public async Task CreateAsync(string code, CancellationToken cancellationToken = default)
	{
		Which = "optional";
		await Task.CompletedTask;
	}
}