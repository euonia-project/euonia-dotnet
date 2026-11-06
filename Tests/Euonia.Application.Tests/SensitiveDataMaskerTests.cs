using System.Collections;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证 <see cref="SensitiveDataMasker"/> 的脱敏行为：空值、标量透传、敏感属性、嵌套对象、集合、循环引用与整体敏感类型。
/// </summary>
public class SensitiveDataMaskerTests
{
	/// <summary>
	/// 传入 <c>null</c> 时，脱敏结果应同样为 <c>null</c>。
	/// </summary>
	[Fact]
	public void Mask_Null_ShouldReturnNull()
	{
		Assert.Null(SensitiveDataMasker.Mask(null));
	}

	/// <summary>
	/// 字符串、整数与布尔等标量应原样返回，不生成脱敏副本。
	/// </summary>
	[Fact]
	public void Mask_StringAndScalars_ShouldPassThrough()
	{
		Assert.Equal("hello", SensitiveDataMasker.Mask("hello"));
		Assert.Equal(42, SensitiveDataMasker.Mask(42));
		Assert.Equal(true, SensitiveDataMasker.Mask(true));
	}

	/// <summary>
	/// 普通属性保留原值，标注 <see cref="SensitiveDataAttribute"/> 的密码属性应替换为掩码文本 <c>"###"</c>。
	/// </summary>
	[Fact]
	public void Mask_Object_ShouldMaskSensitiveProperties()
	{
		var command = new LoginCommand { Username = "alice", Password = "plain" };

		var result = SensitiveDataMasker.Mask(command) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("alice", result[nameof(LoginCommand.Username)]);
		Assert.Equal("###", result[nameof(LoginCommand.Password)]);
	}

	/// <summary>
	/// 嵌套对象应递归脱敏：外层属性保留原值，内层敏感属性替换为 <c>"###"</c>。
	/// </summary>
	[Fact]
	public void Mask_NestedSensitiveObject_ShouldRecurse()
	{
		var outer = new OuterCommand { Name = "outer" };
		outer.Inner = new InnerCommand { Id = 1, Secret = "s3cret" };

		var result = SensitiveDataMasker.Mask(outer) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("outer", result[nameof(OuterCommand.Name)]);
		var inner = Assert.IsAssignableFrom<IDictionary<string, object>>(result[nameof(OuterCommand.Inner)]);
		Assert.Equal(1, inner[nameof(InnerCommand.Id)]);
		Assert.Equal("###", inner[nameof(InnerCommand.Secret)]);
	}

	/// <summary>
	/// 集合中的每个元素都应独立脱敏，敏感属性均替换为掩码，且返回可索引的列表。
	/// </summary>
	[Fact]
	public void Mask_Collection_ShouldMaskEachItem()
	{
		var list = new List<LoginCommand>
		{
			new() { Username = "a", Password = "s1" },
			new() { Username = "b", Password = "s2" },
		};

		var result = SensitiveDataMasker.Mask(list) as IList;

		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		var first = Assert.IsAssignableFrom<IDictionary<string, object>>(result[0]);
		Assert.Equal("a", first[nameof(LoginCommand.Username)]);
		Assert.Equal("###", first[nameof(LoginCommand.Password)]);
	}

	/// <summary>
	/// 自引用对象图不应导致无限递归，循环引用处返回掩码占位。
	/// </summary>
	[Fact]
	public void Mask_CyclicReference_ShouldNotInfiniteLoop()
	{
		var node = new CyclicNode { Name = "node" };
		node.Child = node;

		var result = SensitiveDataMasker.Mask(node) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("node", result[nameof(CyclicNode.Name)]);
		Assert.NotNull(result[nameof(CyclicNode.Child)]);
	}

	/// <summary>
	/// 无法展开的框架类型（如 <see cref="Uri"/>）应替换为包含类型名的占位符，且不泄露原始值。
	/// </summary>
	[Fact]
	public void Mask_Opaque_FrameworkType_ShouldUseTypeNamePlaceholder()
	{
		var outer = new OuterCommand { Name = "outer" };
		outer.InnerRef = new Uri("https://example.com/plain");

		var result = SensitiveDataMasker.Mask(outer) as IDictionary<string, object>;

		Assert.NotNull(result);
		var placeholder = Assert.IsAssignableFrom<string>(result[nameof(OuterCommand.InnerRef)]);
		Assert.Contains("Uri", placeholder);
		Assert.DoesNotContain("plain", placeholder);
	}

	/// <summary>
	/// 类型自身标注 <see cref="SensitiveDataAttribute"/> 时，应整体替换为默认掩码 <c>"***"</c>。
	/// </summary>
	[Fact]
	public void Mask_SensitiveClass_ShouldMaskWholeObject()
	{
		var secret = new WholeSensitiveCommand { PublicName = "x" };

		var result = SensitiveDataMasker.Mask(secret);

		Assert.Equal("***", result);
	}

	/// <summary>
	/// 对同一对象重复脱敏应得到一致结果，不因缓存状态而变化。
	/// </summary>
	[Fact]
	public void Mask_RepeatedCalls_ShouldBeStable()
	{
		var command = new LoginCommand { Username = "alice", Password = "plain" };

		var first = SensitiveDataMasker.Mask(command) as IDictionary<string, object>;
		var second = SensitiveDataMasker.Mask(command) as IDictionary<string, object>;

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.Equal(first[nameof(LoginCommand.Username)], second[nameof(LoginCommand.Username)]);
		Assert.Equal(first[nameof(LoginCommand.Password)], second[nameof(LoginCommand.Password)]);
	}

	/// <summary>
	/// 嵌套脱敏场景的外层对象：普通属性保留原值，内层对象与其内部的敏感属性需递归处理。
	/// </summary>
	private sealed class OuterCommand
	{
		public string Name { get; set; } = null!;

		public InnerCommand Inner { get; set; } = null!;

		public object InnerRef { get; set; } = null!;
	}

	/// <summary>
	/// 嵌套脱敏场景的内层对象，其中 <see cref="Secret"/> 标注了自定义掩码的特性。
	/// </summary>
	private sealed class InnerCommand
	{
		public int Id { get; set; }

		[SensitiveData(Mask = "###")]
		public string Secret { get; set; } = null!;
	}

	/// <summary>
	/// 整体标注 <see cref="SensitiveDataAttribute"/> 的类型，用于验证对象会被整体替换为默认掩码。
	/// </summary>
	[SensitiveData]
	private sealed class WholeSensitiveCommand
	{
		public string PublicName { get; set; } = null!;
	}

	/// <summary>
	/// 可自引用的节点类型，用于构造循环对象图并验证脱敏不会无限递归。
	/// </summary>
	private sealed class CyclicNode
	{
		public string Name { get; set; } = null!;

		public CyclicNode Child { get; set; } = null!;
	}
}