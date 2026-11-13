namespace Nerosoft.Euonia.Endpoint;

/// <summary>
/// 标记方法需要暴露为 gRPC 服务方法。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ExposeGrpcAttribute : Attribute
{
	/// <summary>
	/// 获取或设置 gRPC 服务全名。
	/// </summary>
	public string Prefix { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置 gRPC 方法名。
	/// </summary>
	public string Route { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置 gRPC 方法类型。
	/// </summary>
	public GrpcMethod Method { get; set; } = GrpcMethod.Unary;
}

/// <summary>
/// gRPC 方法类型。
/// </summary>
public enum GrpcMethod
{
	/// <summary>
	/// 一元调用。
	/// </summary>
	Unary = 0,

	/// <summary>
	/// 服务端流。
	/// </summary>
	ServerStreaming = 1,

	/// <summary>
	/// 客户端流。
	/// </summary>
	ClientStreaming = 2,

	/// <summary>
	/// 双向流。
	/// </summary>
	DuplexStreaming = 3
}
