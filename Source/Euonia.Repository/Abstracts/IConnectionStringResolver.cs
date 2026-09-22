namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 定义针对特定仓储上下文类型的连接字符串解析器契约。
/// </summary>
/// <typeparam name="TContext">
/// 该解析器所面向的仓储上下文类型，必须是实现了 <see cref="IRepositoryContext"/> 的引用类型。
/// </typeparam>
/// <remarks>
/// 该解析器通常作为连接字符串的兜底来源参与解析，可用于从密钥保管库等外部来源按需获取连接字符串。
/// </remarks>
public interface IConnectionStringResolver<TContext>
	where TContext : class, IRepositoryContext
{
	/// <summary>
	/// 以异步方式获取已配置上下文的连接字符串。
	/// </summary>
	/// <param name="cancellation">
	/// 用于取消操作的 <see cref="CancellationToken"/>，默认为 <see cref="CancellationToken.None"/>。
	/// </param>
	/// <returns>
	/// 表示异步操作的任务，其结果为解析出的连接字符串。
	/// 若无法解析到连接字符串，结果可能为 <c>null</c> 或空字符串。
	/// </returns>
	Task<string> GetConnectionStringAsync(CancellationToken cancellation = default);
}