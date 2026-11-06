using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证属性注册的表达式重载可以直接从属性推断值类型，避免显式泛型参数与装箱。
/// </summary>
public class PropertyRegistrationTests
{
	/// <summary>
	/// 验证属性注册的表达式重载可从属性表达式推断值类型，赋值与读取均正常工作，
	/// 且属性名与类型元数据与表达式一致。
	/// </summary>
	[Fact]
	public void RegisterProperty_ShouldInferValueTypeFromExpression()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<InferredPropertyObject>(provider);

		target.Name = "osba";
		target.Count = 42;

		Assert.Equal("osba", target.Name);
		Assert.Equal(42, target.Count);
		Assert.Equal(nameof(InferredPropertyObject.Name), InferredPropertyObject.NameProperty.Name);
		Assert.Equal(nameof(InferredPropertyObject.Count), InferredPropertyObject.CountProperty.Name);
		Assert.Equal(typeof(string), InferredPropertyObject.NameProperty.Type);
		Assert.Equal(typeof(int), InferredPropertyObject.CountProperty.Type);
	}

	/// <summary>
	/// 验证表达式重载支持直接提供默认值，未赋值时读取到该默认值。
	/// </summary>
	[Fact]
	public void RegisterProperty_ShouldSupportDefaultValueOverload()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<InferredPropertyObject>(provider);

		Assert.Equal(7, target.Defaulted);
		Assert.Equal(7, InferredPropertyObject.DefaultedProperty.DefaultValue);
	}

	/// <summary>
	/// 验证表达式重载支持默认值工厂，每个对象获得独立实例，互不共享集合内容。
	/// </summary>
	[Fact]
	public void RegisterProperty_ShouldSupportDefaultValueFactoryOverload()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var first = RuleTestHarness.Create<InferredPropertyObject>(provider);
		var second = RuleTestHarness.Create<InferredPropertyObject>(provider);

		first.Tags.Add("a");

		Assert.Empty(second.Tags);
		Assert.Single(first.Tags);
	}
}

/// <summary>
/// 使用表达式推断属性类型的测试对象。
/// </summary>
public sealed class InferredPropertyObject : EditableObject<InferredPropertyObject>
{
	/// <summary>
	/// 名称属性的元数据，类型由表达式推断为 <see cref="string"/>。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	/// <summary>
	/// 计数属性的元数据，类型由表达式推断为 <see cref="int"/>。
	/// </summary>
	public static readonly PropertyInfo<int> CountProperty = RegisterProperty(p => p.Count);

	/// <summary>
	/// 带默认值 <c>7</c> 的数值属性元数据。
	/// </summary>
	public static readonly PropertyInfo<int> DefaultedProperty = RegisterProperty(p => p.Defaulted, "默认值", 7);

	/// <summary>
	/// 带默认值工厂的标签集合属性元数据，每个对象获得独立集合。
	/// </summary>
	public static readonly PropertyInfo<List<string>> TagsProperty = RegisterProperty(p => p.Tags, "标签", () => []);

	/// <summary>
	/// 获取或设置名称。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 获取或设置计数。
	/// </summary>
	public int Count
	{
		get => GetProperty(CountProperty);
		set => SetProperty(CountProperty, value);
	}

	/// <summary>
	/// 获取或设置带默认值的数值。
	/// </summary>
	public int Defaulted
	{
		get => GetProperty(DefaultedProperty);
		set => SetProperty(DefaultedProperty, value);
	}

	/// <summary>
	/// 获取或设置标签集合。
	/// </summary>
	public List<string> Tags
	{
		get => GetProperty(TagsProperty);
		set => SetProperty(TagsProperty, value);
	}
}
