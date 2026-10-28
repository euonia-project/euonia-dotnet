namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 服务对象：封装一次独立的业务操作。
/// </summary>
/// <typeparam name="T">服务对象的具体类型。</typeparam>
/// <remarks>
/// <para>
/// 服务对象用于承载<b>不属于单个业务对象生命周期</b>的逻辑：协调多个领域对象、
/// 执行特定业务流程，或直接操作数据。它只保留属性存储与规则能力，
/// 不参与变更追踪、没有 <c>SaveAsync</c>，也不属于可编辑实体。
/// </para>
/// <para>
/// 输入由工厂方法参数（执行器的 <c>criteria</c>）或普通属性带入；执行体写在
/// <see cref="ExecuteAsync(CancellationToken)"/>。属性级规则依赖 setter 变更通知，
/// 不适用于服务对象；校验请写成<b>对象级</b>规则，在业务执行体之前由工厂边界裁决。
/// </para>
/// </remarks>
public abstract class ServiceObject<T> : ObjectBase<T>, IServiceObject
	where T : ServiceObject<T>
{
	/// <summary>
	/// 执行业务操作。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	protected internal virtual Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 创建新的服务对象。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步创建操作的任务。</returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}
