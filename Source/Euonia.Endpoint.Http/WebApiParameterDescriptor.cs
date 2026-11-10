using System.Reflection;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 参数绑定来源。
/// </summary>
internal enum WebApiParameterSource
{
	/// <summary>
	/// 路由值。
	/// </summary>
	Route,

	/// <summary>
	/// 查询字符串。
	/// </summary>
	Query,

	/// <summary>
	/// JSON 请求体。
	/// </summary>
	Body,

	/// <summary>
	/// 当前请求对象。
	/// </summary>
	Special,

	/// <summary>
	/// 请求取消令牌。
	/// </summary>
	CancellationToken
}

/// <summary>
/// 描述一个 HTTP 参数的绑定方式。
/// </summary>
internal sealed class WebApiParameterDescriptor
{
	/// <summary>
	/// 获取参数元数据。
	/// </summary>
	public required ParameterInfo Parameter { get; init; }

	/// <summary>
	/// 获取绑定来源。
	/// </summary>
	public required WebApiParameterSource Source { get; init; }

	/// <summary>
	/// 获取参数名称。
	/// </summary>
	public string? Name => Parameter.Name;

	/// <summary>
	/// 获取一个值，指示参数缺省时是否允许默认值或 <c>null</c>。
	/// </summary>
	public bool IsOptional { get; init; }

	/// <summary>
	/// 获取参数默认值。
	/// </summary>
	public object? DefaultValue { get; init; }

	/// <summary>
	/// 获取一个值，指示参数是否为集合。
	/// </summary>
	public bool IsCollection { get; init; }

	/// <summary>
	/// 获取集合元素类型；非集合参数为 <c>null</c>。
	/// </summary>
	public Type? ElementType { get; init; }

	/// <summary>
	/// 获取用于转换的目标类型。
	/// </summary>
	public required Type ValueType { get; init; }

	/// <summary>
	/// 获取一次性构建的集合工厂；参数不是集合时为 <c>null</c>。
	/// </summary>
	public Func<Array, object>? CollectionFactory { get; init; }
}
