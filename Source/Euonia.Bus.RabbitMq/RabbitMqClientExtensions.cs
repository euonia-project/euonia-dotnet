using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 针对 RabbitMQ <see cref="IChannel"/> 的扩展辅助方法集合，用于以被动方式探测队列状态。
/// </summary>
internal static class RabbitMqClientExtensions
{
	/// <param name="channel">目标 RabbitMQ 通道。</param>
	extension(IChannel channel)
	{
		/// <summary>
		/// 检查队列是否存在。
		/// </summary>
		/// <param name="queueName">要检查的队列名称。</param>
		/// <returns>队列存在且至少有一个消费者时为 <see langword="true"/>；队列不存在（回复码 404）时为 <see langword="false"/>。</returns>
		public async Task<bool> ExistsQueueAsync(string queueName)
		{
			//var queueDeclareOk = channel.QueueDeclarePassive(queueName);
			//if (queueDeclareOk.MessageCount > 0)
			//{
			//	throw new InvalidOperationException($"Queue '{queueName}' is not empty.");
			//}

			try
			{
				var queueDeclare = await channel.QueueDeclarePassiveAsync(queueName);

				return queueDeclare?.ConsumerCount > 0;
			}
			catch (OperationInterruptedException exception)
			{
				if (exception.ShutdownReason?.ReplyCode == 404)
				{
					return false;
					//throw new InvalidOperationException("No consumer found for the channel.", exception);
				}

				throw;
			}
		}

		/// <summary>
		/// 以被动方式声明队列，并返回声明结果。
		/// </summary>
		/// <param name="queueName">要声明的队列名称。</param>
		/// <returns>队列声明结果；队列不存在（回复码 404）时为 <see langword="null"/>。</returns>
		/// <exception cref="OperationInterruptedException">被动声明失败且回复码不是 404。</exception>
		public async Task<QueueDeclareOk?> DeclareQueuePassivelyAsync(string queueName)
		{
			try
			{
				return await channel.QueueDeclarePassiveAsync(queueName);
			}
			catch (OperationInterruptedException exception)
			{
				if (exception.ShutdownReason?.ReplyCode == 404)
				{
					return null;
					//throw new InvalidOperationException("No consumer found for the channel.", exception);
				}

				throw;
			}
		}
	}
}