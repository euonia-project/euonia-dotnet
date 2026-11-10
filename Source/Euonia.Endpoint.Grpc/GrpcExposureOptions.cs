namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// gRPC 端点暴露选项。
/// </summary>
public sealed class GrpcExposureOptions
{
	/// <summary>默认配置节名称。</summary>
	public const string SectionName = "Euonia:Endpoint:Grpc";

	/// <summary>是否启用 gRPC 端点暴露。</summary>
	public bool EnableExposeApplicationServiceAsGrpc { get; set; }

	/// <summary>是否统一要求授权。</summary>
	public bool RequireAuthorization { get; set; } = true;

	/// <summary>是否启用 gRPC reflection。</summary>
	public bool EnableReflection { get; set; }

	/// <summary>未注册服务是否抛异常；否则跳过该服务。</summary>
	public bool ThrowOnUnregisteredService { get; set; } = true;

	/// <summary>要扫描的程序集名称；配置后不再回退到入口程序集。</summary>
	public string[] ScanAssemblies { get; set; } = Array.Empty<string>();

	/// <summary>未配置 <see cref="ScanAssemblies"/> 时是否扫描入口程序集；默认开启。</summary>
	public bool ScanEntryAssembly { get; set; } = true;
}
