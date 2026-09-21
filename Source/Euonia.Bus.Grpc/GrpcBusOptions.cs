namespace Nerosoft.Euonia.Bus.Grpc;

/// <summary>
/// 基于 gRPC 的消息总线选项定义。
/// </summary>
public class GrpcBusOptions
{
	/// <summary>
	/// 获取或设置一个值，指示该功能是否启用。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 获取或设置传输器名称。
	/// </summary>
	public string Name { get; set; } = "grpc";

	/// <summary>
	/// 获取或设置客户端连接的服务端地址，例如 <c>https://localhost:5001</c>。
	/// </summary>
	public string Endpoint { get; set; }

	/// <summary>
	/// 获取或设置序列化器提供程序名称。
	/// </summary>
	public string SerializerProvider { get; set; } = "SystemTestJson";
}