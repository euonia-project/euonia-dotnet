namespace Nerosoft.Euonia.Hosting.Webapi;

/// <summary>
/// <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.AddEuoniaWebApi"/> 的配置选项。
/// </summary>
public sealed class EuoniaWebApiOptions
{
	/// <summary>
	/// 获取或设置是否启用统一异常处理；默认 <c>true</c>。
	/// </summary>
	public bool EnableExceptionHandling { get; set; } = true;

	/// <summary>
	/// 获取或设置是否启用 OpenAPI 文档；默认 <c>true</c>。
	/// </summary>
	public bool EnableOpenApi { get; set; } = true;

	/// <summary>
	/// 获取或设置是否启用健康检查端点；默认 <c>true</c>。
	/// </summary>
	public bool EnableHealthChecks { get; set; } = true;

	/// <summary>
	/// 获取或设置健康检查端点路径；默认 <c>/health</c>。
	/// </summary>
	public string HealthCheckPath { get; set; } = "/health";

	/// <summary>
	/// 获取或设置是否启用 HTTPS 重定向；默认 <c>true</c>。
	/// </summary>
	public bool EnableHttpsRedirection { get; set; } = true;

	/// <summary>
	/// 获取或设置允许的 CORS 来源；为空时不注册 CORS。
	/// </summary>
	public string[] AllowedOrigins { get; set; } = [];
}
