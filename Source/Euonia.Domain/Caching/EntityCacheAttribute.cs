namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 指示被修饰的类是否启用实体缓存。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityCacheAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="EntityCacheAttribute"/> 的新实例。
	/// </summary>
	/// <param name="enabled">是否启用缓存，默认为 <c>true</c>。</param>
	public EntityCacheAttribute(bool enabled = true)
	{
		Enabled = enabled;
	}

	/// <summary>
	/// 获取一个值，指示是否启用实体缓存。
	/// </summary>
	public bool Enabled { get; }
}