using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus.Behaviors;

/// <summary>
/// 发件箱（Outbox）出站行为，在传输操作完成后更新发件箱存储中的发送状态。
/// </summary>
/// <typeparam name="TMessage">消息类型。</typeparam>
/// <typeparam name="TResult">结果类型。</typeparam>
internal sealed class OutgoingOutboxBehavior<TMessage, TResult> : IPipelineBehavior<IMessageEnvelope<TMessage>, TResult>
{
	private readonly IOutboxStore _store;
	private readonly string _transport;

	/// <summary>
	/// 初始化 <see cref="OutgoingOutboxBehavior{TMessage, TResult}"/> 类的新实例。
	/// </summary>
	/// <param name="store">发件箱存储。</param>
	/// <param name="transport">当前传输器的名称。</param>
	public OutgoingOutboxBehavior(IOutboxStore store, string transport)
	{
		_store = store;
		_transport = transport;
	}

	/// <inheritdoc/>
	public async Task<TResult> HandleAsync(IMessageEnvelope<TMessage> context, PipelineDelegate<IMessageEnvelope<TMessage>, TResult> next)
	{
		try
		{
			var result = await next(context);
			_store.MarkAsSuccess(context.MessageId, _transport);
			return result;
		}
		catch (Exception exception)
		{
			_store.MarkAsFailed(context.MessageId, _transport, exception.Message);
			throw;
		}
	}
}