namespace Nerosoft.Euonia.Endpoint;

/// <summary>
/// 标记方法需要暴露为 Web API。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ExposeWebApiAttribute : Attribute
{
	/// <summary>
	/// 获取或设置路由前缀。
	/// </summary>
	public string Prefix { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置路由模板。
	/// </summary>
	public string Route { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置 HTTP 方法数组；至少包含一个元素。
	/// </summary>
	public string[] Method { get; set; } = new[] { HttpMethod.Post.Method };
}
