using System.Reflection;
using System.Security.Claims;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 受控资源：仅供本测试项目内的模型与判定使用，与任何宿主框架无关。
/// </summary>
public sealed class Asset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }

	public string Level { get; set; }
}

/// <summary>
/// <see cref="Asset"/> 的权限模型：本人或本部门可见，密级资源一律拒绝。
/// </summary>
public sealed class AssetModel : ScopeModel<Asset>
{
	public override void Define(ScopeModelBuilder<Asset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId)
		       .Classify("level", x => x.Level);
	}

	public override ScopePolicy<Asset> Policy =>
		ScopePolicy<Asset>.All(
			ScopePolicy<Asset>.Any(
				ScopePolicy<Asset>.Self(),
				ScopePolicy<Asset>.Grant(ScopeDimensions.Dept)),
			ScopePolicy<Asset>.Deny(ScopePolicy<Asset>.Where(x => x.Level == "secret")));
}

/// <summary>
/// 只有权限码、没有「操作」概念的资源：宿主从不按对象状态推断操作，判定入口一律直接给码。
/// </summary>
public sealed class KeyedAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }
}

/// <summary>
/// <see cref="KeyedAsset"/> 的权限模型：全部用 <see cref="ScopePolicySet{T}.For"/> 按<b>权限码</b>声明。
/// </summary>
public sealed class KeyedAssetModel : ScopeModel<KeyedAsset>
{
	public const string View = "keyed:view";

	public const string Delete = "keyed:delete";

	public override void Define(ScopeModelBuilder<KeyedAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	public override ScopePolicy<KeyedAsset> Policy => ScopePolicy<KeyedAsset>.Self();

	public override void Declare(ScopePolicySet<KeyedAsset> policies)
	{
		policies.For(View, ScopePolicy<KeyedAsset>.Any(ScopePolicy<KeyedAsset>.Self(), ScopePolicy<KeyedAsset>.Grant(ScopeDimensions.Dept)));
		policies.For(Delete, ScopePolicy<KeyedAsset>.Grant(ScopeDimensions.Owner));
	}
}

/// <summary>
/// 子表行：工作区成员。授权关系（谁属于这个工作区）存在这里，而不是工作区行上。
/// </summary>
public sealed class WorkspaceMember
{
	public string UserId { get; set; }

	public string Status { get; set; }
}

/// <summary>
/// 子表维度资源：成员关系在子表里，「我加入了哪些工作区」由此判定（见 <see cref="WorkspaceModel"/>）。
/// </summary>
/// <remarks>
/// 子集合<b>刻意不给初始化器</b>：未加载时为空引用，单行判定会明确报错而不是静默拒绝。
/// </remarks>
public sealed class Workspace
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public List<WorkspaceMember> Members { get; set; }
}

/// <summary>
/// <see cref="Workspace"/> 的权限模型：成员（子表维度）或负责人可见。
/// </summary>
public sealed class WorkspaceModel : ScopeModel<Workspace>
{
	public override void Define(ScopeModelBuilder<Workspace> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .MapMany(ScopeDimensions.Member, x => x.Members.Select(m => m.UserId));
	}

	public override ScopePolicy<Workspace> Policy =>
		ScopePolicy<Workspace>.Any(
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Member),
			ScopePolicy<Workspace>.Self());
}

/// <summary>
/// 子表行：频道成员，带状态属性（用于验证子表属性参与选择器）。
/// </summary>
public sealed class ChannelMember
{
	public string UserId { get; set; }

	public string Status { get; set; }
}

/// <summary>
/// 子表维度资源的第二种形态：只有「有效成员」算成员——子表属性写在选择器里，由数据库实时求值。
/// </summary>
public sealed class Channel
{
	public string Id { get; set; }

	public List<ChannelMember> Members { get; set; }
}

/// <summary>
/// <see cref="Channel"/> 的权限模型：仅「有效成员」可见。
/// </summary>
public sealed class ChannelModel : ScopeModel<Channel>
{
	public override void Define(ScopeModelBuilder<Channel> builder)
	{
		builder.MapMany(ScopeDimensions.Member, x => x.Members.Where(m => m.Status == "active").Select(m => m.UserId));
	}

	public override ScopePolicy<Channel> Policy => ScopePolicy<Channel>.Grant(ScopeDimensions.Member);
}

/// <summary>
/// 子表维度资源的第三种形态：集合元素本身就是维度值（选择器不带取值投射）。
/// </summary>
public sealed class SharedDocument
{
	public string Id { get; set; }

	public List<string> ReaderIds { get; set; }
}

/// <summary>
/// <see cref="SharedDocument"/> 的权限模型：被列为读者的用户可见。
/// </summary>
public sealed class SharedDocumentModel : ScopeModel<SharedDocument>
{
	public const string Reader = "reader";

	public override void Define(ScopeModelBuilder<SharedDocument> builder)
	{
		builder.MapMany(Reader, x => x.ReaderIds);
	}

	public override ScopePolicy<SharedDocument> Policy => ScopePolicy<SharedDocument>.Grant(Reader);
}

/// <summary>入口特性：只用于验证配置载体按特性类型匹配。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UnionProbeAttribute : Attribute;

/// <summary>入口特性的基类：验证「规则写基类型、派生特性实例同样命中」。</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class EntryMarkAttribute : Attribute;

/// <summary><see cref="EntryMarkAttribute"/> 的派生特性。</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DerivedEntryMarkAttribute : EntryMarkAttribute;

/// <summary>泛型入口特性：配置里引用它应在注册期被拒绝（泛型特性无法作为入口标记）。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GenericMarkAttribute<T> : Attribute;

/// <summary>
/// 「按特性」与「按命名」分别对应不同权限码的资源：用于证明配置里的两条规则是或语义，
/// 而不是「只生效一条也不会被发现」。
/// </summary>
public sealed class UnionProbeAsset
{
	/// <summary>只按命名命中。</summary>
	[Permission("probe:by-name")]
	public void Probe() { }

	/// <summary>只按特性命中。</summary>
	[Permission("probe:by-attr")]
	[UnionProbe]
	public void Purge() { }

	/// <summary>只按派生特性命中（规则里写的是基类型）。</summary>
	[Permission("probe:by-derived")]
	[DerivedEntryMark]
	public void Sweep() { }
}

/// <summary>
/// 未密封的受控资源：用于构造「声明类型已注册、实例却是派生类型」的场景。
/// </summary>
public class ProxyableAsset
{
	public string Id { get; set; }

	public string DeptId { get; set; }
}

/// <summary>
/// <see cref="ProxyableAsset"/> 的权限模型：本部门可见。只声明默认策略，
/// 因此可与 <see cref="EmptyCodeSource"/> 搭配使用。
/// </summary>
public sealed class ProxyableAssetModel : ScopeModel<ProxyableAsset>
{
	public override void Define(ScopeModelBuilder<ProxyableAsset> builder)
	{
		builder.Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	public override ScopePolicy<ProxyableAsset> Policy =>
		ScopePolicy<ProxyableAsset>.Grant(ScopeDimensions.Dept);
}

/// <summary>
/// 类型级声明权限码的资源。
/// </summary>
[Permission("asset:read")]
public sealed class ClassLevelAsset
{
	public string Id { get; set; }
}

/// <summary>
/// 按「带 <c>run</c> 特性即视为执行入口」的约定提供权限码的来源，
/// 用于验证方法级声明只有经 <see cref="IPermissionCodeSource"/> 才能被看见。
/// </summary>
public sealed class ConventionCodeSource : IPermissionCodeSource
{
	private static readonly string[] Operations = [.. BusinessOperation.All];

	public IReadOnlyList<string> AllOperations => Operations;

	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		if (operation != BusinessOperation.Execute)
		{
			return [];
		}

		// 「名为 Run 的方法即执行入口」——这正是引擎无从推断、必须由宿主给出的约定
		var method = type.GetMethod("Run");

		return method?.GetCustomAttributes<PermissionAttribute>(true)
		           .Select(x => x.Permission)
		           .Where(x => !string.IsNullOrEmpty(x))
		           .ToArray() ?? [];
	}
}

/// <summary>
/// 按调用参数返回固定授权数据的解析器。
/// </summary>
public sealed class FixedSubjectResolver : IScopeSubjectResolver
{
	private readonly ScopeSubjectSet _subjects;

	public FixedSubjectResolver(string[] codes = null, (string Dimension, string Value)[] grants = null)
	{
		var builder = ScopeSubjectSet.CreateBuilder();

		if (codes is { Length: > 0 })
		{
			builder.AddCodes(codes);
		}

		foreach (var (dimension, value) in grants ?? [])
		{
			builder.Add(dimension, value);
		}

		_subjects = builder.Build();
	}

	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
		=> new(_subjects);
}

/// <summary>
/// 标记「审批」操作入口的示例特性，用于验证通用来源的按特性约定。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AssetApproveAttribute : Attribute;

public sealed class AssetSecondApproveAttribute : Attribute;

/// <summary>
/// 类型级 + 方法级（两种约定：特性与命名）都声明了审批权限的资源。
/// </summary>
[Permission("asset:read")]
public sealed class ApprovableAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }

	[Permission("asset:approve", "auditor", "manager")]
	[AssetApprove]
	public void Approve() { }

	[Permission("asset:approve")]
	public void ApproveAsync() { }

	[AssetApprove]
	[AssetSecondApprove]
	public void ApproveViaAttribute() { }
}

/// <summary>
/// 没有类型级权限声明的资源：用于精确验证「只按入口方法收集」的语义。
/// </summary>
public sealed class ApproveOnlyAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	[Permission("asset:approve")]
	[AssetApprove]
	public void Approve() { }

	[Permission("asset:approve")]
	public void ApproveAsync() { }

	[AssetApprove]
	[AssetSecondApprove]
	public void ApproveViaAttribute() { }
}
