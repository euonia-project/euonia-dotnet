using Nerosoft.Euonia.Bus;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 分页查询请求的基类，携带分页参数并声明返回结果列表。
/// </summary>
/// <typeparam name="TResult">分页结果中单项的类型。</typeparam>
public abstract record PagedQueryRequest<TResult> : IRequest<List<TResult>>
{
	/// <summary>
	/// 获取或设置需要跳过的记录数，默认为 0。
	/// </summary>
	public int Skip { get; set; } = 0;

	/// <summary>
	/// 获取或设置本次取回的记录数，默认为 20。
	/// </summary>
	public int Take { get; set; } = 20;
}