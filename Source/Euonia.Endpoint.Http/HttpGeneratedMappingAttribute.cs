namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 标记包含 HTTP 端点映射的生成类型。
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class HttpGeneratedMappingAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="HttpGeneratedMappingAttribute"/> 的新实例。
	/// </summary>
	/// <param name="mappingType">包含 <c>MapGenerated</c> 方法的映射类型。</param>
	public HttpGeneratedMappingAttribute(Type mappingType)
	{
		MappingType = mappingType;
	}

	/// <summary>
	/// 获取映射类型。
	/// </summary>
	public Type MappingType { get; }
}
