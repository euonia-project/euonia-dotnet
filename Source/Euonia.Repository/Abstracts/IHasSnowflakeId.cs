namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示使用雪花算法（Snowflake）生成标识的对象。
/// </summary>
/// <remarks>
/// 标识为 <see cref="long"/> 类型，通常由雪花算法在应用侧生成，以保证分布式环境下标识的全局唯一与趋势递增。
/// </remarks>
public interface IHasSnowflakeId
{
	/// <summary>
	/// 获取或设置 <see cref="long"/> 类型的对象标识。
	/// </summary>
	/// <value>由雪花算法生成的对象标识。</value>
	long Id { get; set; }
}