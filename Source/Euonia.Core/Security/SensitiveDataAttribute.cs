namespace Nerosoft.Euonia.Security;

/// <summary>
/// 标记参数、属性、字段或对象类型为敏感数据，日志与审计记录时应进行脱敏。
/// </summary>
/// <remarks>
/// 应用于方法参数时，日志/审计适配层在记录调用参数前将其实参替换为掩码；
/// 应用于类型成员（属性/字段）时，<see cref="SensitiveDataMasker"/> 在序列化前将
/// 该成员替换为掩码，从而避免密码、令牌等敏感信息落入日志或审计存储。
/// 类上标注时，该类型（及其派生）在被记录时整体替换为掩码。
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
public sealed class SensitiveDataAttribute : Attribute
{
	/// <summary>
	/// 获取或设置脱敏后写入的掩码文本。
	/// </summary>
	public string Mask { get; set; } = "***";
}
