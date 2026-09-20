namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 记录本次注册中权限体系对应用的要求，供构建容器后的启动期校验使用。
/// </summary>
/// <remarks>
/// 之所以不在 <c>AddBusinessObject</c> 里直接检查解析器是否已注册：解析器通常在该调用之后才注册，
/// 在那里检查会误报。因此这里只记录「是否需要」，真正的检查放在
/// <c>provider.ValidatePermissionSetup()</c>（容器已构建、注册顺序已确定）。
/// </remarks>
public sealed class PermissionSetup
{
	/// <summary>
	/// 初始化 <see cref="PermissionSetup"/> 的新实例。
	/// </summary>
	/// <param name="requiresSubjectResolver">是否需要 <see cref="IScopeSubjectResolver"/>。</param>
	internal PermissionSetup(bool requiresSubjectResolver)
	{
		RequiresSubjectResolver = requiresSubjectResolver;
	}

	/// <summary>
	/// 获取一个值，指示是否必须注册 <see cref="IScopeSubjectResolver"/>。
	/// </summary>
	/// <remarks>
	/// 声明了权限模型（<see cref="IScopeModel{T}"/>）或使用了 <see cref="PermissionAttribute"/>
	/// 时为 <see langword="true"/>——两者都需要从授权数据解析「用户被授予了什么」。
	/// </remarks>
	public bool RequiresSubjectResolver { get; }
}
