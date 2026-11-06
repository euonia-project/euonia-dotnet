using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证本次对 Euonia.Osba 的修复与新增功能：
/// 规则完成事件、保存后状态复位、MarkAsClean、ObservableList 构造器/AddRange、GetOrAdd、
/// 同步 lambda 规则重载、可选参数工厂方法匹配。
/// </summary>
public class OsbaFeatureTests
{
	#region Rules

	/// <summary>
	/// 验证异步对象级规则检查完成时只引发一次 <see cref="ObjectBase.ValidationComplete"/> 事件，
	/// 且无违规时对象保持有效。
	/// </summary>
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

	/// <summary>
	/// 验证同步路径的对象级规则检查同样只引发一次 <see cref="ObjectBase.ValidationComplete"/> 事件。
	/// </summary>
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

	/// <summary>
	/// 验证同步 lambda 规则会参与求值：未赋值时产生违规并使对象无效，赋值后重新检查可清除违规并恢复有效。
	/// </summary>
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

	/// <summary>
	/// 验证等待异步对象级规则检查完成不会死锁，正常结束后完成事件被引发。
	/// </summary>
	[Fact]
	public async Task CheckObjectRulesAsync_ShouldNotDeadlock_WaitingForCompletion()
	{
		// 对象级检查正常完成后必须引发一次完成事件，且 HasRunningRules 随之回到 false
		using var scope = BeginRuleScope<RuleObject>(out var obj);

		var completed = false;
		obj.ValidationComplete += (_, _) => completed = true;

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(completed);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Save / MarkAsClean

	/// <summary>
	/// 验证保存成功后返回同一实例、复位为干净状态并解除忙碌与可保存标志，再次编辑后可继续走更新路径保存。
	/// </summary>
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

		object? saved = null;
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

	/// <summary>
	/// 验证已标记删除的对象保存后复位为干净状态，重复保存为幂等的空操作，不会重复执行删除。
	/// </summary>
	[Fact]
	public async Task SaveDeleted_ShouldResetState_AndBeIdempotent()
	{
		using var scope = CreateScope(out var provider);
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		SaveEditableObject.Reset();

		var obj = new SaveEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();
		obj.MarkAsDeleted();

		await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		// 删除成功后对象应复位为干净状态：否则下一次 SaveAsync 会再执行一次删除。
		Assert.Equal(ObjectEditState.None, obj.State);
		Assert.True(SaveEditableObject.DeleteCalled);
		Assert.Equal(1, SaveEditableObject.DeleteCount);

		// 第二次保存没有待执行的操作，必须是幂等的 no-op。
		await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(1, SaveEditableObject.DeleteCount);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 验证 <see cref="ObservableObject{T}.MarkAsClean"/> 会清除全部脏标志：
	/// 属性变更标记、变更状态、编辑状态与可保存性均复位。
	/// </summary>
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

	/// <summary>
	/// 验证 <see cref="ObservableObject{T}.AcceptChanges"/> 提交当前值并清空变更跟踪，
	/// 与 <c>DataRow.AcceptChanges</c> 的语义一致。
	/// </summary>
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

	/// <summary>
	/// 实现 <see cref="INotifyPropertyChanged"/> 的列表子项，用于验证列表挂钩子项变更并向上转发。
	/// </summary>
	private class TestItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		private string _name = null!;

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

	/// <summary>
	/// 验证以单次枚举的序列构造列表时，所有元素都会被正确挂钩，后续子项属性变更可被上报。
	/// </summary>
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

	/// <summary>
	/// 验证同一实例被重复添加时只挂钩一次，子项属性变更只会引发一次
	/// <see cref="ObservableList{TItem}.ChildChanged"/> 事件。
	/// </summary>
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

	/// <summary>
	/// 验证批量添加只引发一次 <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Reset"/>
	/// 通知，不会为每个元素逐一发出其它集合变更通知。
	/// </summary>
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

	/// <summary>
	/// 验证批量添加的元素均已挂上钩子，后续子项属性变更能够引发
	/// <see cref="ObservableList{TItem}.ChildChanged"/> 事件。
	/// </summary>
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

	/// <summary>
	/// 验证 <see cref="ObservableDictionary{TKey,TValue}.GetOrAdd(TKey,System.Func{TKey,TValue})"/> 在键不存在时
	/// 调用工厂写入值，并引发一次添加类型的 <see cref="ObservableDictionary{TKey,TValue}.ItemChanged"/> 事件。
	/// </summary>
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

	/// <summary>
	/// 验证 <see cref="ObservableDictionary{TKey,TValue}.GetOrAdd(TKey,System.Func{TKey,TValue})"/> 在键已存在时
	/// 直接返回现有值，不调用工厂也不引发变更事件。
	/// </summary>
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

	/// <summary>
	/// 验证接收值而非工厂的 <see cref="ObservableDictionary{TKey,TValue}.GetOrAdd(TKey,TValue)"/> 重载
	/// 在键不存在时会写入并返回该值。
	/// </summary>
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

	/// <summary>
	/// 验证工厂能够匹配末尾带可选 <see cref="CancellationToken"/> 参数的创建方法，并传入调用参数。
	/// </summary>
	[Fact]
	public async Task CreateAsync_ShouldMatchMethodWithOptionalLastParameter()
	{
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var obj = await factory.CreateAsync<OptionalCriteriaObject>("abc");

		Assert.Equal("abc", obj.Code);
	}

	/// <summary>
	/// 验证存在多个重载时优先选择参数个数精确匹配的方法，而非带可选参数的重载。
	/// </summary>
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

	#region Factory method discovery convention

	/// <summary>
	/// 验证带 <see cref="FactoryCreateAttribute"/> 标记的方法即使名称不符合约定也能被发现并用作创建工厂。
	/// </summary>
	[Fact]
	public async Task CreateAsync_AttributeMarkedMethod_ShouldIgnoreMethodName()
	{
		// 有 [FactoryXxx] 标记的方法：名称与约定无关，仍应被发现
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var obj = await factory.CreateAsync<AttributeNamedObject>("abc");

		Assert.Equal("abc", obj.Code);
	}

	/// <summary>
	/// 验证无特性但名称符合约定（<c>Create</c>、<c>CreateAsync</c>、<c>FactoryCreate</c>、
	/// <c>FactoryCreateAsync</c>）的方法会被发现并用作创建工厂。
	/// </summary>
	[Fact]
	public async Task CreateAsync_ConventionallyNamedMethod_ShouldBeDiscovered()
	{
		// 无标记的方法：严格按名称搜索（Create / CreateAsync / FactoryCreate / FactoryCreateAsync）
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var obj = await factory.CreateAsync<ConventionallyNamedObject>("abc");

		Assert.Equal("abc", obj.Code);
	}

	/// <summary>
	/// 验证名称拼错且无工厂特性的方法不会被识别为创建工厂，调用创建时抛出
	/// <see cref="MissingMethodException"/>。
	/// </summary>
	[Fact]
	public async Task CreateAsync_MisspelledMethodName_ShouldNotBeAccepted()
	{
		// 无标记且名称不符约定：不得被当作该操作的工厂方法
		using var scope = CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		await Assert.ThrowsAsync<MissingMethodException>(() => factory.CreateAsync<MisspelledMethodObject>("abc"));
	}

	#endregion

	#region Helpers

	private static IServiceScope BeginRuleScope<T>(out T obj)
		where T : ObjectBase, new()
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
	/// <summary>
	/// 对象名称属性的元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 获取对象规则集合，供测试直接触发规则检查。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 获取或设置对象名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 注册对象级警告规则与名称必填规则，用于验证完成事件与有效性判定。
	/// </summary>
	protected override void AddRules()
	{
		Rules.AddRule(new AlwaysWarnObjectRule());
		Rules.AddRule<RuleObject>(NameProperty, o => !string.IsNullOrWhiteSpace(o.Name), "Name required");
	}
}

/// <summary>
/// 仅产生警告的对象级规则（Property 为 null），不会让对象无效。
/// </summary>
public class AlwaysWarnObjectRule : ObjectRuleBase
{
	/// <summary>
	/// 执行对象级规则并添加一条警告结果，不使对象无效。
	/// </summary>
	/// <param name="context">规则执行上下文。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示规则执行完成的任务。</returns>
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
	/// <summary>
	/// 名称属性的元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 获取对象规则集合，供测试直接触发规则检查。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 获取或设置对象名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 注册同步 lambda 形式的名称必填规则。
	/// </summary>
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
	/// <summary>
	/// 对象名称属性的元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 获取更新工厂方法是否被调用。
	/// </summary>
	public static bool UpdateCalled { get; private set; }

	/// <summary>
	/// 获取删除工厂方法是否被调用。
	/// </summary>
	public static bool DeleteCalled { get; private set; }

	/// <summary>
	/// 获取删除工厂方法被调用的次数。
	/// </summary>
	public static int DeleteCount { get; private set; }

	/// <summary>
	/// 复位工厂调用标记，避免静态状态在测试之间泄漏。
	/// </summary>
	internal static void Reset()
	{
		UpdateCalled = false;
		DeleteCalled = false;
		DeleteCount = 0;
	}

	/// <summary>
	/// 获取或设置对象名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 插入操作的工厂实现，测试中不执行实际操作。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[FactoryInsert]
	protected internal override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 更新操作的工厂实现，记录更新方法已被调用。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[FactoryUpdate]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		UpdateCalled = true;
		await Task.CompletedTask;
	}

	/// <summary>
	/// 删除操作的工厂实现，记录删除方法被调用并累计调用次数。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[FactoryDelete]
	protected internal override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		DeleteCalled = true;
		DeleteCount++;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 末尾带可选 CancellationToken 参数的创建方法。
/// </summary>
public class OptionalCriteriaObject : BusinessObject<OptionalCriteriaObject>
{
	/// <summary>
	/// 获取根据创建条件写入的编码。
	/// </summary>
	public string Code { get; private set; } = null!;

	/// <summary>
	/// 末尾带可选取消令牌的创建工厂方法，将创建条件写入 <see cref="Code"/>。
	/// </summary>
	/// <param name="code">创建条件。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[FactoryCreate]
	public async Task CreateAsync(string code, CancellationToken cancellationToken = default)
	{
		Code = code;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 带 [FactoryCreate] 标记、但方法名不符合任何约定的对象。
/// </summary>
public class AttributeNamedObject : BusinessObject<AttributeNamedObject>
{
	/// <summary>
	/// 获取根据创建条件写入的编码。
	/// </summary>
	public string Code { get; private set; } = null!;

	/// <summary>
	/// 名称与约定无关，仅靠特性被识别为创建工厂方法。
	/// </summary>
	/// <param name="code">创建条件。</param>
	[FactoryCreate]
	public async Task BuildItUp(string code)
	{
		Code = code;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 无 factory 特性、仅按约定名称（<c>CreateAsync</c>）声明的对象。
/// </summary>
public class ConventionallyNamedObject : BusinessObject<ConventionallyNamedObject>
{
	/// <summary>
	/// 获取根据创建条件写入的编码。
	/// </summary>
	public string Code { get; private set; } = null!;

	/// <summary>
	/// 无特性，按约定名称被识别为创建工厂方法。
	/// </summary>
	/// <param name="code">创建条件。</param>
	public async Task CreateAsync(string code)
	{
		Code = code;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 方法名拼错且无 factory 特性的对象。
/// </summary>
public class MisspelledMethodObject : BusinessObject<MisspelledMethodObject>
{
	/// <summary>
	/// 获取根据创建条件写入的编码。
	/// </summary>
	public string Code { get; private set; } = null!;

	/// <summary>
	/// 名称拼错（<c>CreateAsnyc</c>）且无特性，不应被识别为创建工厂方法。
	/// </summary>
	/// <param name="code">创建条件。</param>
	public async Task CreateAsnyc(string code)
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
	/// <summary>
	/// 获取实际被工厂选中的重载标识。
	/// </summary>
	public string Which { get; private set; } = null!;

	/// <summary>
	/// 参数个数精确匹配的创建工厂重载，将选中标识记为 <c>exact</c>。
	/// </summary>
	/// <param name="code">创建条件。</param>
	/// <returns>表示创建完成的任务。</returns>
	[FactoryCreate]
	public Task CreateAsync(string code)
	{
		Which = "exact";
		return Task.CompletedTask;
	}

	/// <summary>
	/// 末尾带可选取消令牌的创建工厂重载，将选中标识记为 <c>optional</c>。
	/// </summary>
	/// <param name="code">创建条件。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示创建完成的任务。</returns>
	[FactoryCreate]
	public async Task CreateAsync(string code, CancellationToken cancellationToken = default)
	{
		Which = "optional";
		await Task.CompletedTask;
	}
}