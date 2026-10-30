namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象的生命周期回调契约。
/// </summary>
/// <remarks>
/// <para>
/// 回调由框架在固定边界调用：<see cref="OnSavingAsync"/> / <see cref="OnSavedAsync"/> 围绕保存，
/// <see cref="OnExecutingAsync"/> / <see cref="OnExecutedAsync"/> 围绕服务对象执行体。
/// 默认实现全部为空，派生类按需覆写，用于审计、领域事件收集、缓存失效等横切逻辑。
/// </para>
/// <para>
/// 回调在业务对象实例上执行，不改变规则与权限的裁决顺序：保存回调位于规则校验之后、工厂方法之前；
/// 执行回调位于授权与规则校验之后、执行体之前。
/// </para>
/// </remarks>
public interface IObjectLifecycle
{
	/// <summary>
	/// 保存前回调。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	Task OnSavingAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 保存成功后回调。
	/// </summary>
	/// <param name="result">工厂返回的保存结果；可能与当前实例不同。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	Task OnSavedAsync(object result, CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行体执行前回调。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	Task OnExecutingAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行体执行成功后回调。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示异步回调的任务。</returns>
	Task OnExecutedAsync(CancellationToken cancellationToken = default);
}
