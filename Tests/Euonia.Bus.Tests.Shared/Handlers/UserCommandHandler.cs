using Nerosoft.Euonia.Bus.Tests.Commands;

namespace Nerosoft.Euonia.Bus.Tests.Handlers
{
	/// <summary>
	/// 示例命令处理器，同时实现带响应与不带响应的命令处理。
	/// </summary>
	public class UserCommandHandler : IScopedDependency, IHandler<UserCreateCommand, int>, IHandler<UserUpdateCommand>
	{
		/// <summary>
		/// 判断处理器是否能处理指定消息类型；此示例始终返回 <c>true</c>。
		/// </summary>
		/// <param name="messageType">待判断的消息类型。</param>
		/// <returns>始终为 <c>true</c>。</returns>
		public bool CanHandle(Type messageType)
		{
			return true;
		}

		/// <summary>
		/// 处理 <see cref="UserCreateCommand"/> 并返回固定结果 <c>1</c>。
		/// </summary>
		/// <param name="message">待处理的创建命令。</param>
		/// <param name="messageContext">消息上下文。</param>
		/// <param name="cancellationToken">取消令牌。</param>
		/// <returns>固定为 <c>1</c> 的处理结果。</returns>
		public Task<int> HandleAsync(UserCreateCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
		{
			Console.WriteLine("UserCreateCommand handled");

			return Task.FromResult(1);
		}

		/// <summary>
		/// 处理 <see cref="UserUpdateCommand"/>，不返回处理结果。
		/// </summary>
		/// <param name="message">待处理的更新命令。</param>
		/// <param name="messageContext">消息上下文。</param>
		/// <param name="cancellationToken">取消令牌。</param>
		/// <returns>表示处理完成的任务。</returns>
		public async Task HandleAsync(UserUpdateCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
		{
			Console.WriteLine("UserUpdateCommand handled");
			await Task.CompletedTask;
		}
	}
}