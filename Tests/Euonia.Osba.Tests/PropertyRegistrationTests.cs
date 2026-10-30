using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证属性注册的表达式重载可以直接从属性推断值类型，避免显式泛型参数与装箱。
/// </summary>
public class PropertyRegistrationTests
{
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

	[Fact]
	public void RegisterProperty_ShouldSupportDefaultValueOverload()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var target = RuleTestHarness.Create<InferredPropertyObject>(provider);

		Assert.Equal(7, target.Defaulted);
		Assert.Equal(7, InferredPropertyObject.DefaultedProperty.DefaultValue);
	}

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
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(p => p.Name);

	public static readonly PropertyInfo<int> CountProperty = RegisterProperty(p => p.Count);

	public static readonly PropertyInfo<int> DefaultedProperty = RegisterProperty(p => p.Defaulted, "默认值", 7);

	public static readonly PropertyInfo<List<string>> TagsProperty = RegisterProperty(p => p.Tags, "标签", () => []);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	public int Count
	{
		get => GetProperty(CountProperty);
		set => SetProperty(CountProperty, value);
	}

	public int Defaulted
	{
		get => GetProperty(DefaultedProperty);
		set => SetProperty(DefaultedProperty, value);
	}

	public List<string> Tags
	{
		get => GetProperty(TagsProperty);
		set => SetProperty(TagsProperty, value);
	}
}
