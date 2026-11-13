namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// 标记包含 gRPC 服务注册与映射代码的生成类型。
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class GrpcGeneratedMappingAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="GrpcGeneratedMappingAttribute"/> 的新实例。
	/// </summary>
	/// <param name="mappingType">包含 <c>AddGenerated</c> / <c>MapGenerated</c> 与 <c>ServiceContracts</c> 的映射类型。</param>
	public GrpcGeneratedMappingAttribute(Type mappingType)
	{
		MappingType = mappingType;
	}

	/// <summary>
	/// 获取映射类型。
	/// </summary>
	public Type MappingType { get; }
}
