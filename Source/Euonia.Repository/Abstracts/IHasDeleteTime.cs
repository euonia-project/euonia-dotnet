namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示包含删除时间属性的对象。
/// </summary>
/// <remarks>
/// 通常与逻辑删除（软删除）配合使用：记录被删除时写入删除时间，未删除时为 <c>null</c>。
/// </remarks>
public interface IHasDeleteTime
{
	/// <summary>
	/// 获取或设置记录的删除时间。
	/// </summary>
	/// <value>记录的删除时间；若尚未删除则为 <c>null</c>。</value>
	DateTime? DeletedAt { get; set; }
}