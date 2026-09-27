using System.Reflection;
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
	private static readonly BusinessOperation[] Operations = Enum.GetValues<BusinessOperation>();

	public IReadOnlyList<BusinessOperation> AllOperations => Operations;

	public IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation)
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
/// 固定键的解析器，用于验证宿主实现不会被 <c>AddPermission</c> 覆盖。
/// </summary>
public sealed class FixedKeyResolver : IScopeKeyResolver
{
	public const string Key = "host:key";

	public string Resolve(object resource, string explicitKey) => explicitKey ?? Key;
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

	public ValueTask<ScopeSubjectSet> ResolveAsync(System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default)
		=> new(_subjects);
}
