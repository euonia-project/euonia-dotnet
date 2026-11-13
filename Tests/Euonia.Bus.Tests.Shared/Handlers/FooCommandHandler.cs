using Nerosoft.Euonia.Bus.Tests.Commands;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 示例命令处理器，按通道订阅分别处理创建与删除命令。
/// </summary>
public class FooCommandHandler
{
	/// <summary>
	/// 处理 <see cref="FooCreateCommand"/> 并返回固定结果 <c>1</c>。
	/// </summary>
	/// <param name="message">待处理的创建命令。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>固定为 <c>1</c> 的处理结果。</returns>
	[Subscribe("foo.create")]
	public Task<int> HandleAsync(FooCreateCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		Console.WriteLine("FooCreateCommand handled");
		//messageContext.Response(1);
		//await Task.CompletedTask;
		return Task.FromResult(1);
	}

	/// <summary>
	/// 处理 <see cref="FooDeleteCommand"/> 并抛出 <see cref="NotFoundException"/>，用于验证异常传播。
	/// </summary>
	/// <param name="message">待处理的删除命令。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>处理任务；实际总是以异常结束。</returns>
	/// <exception cref="NotFoundException">总是抛出，模拟业务对象不存在。</exception>
	[Subscribe("foo.delete")]
	public async Task HandleAsync(FooDeleteCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		throw new NotFoundException();
	}
}