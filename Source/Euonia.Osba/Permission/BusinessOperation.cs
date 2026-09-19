namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示可在业务对象上执行的业务操作类型。
/// </summary>
public enum BusinessOperation
{
	/// <summary>读取操作，例如 <see cref="IObjectFactory.Fetch{T}"/>。</summary>
	Read,

	/// <summary>创建（插入）操作。</summary>
	Create,

	/// <summary>更新操作。</summary>
	Update,

	/// <summary>删除操作。</summary>
	Delete,

	/// <summary>命令执行操作，例如 <see cref="IObjectFactory"/> 的执行方法。</summary>
	Execute
}