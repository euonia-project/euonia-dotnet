namespace Nerosoft.Euonia.MultiTenancy;

/// <summary>
/// 标记实体参与租户隔离。
/// </summary>
/// <remarks>
/// 该接口是纯标记：默认情况下 EF 适配层会为标记实体配置影子属性 <c>TenantId</c>，
/// 实体无需声明属性，也不占用基类继承。需要直接读取租户标识的实体可实现
/// <see cref="IMultiTenantEntity"/>。
/// </remarks>
public interface IMultiTenant
{
}

/// <summary>
/// 标记实体参与租户隔离，并提供显式的租户标识属性。
/// </summary>
public interface IMultiTenantEntity : IMultiTenant
{
	/// <summary>
	/// 获取或设置租户标识。
	/// </summary>
	string TenantId { get; set; }
}
