using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证 FieldData 撤销/变更跟踪及 PropertyInfo.FriendlyName 的修复行为。
/// </summary>
public class OsbaRegressionTests
{
	/// <summary>
	/// 验证新建的 <see cref="FieldData{T}"/> 未被修改过，变更标志为 <see langword="false"/>。
	/// </summary>
	[Fact]
	public void NewFieldData_ShouldNotBeChanged()
	{
		var field = new FieldData<string>("Name");

		Assert.False(field.IsChanged);
	}

	/// <summary>
	/// 验证为 <see cref="FieldData{T}"/> 赋值后会被标记为已修改。
	/// </summary>
	[Fact]
	public void SettingValue_ShouldMarkAsChanged()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";

		Assert.True(field.IsChanged);
	}

	/// <summary>
	/// 验证 <see cref="FieldData{T}.Undo"/> 会恢复上一次的值且不把恢复的值重新压入历史，
	/// 连续撤销可回到原始值并清除变更标志。
	/// </summary>
	[Fact]
	public void Undo_ShouldRestorePreviousValue_AndNotRepushHistory()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";
		field.Value = "second";

		field.Undo();

		// Undo 恢复上一次的值，且不会把该值重新压入历史栈
		Assert.Equal("first", field.Value);
		Assert.True(field.IsChanged);

		field.Undo();

		// 连续撤销应回到原始值，且 IsChanged 变为 false
		Assert.Null(field.Value);
		Assert.False(field.IsChanged);
	}

	/// <summary>
	/// 回归测试：历史深度受限时裁剪最旧记录不得倒置撤销顺序，撤销应优先恢复最新的值。
	/// </summary>
	[Fact]
	public void Undo_AfterHistoryTrim_ShouldRestoreNewestValuesFirst()
	{
		// 回归测试：裁剪最旧历史时曾把整个栈倒置，导致撤销顺序反向。
		var field = new FieldData<int>("Number") { HistoryDepth = 3 };
		field.Value = 1;
		field.Value = 2;
		field.Value = 3;
		field.Value = 4;

		field.Undo();
		Assert.Equal(3, field.Value);

		field.Undo();
		Assert.Equal(2, field.Value);

		field.Undo();
		Assert.Equal(1, field.Value);
	}

	/// <summary>
	/// 验证 <see cref="FieldData{T}.MarkAsUnchanged"/> 清除变更标志与历史记录，此后撤销不改变当前值。
	/// </summary>
	[Fact]
	public void MarkAsUnchanged_ShouldClearHistory()
	{
		var field = new FieldData<string>("Name");
		field.Value = "first";
		field.MarkAsUnchanged();

		Assert.False(field.IsChanged);

		// 无历史时撤销不应改变当前值
		field.Undo();
		Assert.Equal("first", field.Value);
	}

	/// <summary>
	/// 验证未显式提供友好名称时，<see cref="PropertyInfo{T}.FriendlyName"/> 回退为属性名。
	/// </summary>
	[Fact]
	public void FriendlyName_ShouldReturnPropertyName_WhenNotProvided()
	{
		var property = new PropertyInfo<string>("Name");

		Assert.Equal("Name", property.FriendlyName);
	}

	/// <summary>
	/// 验证显式提供友好名称时，<see cref="PropertyInfo{T}.FriendlyName"/> 返回该值而非属性名。
	/// </summary>
	[Fact]
	public void FriendlyName_ShouldReturnExplicitValue_WhenProvided()
	{
		var property = new PropertyInfo<string>("Name", "Full Name", default(string)!);

		Assert.Equal("Full Name", property.FriendlyName);
	}

	/// <summary>
	/// 验证订阅 <see cref="FieldData{T}"/> 的忙碌变更事件不会抛出
	/// <see cref="NotImplementedException"/>。
	/// </summary>
	[Fact]
	public void FieldData_BusyChangedEvent_ShouldNotThrow()
	{
		var field = new FieldData<string>("Name");
		BusyChangedEventArgs? raised = null;

		((INotifyBusy)field).BusyChanged += (_, args) => raised = args;

		// 事件订阅本身不应抛出 NotImplementedException
		Assert.Null(raised);
	}

	/// <summary>
	/// 验证以可变引用类型作为静态默认值注册时，每个对象获得独立实例，修改集合不会互相影响。
	/// </summary>
	[Fact]
	public void RegisterProperty_WithMutableDefault_ShouldNotShareInstanceAcrossObjects()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		first.Tags.Add("shared?");

		// 修改第一个对象的集合不应影响第二个对象
		Assert.Empty(second.Tags);
	}

	/// <summary>
	/// 验证以工厂提供默认值时，每个对象各调用一次工厂并获得独立实例。
	/// </summary>
	[Fact]
	public void RegisterProperty_WithFactory_ShouldCreateIndependentInstancePerObject()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		Assert.NotSame(first.FactoryTags, second.FactoryTags);

		first.FactoryTags.Add("x");

		Assert.Empty(second.FactoryTags);
	}

	/// <summary>
	/// 验证以可克隆引用类型作为默认值注册时，每个对象获得克隆副本，互不共享状态。
	/// </summary>
	[Fact]
	public void RegisterProperty_WithCloneableDefault_ShouldCreateIndependentInstancePerObject()
	{
		var first = new DefaultValueBusinessObject();
		var second = new DefaultValueBusinessObject();

		Assert.NotSame(first.Child, second.Child);

		first.Child.Name = "changed";
		Assert.Equal("initial", second.Child.Name);
	}

	/// <summary>
	/// 验证以值类型默认值注册的属性返回该类型的默认值。
	/// </summary>
	[Fact]
	public void RegisterProperty_WithValueTypeDefault_ShouldReturnDefaultValue()
	{
		var obj = new DefaultValueBusinessObject();

		Assert.Equal(0, obj.Number);
	}

	/// <summary>
	/// 验证解析 <see cref="BusinessContextAccessor"/> 本身不会产生环境上下文副作用，
	/// 显式设置的提供程序可跨异步边界流向派生任务。
	/// </summary>
	[Fact]
	public async Task BusinessContextAccessor_ShouldFlowExplicitlySetProviderAcrossAsyncBoundary()
	{
		var provider = new ServiceCollection()
		               .AddScoped<BusinessContextAccessor>()
		               .BuildServiceProvider();
		using var scope = provider.CreateScope();
		var scopedProvider = scope.ServiceProvider;

		// 解析 Scoped 服务本身不再产生环境上下文副作用；上下文必须显式建立。
		_ = scopedProvider.GetRequiredService<BusinessContextAccessor>();
		Assert.Null(BusinessContextAccessor.Current);

		BusinessContextAccessor.SetCurrent(scopedProvider);

		// 派生任务通过 ExecutionContext 继承当前异步流的 provider
		var flowed = await Task.Run(() => BusinessContextAccessor.Current);

		Assert.Same(scopedProvider, flowed);
		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 验证不同并行异步流之间 <see cref="BusinessContextAccessor.Current"/> 相互隔离，不会泄漏。
	/// </summary>
	[Fact]
	public async Task BusinessContextAccessor_ShouldIsolateBetweenParallelFlows()
	{
		var providerA = new ServiceCollection().BuildServiceProvider();
		var providerB = new ServiceCollection().BuildServiceProvider();

		var taskA = Task.Run(() =>
		{
			BusinessContextAccessor.SetCurrent(providerA);
			return BusinessContextAccessor.Current;
		});

		var taskB = Task.Run(() =>
		{
			BusinessContextAccessor.SetCurrent(providerB);
			return BusinessContextAccessor.Current;
		});

		var results = await Task.WhenAll(taskA, taskB);

		// 不同异步流之间互不泄漏
		Assert.Same(providerA, results[0]);
		Assert.Same(providerB, results[1]);
		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 验证 <see cref="BusinessContextAccessor.Clear"/> 会清除当前提供程序，
	/// 之后 <see cref="BusinessContextAccessor.Current"/> 为 <see langword="null"/>。
	/// </summary>
	[Fact]
	public void BusinessContextAccessor_Clear_ShouldResetCurrentProvider()
	{
		var provider = new ServiceCollection().BuildServiceProvider();
		BusinessContextAccessor.SetCurrent(provider);

		BusinessContextAccessor.Clear();

		Assert.Null(BusinessContextAccessor.Current);
	}
}

/// <summary>
/// 用于验证默认值隔离的测试业务对象。
/// </summary>
public class DefaultValueBusinessObject : BusinessObject<DefaultValueBusinessObject>
{
	/// <summary>
	/// 使用可变引用类型（集合）作为静态默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<List<string>> TagsProperty =
		RegisterProperty<List<string>>(nameof(Tags), null, new List<string>());

	/// <summary>
	/// 使用工厂生成默认值的属性。
	/// </summary>
	public static readonly PropertyInfo<List<string>> FactoryTagsProperty =
		RegisterProperty<List<string>>(nameof(FactoryTags), null, () => new List<string>());

	/// <summary>
	/// 使用实现了 <see cref="ICloneable"/> 的引用类型默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<DefaultValueChild> ChildProperty =
		RegisterProperty<DefaultValueChild>(nameof(Child), null, new DefaultValueChild("initial"));

	/// <summary>
	/// 使用值类型默认值注册的属性。
	/// </summary>
	public static readonly PropertyInfo<int> NumberProperty =
		RegisterProperty<int>(nameof(Number));

	/// <summary>
	/// 获取注册时提供的可变集合默认值。
	/// </summary>
	public List<string> Tags => ReadProperty(TagsProperty);

	/// <summary>
	/// 获取由工厂创建的集合默认值。
	/// </summary>
	public List<string> FactoryTags => ReadProperty(FactoryTagsProperty);

	/// <summary>
	/// 获取注册时提供的可克隆子对象默认值。
	/// </summary>
	public DefaultValueChild Child => ReadProperty(ChildProperty);

	/// <summary>
	/// 获取以值类型默认值注册的数值。
	/// </summary>
	public int Number => ReadProperty(NumberProperty);
}

/// <summary>
/// 可克隆的测试子对象。
/// </summary>
public class DefaultValueChild : ICloneable
{
	/// <summary>
	/// 以指定名称初始化 <see cref="DefaultValueChild"/> 的新实例。
	/// </summary>
	/// <param name="name">子对象名称。</param>
	public DefaultValueChild(string name)
	{
		Name = name;
	}

	/// <summary>
	/// 获取或设置子对象名称。
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// 创建包含当前名称的副本。
	/// </summary>
	/// <returns>新的 <see cref="DefaultValueChild"/> 实例。</returns>
	public object Clone()
	{
		return new DefaultValueChild(Name);
	}
}
