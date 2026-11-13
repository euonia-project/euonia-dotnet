using System.Reflection;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 描述一个待映射的 HTTP 端点。
/// </summary>
internal sealed class WebApiActionDescriptor
{
	/// <summary>
	/// 获取服务接口类型。
	/// </summary>
	public required Type ServiceType { get; init; }

	/// <summary>
	/// 获取接口方法。
	/// </summary>
	public required MethodInfo Method { get; init; }

	/// <summary>
	/// 获取路由模板。
	/// </summary>
	public required string RoutePattern { get; init; }

	/// <summary>
	/// 获取 HTTP 方法集合。
	/// </summary>
	public required IReadOnlyList<string> HttpMethods { get; init; }

	/// <summary>
	/// 获取参数描述集合。
	/// </summary>
	public required IReadOnlyList<WebApiParameterDescriptor> Parameters { get; init; }

	/// <summary>
	/// 获取一个值，指示方法是否无返回值。
	/// </summary>
	public required bool ReturnsVoid { get; init; }

	/// <summary>
	/// 获取返回值类型；无返回值时为 <c>null</c>。
	/// </summary>
	public required Type? ResultType { get; init; }

	/// <summary>
	/// 获取端点元数据。
	/// </summary>
	public required IReadOnlyList<object> Metadata { get; init; }
}
