namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 多租户能力的全局选项。
/// </summary>
public sealed class MultiTenancyOptions
{
	/// <summary>
	/// 配置节名称。
	/// </summary>
	public const string ConfigurationSection = "Euonia:MultiTenancy";

	/// <summary>
	/// 获取或设置一个值，指示是否启用多租户。
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否要求每个请求/消息必须解析到租户。
	/// </summary>
	public bool RequireTenant { get; set; } = true;

	/// <summary>
	/// 获取或设置承载租户标识的请求头名称。
	/// </summary>
	public string HeaderName { get; set; } = "X-Tenant-Id";

	/// <summary>
	/// 获取或设置承载活动租户声明的声明类型。
	/// </summary>
	public string ClaimType { get; set; } = "tenant";

	/// <summary>
	/// 获取或设置子域名解析格式，例如 <c>{tenant}.example.com</c>；为 <c>null</c> 时不启用。
	/// </summary>
	public string? HostFormat { get; set; }

	/// <summary>
	/// 获取或设置路由租户参数名；为 <c>null</c> 时不启用。
	/// </summary>
	public string? RouteKey { get; set; } = "tenant";

	/// <summary>
	/// 获取或设置单租户兼容模式下使用的默认租户。
	/// </summary>
	public string? DefaultTenantId { get; set; }

	/// <summary>
	/// 获取或设置默认隔离模式。
	/// </summary>
	public TenantIsolation DefaultIsolation { get; set; } = TenantIsolation.SharedTable;

	/// <summary>
	/// 获取或设置租户存储的缓存时长。
	/// </summary>
	public TimeSpan StoreCacheDuration { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 获取或设置一个值，指示匿名请求是否信任租户请求头。
	/// </summary>
	public bool TrustHeaderForAnonymous { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否在响应中回写租户头。
	/// </summary>
	public bool EmitResponseHeader { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否在错误响应中区分“租户不存在/停用/无权”。
	/// </summary>
	public bool ExposeTenantExistence { get; set; }

	/// <summary>
	/// 获取或设置平台管理员角色列表，这些角色可进入跨租户作用域。
	/// </summary>
	public string[] CrossTenantRoles { get; set; } = [];

	/// <summary>
	/// 获取或设置跳过租户要求的路径前缀。
	/// </summary>
	public string[] ExcludedPaths { get; set; } = ["/health", "/metrics", "/swagger"];

	/// <summary>
	/// 获取或设置 <see cref="TenantIsolation.DatabasePerTenant"/> 下的连接串模板。
	/// </summary>
	public string? ConnectionStringTemplate { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否信任 Broker 消息中的租户元数据。
	/// </summary>
	public bool TrustBrokerTenant { get; set; }

	/// <summary>
	/// 获取或设置可信 Broker 传输名称列表；仅在 <see cref="TrustBrokerTenant"/> 为 <see langword="true"/> 时生效。
	/// </summary>
	public string[] TrustedTransports { get; set; } = [];

	/// <summary>
	/// 获取或设置停用/归档租户的存量消息处理策略。
	/// </summary>
	public TenantInactiveMessagePolicy InactiveTenantMessagePolicy { get; set; } = TenantInactiveMessagePolicy.DeadLetter;

	/// <summary>
	/// 获取或设置一个值，指示 <see cref="TenantInactiveMessagePolicy.Process"/> 策略下是否允许写入停用租户。
	/// </summary>
	public bool AllowInactiveTenantWrites { get; set; }

	/// <summary>
	/// 获取或设置配置存储中的租户列表。
	/// </summary>
	public List<TenantInfo> Tenants { get; set; } = [];
}
