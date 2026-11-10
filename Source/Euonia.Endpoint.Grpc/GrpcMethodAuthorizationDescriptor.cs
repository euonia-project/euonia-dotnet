namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// 描述一个 gRPC 方法与对应应用服务方法的授权元数据映射。
/// </summary>
public sealed class GrpcMethodAuthorizationDescriptor
{
	/// <summary>
	/// 初始化 <see cref="GrpcMethodAuthorizationDescriptor"/> 的新实例。
	/// </summary>
	/// <param name="methodPath">gRPC 方法完整路径，例如 <c>/Order/Get</c>。</param>
	/// <param name="serviceType">应用服务接口类型。</param>
	/// <param name="methodName">应用服务方法名称。</param>
	public GrpcMethodAuthorizationDescriptor(string methodPath, Type serviceType, string methodName)
	{
		MethodPath = methodPath;
		ServiceType = serviceType;
		MethodName = methodName;
	}

	/// <summary>获取 gRPC 方法完整路径。</summary>
	public string MethodPath { get; }

	/// <summary>获取应用服务接口类型。</summary>
	public Type ServiceType { get; }

	/// <summary>获取应用服务方法名称。</summary>
	public string MethodName { get; }
}
