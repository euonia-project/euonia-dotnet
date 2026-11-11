namespace Nerosoft.Euonia.Messaging;

/// <summary>
/// 定义跨组件共享的消息元数据键。
/// </summary>
/// <remarks>
/// 这些键放在 Core，Bus 与 Application 无需引用 Tenancy 即可写入/读取；
/// 租户与追踪语义由上层适配层解释。
/// </remarks>
public static class MessageMetadataKeys
{
	/// <summary>
	/// 重投消息引用的原始 traceparent；消费时作为 ActivityLink 而不是父级。
	/// </summary>
	public const string LinkTraceParent = "trace.link";

	/// <summary>
	/// 租户标识元数据键。
	/// </summary>
	public const string TenantId = "$nerosoft:tenant.id";

	/// <summary>
	/// 租户显示名称元数据键；仅诊断使用。
	/// </summary>
	public const string TenantName = "$nerosoft:tenant.name";

	/// <summary>
	/// 用户标识元数据键。
	/// </summary>
	public const string UserId = "$nerosoft:user.id";

	/// <summary>
	/// 旧版用户租户元数据键，兼容现有 <c>UserContextBehavior</c>。
	/// </summary>
	public const string LegacyUserTenant = "$nerosoft:user.tenant";
}
