namespace Nerosoft.Euonia.Security.Tests.BrokenFixtures;

/// <summary>
/// 只映射了 owner，却按 dept 授权的资源。
/// </summary>
public sealed class UnmappedDimensionAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }
}

/// <summary>
/// 策略引用了未映射维度的模型：应在注册期报错，而不是运行时静默放行。
/// </summary>
public sealed class UnmappedDimensionModel : ScopeModel<UnmappedDimensionAsset>
{
	/// <inheritdoc />
	public override void Define(ScopeModelBuilder<UnmappedDimensionAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId);
	}

	/// <inheritdoc />
	public override ScopePolicy<UnmappedDimensionAsset> Policy =>
		ScopePolicy<UnmappedDimensionAsset>.Grant(ScopeDimensions.Dept);
}

/// <summary>
/// 始终可判定的资源。
/// </summary>
public sealed class AlwaysDenyAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }
}

/// <summary>
/// 结构性恒不放行的模型：<c>Any</c> 之下全是拒绝条件，应在注册期报错。
/// </summary>
public sealed class AlwaysDenyModel : ScopeModel<AlwaysDenyAsset>
{
	/// <inheritdoc />
	public override void Define(ScopeModelBuilder<AlwaysDenyAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId);
	}

	/// <inheritdoc />
	public override ScopePolicy<AlwaysDenyAsset> Policy =>
		ScopePolicy<AlwaysDenyAsset>.Any(
			ScopePolicy<AlwaysDenyAsset>.Deny(ScopePolicy<AlwaysDenyAsset>.Where(_ => true)));
}

/// <summary>
/// 子表维度用了不受支持取值形状的资源。
/// </summary>
public sealed class UnsupportedCollectionAsset
{
	public string Id { get; set; }

	public string[] Tags { get; set; }

	public string[] OtherTags { get; set; }
}

/// <summary>
/// 取值形状不受支持的模型：集合维度只支持「导航集合（可带 Where 过滤）再取字符串值」，
/// 其余形状应<b>在注册期</b>被拒绝，而不是等到查询时由提供程序抛出翻译失败。
/// </summary>
public sealed class UnsupportedCollectionModel : ScopeModel<UnsupportedCollectionAsset>
{
	/// <inheritdoc />
	public override void Define(ScopeModelBuilder<UnsupportedCollectionAsset> builder)
	{
		builder.MapMany("tags", x => x.Tags.Concat(x.OtherTags));
	}

	/// <inheritdoc />
	public override ScopePolicy<UnsupportedCollectionAsset> Policy =>
		ScopePolicy<UnsupportedCollectionAsset>.Grant("tags");
}

/// <summary>
/// 正常的资源，用于验证混合注册（程序集 + 实例）路径。
/// </summary>
public sealed class ReportAsset
{
	public string Id { get; set; }

	public string OwnerId { get; set; }

	public string DeptId { get; set; }
}

/// <summary>
/// 配置正确的模型。
/// </summary>
public sealed class ReportAssetModel : ScopeModel<ReportAsset>
{
	/// <inheritdoc />
	public override void Define(ScopeModelBuilder<ReportAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.DeptId);
	}

	/// <inheritdoc />
	public override ScopePolicy<ReportAsset> Policy =>
		ScopePolicy<ReportAsset>.Grant(ScopeDimensions.Dept);
}
