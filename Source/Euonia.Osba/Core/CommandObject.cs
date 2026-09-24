namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 命令对象。
/// </summary>
/// <typeparam name="T">命令对象的具体类型。</typeparam>
/// <remarks>
/// <para>
/// <b>命令对象是无状态的</b>：它只表达「要执行某种操作」，原则上不持有属性。执行所需的输入
/// 由工厂方法的参数（执行器的 <c>criteria</c>）带入，而不是先写进对象属性再校验——
/// 因此它不与变更追踪（<see cref="ObservableObject{T}"/> 的 <c>SetProperty</c>）耦合，
/// <see cref="BusinessObject.ChangedProperties"/> 恒为空。
/// </para>
/// <para>
/// 与之相应，<b>属性级规则对命令对象无从触发</b>（那是「某个字段值合不合格」的模型），
/// 命令的校验请写成<b>对象级</b>规则——它在命令体之前由工厂边界裁决，不通过则命令不执行。
/// </para>
/// </remarks>
public abstract class CommandObject<T> : BusinessObject<T>, ICommandObject
	where T : CommandObject<T>
{
	/// <summary>
	/// 执行命令。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步命令执行操作的任务。</returns>
	protected internal virtual Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 创建新的命令对象。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步创建操作的任务。</returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}