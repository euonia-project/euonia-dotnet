namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// HTTP 端点暴露选项。
/// </summary>
public sealed class HttpExposureOptions
{
	/// <summary>
	/// 默认配置节名称。
	/// </summary>
	public const string SectionName = "Euonia:Endpoint:Http";

	/// <summary>
	/// 是否启用 HTTP 端点暴露。
	/// </summary>
	public bool EnableExposeApplicationServiceAsWebApi { get; set; }

	/// <summary>
	/// 默认路由前缀。
	/// </summary>
	public string DefaultRoutePrefix { get; set; } = "api";

	/// <summary>
	/// 是否统一要求授权；默认开启。
	/// </summary>
	public bool RequireAuthorization { get; set; } = true;

	/// <summary>
	/// 未注册服务是否抛异常；否则跳过并记录警告。
	/// </summary>
	public bool ThrowOnUnregisteredService { get; set; } = true;

	/// <summary>
	/// 路由冲突是否抛异常；否则跳过并记录警告。
	/// </summary>
	public bool ThrowOnRouteConflict { get; set; } = true;

	/// <summary>
	/// 要扫描的程序集名称；配置后不再回退到入口程序集。
	/// </summary>
	public string[] ScanAssemblies { get; set; } = Array.Empty<string>();

	/// <summary>
	/// 要扫描的命名空间前缀；为空时不限制。
	/// </summary>
	public string[] ScanNamespaces { get; set; } = Array.Empty<string>();

	/// <summary>
	/// 要扫描的类型完整名称；为空时不限制。
	/// </summary>
	public string[] ScanTypes { get; set; } = Array.Empty<string>();

	/// <summary>
	/// 未配置 <see cref="ScanAssemblies"/> 时是否扫描入口程序集；默认开启。
	/// </summary>
	public bool ScanEntryAssembly { get; set; } = true;
}
