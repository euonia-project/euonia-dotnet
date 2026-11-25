using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// Core 侧权限声明读取器（<see cref="PermissionRequirements"/>）的行为：
/// 两级缓存、类型级 / 方法级并集、<see cref="PermissionRequirements.Merge"/> 的保序去重、
/// 按 <c>(类型, 操作)</c> 查询的兼容来源的适配器复用，以及裁剪后保留 <see cref="PermissionAttribute.Message"/>。
/// </summary>
/// <remarks>
/// 这些用例直接读声明、不经过 <c>AddPermission</c>：注册期还会做违规声明校验，
/// 而这里要钉住的是「发现」本身的语义。
/// </remarks>
public class PermissionRequirementsTests
{
	private static readonly MethodInfo RunMethod = typeof(CacheProbeAsset).GetMethod(nameof(CacheProbeAsset.Run))!;

	private static readonly MethodInfo TrimmedMethod = typeof(CacheProbeAsset).GetMethod(nameof(CacheProbeAsset.Trimmed))!;

	/// <summary>
	/// 类型级声明按类型缓存：两次读取返回同一个只读快照，且内容就是类型上声明的那条码。
	/// </summary>
	[Fact]
	public void Type_Requirements_Should_Be_Cached_And_Come_From_The_Type()
	{
		var first = PermissionRequirements.For(typeof(CacheProbeAsset));
		var second = PermissionRequirements.For(typeof(CacheProbeAsset));

		Assert.Same(first, second);
		Assert.Equal(["cache:type-level"], first.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 方法级声明按 <c>(Module, MetadataToken)</c> 缓存：即便反射返回不同的 <see cref="MethodInfo"/> 实例，
	/// 也命中同一份只读快照（用引用相等作键会让缓存形同虚设）。
	/// </summary>
	[Fact]
	public void Method_Requirements_Should_Be_Cached_Across_Reflection_Instances()
	{
		var first = PermissionRequirements.For(RunMethod);
		var second = PermissionRequirements.For(typeof(CacheProbeAsset).GetMethod(nameof(CacheProbeAsset.Run))!);

		Assert.Same(first, second);
		Assert.Equal(["cache:method-level"], first.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// <c>For(type, null)</c> 表示「不是从某个具体方法进入的」，只返回类型级声明。
	/// </summary>
	[Fact]
	public void For_With_Null_Method_Should_Return_Type_Level_Only()
	{
		var requirements = PermissionRequirements.For(typeof(CacheProbeAsset), null);

		Assert.Equal(["cache:type-level"], requirements.Select(requirement => requirement.Permission));
		Assert.DoesNotContain(requirements, requirement => requirement.Permission == "cache:method-level");
	}

	/// <summary>
	/// <c>For(type, method)</c> 取类型级 ∪ 方法级，类型级在前；同一条码不会出现两次。
	/// </summary>
	[Fact]
	public void For_With_Method_Should_Union_Type_And_Method_Requirements()
	{
		var requirements = PermissionRequirements.For(typeof(CacheProbeAsset), RunMethod);

		Assert.Equal(["cache:type-level", "cache:method-level"], requirements.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 方法级读取只读<b>这一个</b>方法：同类型的兄弟方法上的声明不会混进来。
	/// </summary>
	[Fact]
	public void For_Method_Should_Not_Contain_Sibling_Declarations()
	{
		var update = PermissionRequirements.For(typeof(MethodIsolationProbe), typeof(MethodIsolationProbe).GetMethod(nameof(MethodIsolationProbe.Update))!);

		Assert.Equal(["probe:update"], update.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// <see cref="PermissionRequirements.Merge"/> 保持首次出现顺序，并按「码（忽略大小写）+ 角色」去重。
	/// </summary>
	[Fact]
	public void Merge_Should_Preserve_First_Seen_Order_And_De_Duplicate()
	{
		var merged = PermissionRequirements.Merge(
			[new PermissionAttribute("order:read"), new PermissionAttribute("order:update")],
			[new PermissionAttribute("ORDER:READ"), new PermissionAttribute("order:delete")],
			[new PermissionAttribute("order:delete")]);

		Assert.Equal(["order:read", "order:update", "order:delete"], merged.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 去重口径与旧合并来源一致：码忽略大小写、角色按序数比较——同码不同角色是两条合法要求。
	/// </summary>
	[Fact]
	public void Merge_Should_Keep_Same_Code_With_Different_Roles()
	{
		var merged = PermissionRequirements.Merge(
			[new PermissionAttribute("order:read", "auditor")],
			[new PermissionAttribute("ORDER:READ", "manager")]);

		Assert.Equal(2, merged.Count);
	}

	/// <summary>
	/// 角色按 <see cref="StringComparison.Ordinal"/> 比较：<c>Admin</c> 与 <c>admin</c> 是两个角色，
	/// 因此同码 + 不同大小写角色是两条要求；码与角色都相同则只保留一条。
	/// </summary>
	[Fact]
	public void Merge_Should_Compare_Roles_Ordinal()
	{
		var distinct = PermissionRequirements.Merge(
			[new PermissionAttribute("order:read", "Admin")],
			[new PermissionAttribute("ORDER:READ", "admin")]);

		Assert.Equal(2, distinct.Count);

		var deduplicated = PermissionRequirements.Merge(
			[new PermissionAttribute("order:read", "Admin")],
			[new PermissionAttribute("ORDER:READ", "Admin")]);

		Assert.Single(deduplicated);
	}

	/// <summary>
	/// 合并时同样裁剪权限码：数据来源给出的 <c>" order:read "</c> 必须与授予的 <c>order:read</c> 命中。
	/// </summary>
	[Fact]
	public void Merge_Should_Trim_Codes()
	{
		var merged = PermissionRequirements.Merge([new PermissionAttribute("  order:read  ")]);

		Assert.Equal(["order:read"], merged.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 裁剪产生新副本时保留原来的 <see cref="PermissionAttribute.Message"/>。
	/// </summary>
	[Fact]
	public void Trimmed_Requirement_Should_Keep_Message()
	{
		var requirements = PermissionRequirements.For(typeof(CacheProbeAsset), TrimmedMethod);
		var trimmed = Assert.Single(requirements, requirement => requirement.Permission == "cache:trimmed");

		Assert.Equal("trimmed message", trimmed.Message);
	}

	/// <summary>
	/// 同一个兼容来源（<see cref="IPermissionCodeSource"/>）实例始终得到同一个适配器（判定路径不反复分配）。
	/// </summary>
	[Fact]
	public void Adapt_Should_Reuse_The_Adapter_For_The_Same_Source()
	{
		var source = new AlwaysEmptyCodeSource();

		Assert.Same(PermissionRequirements.Adapt(source), PermissionRequirements.Adapt(source));
	}

	/// <summary>
	/// 适配器按 <c>(类型, 操作)</c> 转交兼容来源、忽略方法上下文：同一 <c>(类型, 操作)</c> 下传入不同方法
	/// 都得到同一份要求；换一个操作则不再返回该码。
	/// </summary>
	[Fact]
	public void Adapted_Source_Should_Forward_By_Type_And_Operation_Ignoring_Method()
	{
		var adapted = PermissionRequirements.Adapt(new LegacyExtraCodeSource());

		var updateMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Update))!;
		var trimmedMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Trimmed))!;

		// 该兼容来源看不见方法：不同方法传入同一 (类型, update) 得到同样的码
		var fromUpdate = adapted.RequirementsFor(typeof(CodeModelAsset), updateMethod, BusinessOperation.Update);
		var fromTrimmed = adapted.RequirementsFor(typeof(CodeModelAsset), trimmedMethod, BusinessOperation.Update);

		Assert.Contains(fromUpdate, requirement => requirement.Permission == "legacy:extra");
		Assert.Equal(["legacy:extra"], fromTrimmed.Select(requirement => requirement.Permission));

		// 换一个操作：说明确实按 (类型, 操作) 转交，而不是恒返回同一个码
		Assert.Empty(adapted.RequirementsFor(typeof(CodeModelAsset), updateMethod, BusinessOperation.Read));
	}

	/// <summary>
	/// 空参数按契约抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void Null_Arguments_Should_Throw()
	{
		Assert.Throws<ArgumentNullException>(() => PermissionRequirements.For((Type)null!));
		Assert.Throws<ArgumentNullException>(() => PermissionRequirements.For((MethodInfo)null!));
		Assert.Throws<ArgumentNullException>(() => PermissionRequirements.Adapt(null!));
		Assert.Throws<ArgumentNullException>(() => PermissionRequirements.Merge(null!));
	}
}

/// <summary>
/// 带类型级与方法级声明的资源：用于缓存、并集与裁剪用例。
/// </summary>
[Permission("cache:type-level")]
public sealed class CacheProbeAsset
{
	/// <summary>
	/// 只带方法级声明的入口方法。
	/// </summary>
	[Permission("cache:method-level")]
	public void Run() { }

	/// <summary>
	/// 权限码带首尾空白、并带提示消息的入口方法。
	/// </summary>
	[Permission("  cache:trimmed  ", Message = "trimmed message")]
	public void Trimmed() { }
}

/// <summary>
/// 两个兄弟方法各带不同权限码的资源：用于证明方法级隔离在读取器这一层就成立。
/// </summary>
public sealed class MethodIsolationProbe
{
	/// <summary>
	/// 更新入口：只声明 <c>probe:update</c>。
	/// </summary>
	[Permission("probe:update")]
	public void Update() { }

	/// <summary>
	/// 归档入口：只声明 <c>probe:archive</c>。
	/// </summary>
	[Permission("probe:archive")]
	public void Archive() { }
}

/// <summary>
/// 永远回答「没有要求」的兼容来源（按 <c>(类型, 操作)</c> 查询）：用于验证适配器的缓存与转交。
/// </summary>
internal sealed class AlwaysEmptyCodeSource : IPermissionCodeSource
{
	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => [];

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation) => [];
}
