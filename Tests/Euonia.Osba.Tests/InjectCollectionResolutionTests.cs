using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// <c>[Inject]</c> 属性类型的解析口径：<b>与 <c>_collectionTypesName</c> 宣称支持的集合接口同源</b>。
/// </summary>
/// <remarks>
/// <c>_collectionTypesName</c> 列出 <c>IList&lt;&gt;</c> / <c>ICollection&lt;&gt;</c> / <c>IEnumerable&lt;&gt;</c>，
/// 泛型分支必须把三者一并解出元素类型，否则常量会过度承诺。
/// 同时钉住「只解一层集合」：第二层开始必须抛出，因为该保护是 fail-closed 的承重结构，
/// 删掉它会让嵌套集合晚爆于 <c>PropertyInfo.SetValue</c> 的晦涩 <c>ArgumentException</c>。
/// </remarks>
public class InjectCollectionResolutionTests
{
	/// <summary>
	/// 验证数组、<see cref="IEnumerable{T}"/>、<see cref="ICollection{T}"/> 与 <see cref="IList{T}"/> 均解析出元素类型并标记为多实例注入。
	/// </summary>
	[Fact]
	public void AutoInject_Should_Resolve_Element_Type_For_Every_Supported_Collection_Interface()
	{
		var byName = PropertiesOf(typeof(CollectionInjectSubject));

		Assert.Equal(typeof(SampleService), byName["Single"].Type);
		Assert.False(byName["Single"].Multiple);

		Assert.Equal(typeof(SampleService), byName["Array"].Type);
		Assert.True(byName["Array"].Multiple);

		Assert.Equal(typeof(SampleService), byName["Enumerable"].Type);
		Assert.True(byName["Enumerable"].Multiple);

		Assert.Equal(typeof(SampleService), byName["Collection"].Type);
		Assert.True(byName["Collection"].Multiple);

		Assert.Equal(typeof(SampleService), byName["List"].Type);
		Assert.True(byName["List"].Multiple);
	}

	/// <summary>
	/// 验证嵌套集合属性被拒绝注入，并抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public void AutoInject_Should_Reject_Nested_Collection_Property()
	{
		var exception = Assert.Throws<NotSupportedException>(
			() => ObjectReflector.GetAutoInjectProperties(typeof(NestedCollectionInjectSubject)));

		Assert.Contains("can not be injected as a single service", exception.Message);
	}

	/// <summary>
	/// 验证基元类型属性被拒绝注入，并抛出 <see cref="NotSupportedException"/>。
	/// </summary>
	[Fact]
	public void AutoInject_Should_Reject_Primitive_Property()
	{
		var exception = Assert.Throws<NotSupportedException>(
			() => ObjectReflector.GetAutoInjectProperties(typeof(PrimitiveInjectSubject)));

		Assert.Contains("primitive type", exception.Message);
	}

	private static Dictionary<string, (Type Type, bool Multiple)> PropertiesOf(Type objectType)
	{
		return ObjectReflector.GetAutoInjectProperties(objectType)
		                     .ToDictionary(tuple => tuple.Item1.Name, tuple => (tuple.Item2, tuple.Item3));
	}

	/// <summary>
	/// 用于验证集合类型注入解析的示例服务。
	/// </summary>
	private sealed class SampleService;

	/// <summary>
	/// 以单值、数组及多种集合接口声明注入点的测试主体。
	/// </summary>
	private sealed class CollectionInjectSubject
	{
		[Inject]
		public SampleService Single { get; set; } = null!;

		[Inject]
		public SampleService[] Array { get; set; } = null!;

		[Inject]
		public IEnumerable<SampleService> Enumerable { get; set; } = null!;

		[Inject]
		public ICollection<SampleService> Collection { get; set; } = null!;

		[Inject]
		public IList<SampleService> List { get; set; } = null!;
	}

	/// <summary>
	/// 声明嵌套集合注入点的测试主体，用于验证只解析一层集合、嵌套集合被拒绝。
	/// </summary>
	private sealed class NestedCollectionInjectSubject
	{
		[Inject]
		public IList<IList<SampleService>> Nested { get; set; } = null!;
	}

	/// <summary>
	/// 声明基元类型注入点的测试主体，用于验证基元类型被拒绝解析。
	/// </summary>
	private sealed class PrimitiveInjectSubject
	{
		[Inject]
		public int Count { get; set; }
	}
}
