namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息执行作用域参与者：在处理器执行前后安装/恢复环境（用户、租户、审计等）。
/// </summary>
public interface IMessageExecutionScopeParticipant
{
	/// <summary>
	/// 获取进入顺序；数字越小越先进入，释放时逆序。
	/// </summary>
	int Order { get; }

	/// <summary>
	/// 进入消息执行作用域。
	/// </summary>
	/// <param name="message">当前消息上下文。</param>
	/// <param name="services">本次处理作用域的服务提供程序。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>租约与裁决；返回 <see cref="MessageExecutionScopeAction.Skip"/> 或 <see cref="MessageExecutionScopeAction.DeadLetter"/> 时不再执行处理器。</returns>
	/// <remarks>
	/// <para>
	/// 本方法在 <c>await</c> 链中执行，直接写入 <see cref="System.Threading.AsyncLocal{T}"/>（执行上下文、租户、用户）
	/// 不会回流到调用方，因此参与者必须通过 <see cref="MessageExecutionScopeEntry.Install"/> 返回同步安装委托，
	/// 由 <c>MessageExecutionScope</c> 在调用方执行流中同步调用；委托返回的租约负责在作用域释放时恢复。
	/// </para>
	/// <para>
	/// 只有解析/校验等异步工作放在本方法内；任何环境安装都在 <see cref="MessageExecutionScopeEntry.Install"/> 中完成。
	/// </para>
	/// </remarks>
	ValueTask<MessageExecutionScopeEntry> EnterAsync(IMessageContext message, IServiceProvider services, CancellationToken cancellationToken = default);
}
