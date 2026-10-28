using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 承载一个宿主容器，并为每次消息处理创建执行作用域。
/// </summary>
/// <remarks>
/// 所有处理器调用路径（处理器上下文、收件箱重试、发件箱重投递）都通过本类型执行。
/// 执行作用域负责服务提供程序的获取/复用与请求上下文安装，宿主组件本身不捕获 scoped 服务。
/// </remarks>
internal sealed class MessageExecutionEnvironment
{
	private readonly IServiceProvider _container;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger _logger;

	/// <summary>
	/// 使用宿主容器创建执行环境。
	/// </summary>
	/// <param name="container">宿主容器，用于创建作用域并识别“复用了根容器”。</param>
	/// <param name="logger">可选日志记录器。</param>
	internal MessageExecutionEnvironment(IServiceProvider container, ILogger logger)
	{
		ArgumentNullException.ThrowIfNull(container);

		_container = container;
		_scopeFactory = container.GetRequiredService<IServiceScopeFactory>();
		_logger = logger;
	}

	/// <summary>
	/// 在描述符指定的执行环境中运行处理器，并返回结果。
	/// </summary>
	/// <typeparam name="TResult">处理结果类型。</typeparam>
	/// <param name="descriptor">执行环境描述符。</param>
	/// <param name="message">消息上下文；出站重投递等无入站消息的场景可为 <c>null</c>。</param>
	/// <param name="execute">使用实际服务提供程序执行的处理委托。</param>
	/// <returns>处理结果。</returns>
	internal async Task<TResult> RunAsync<TResult>(
		MessageExecutionDescriptor descriptor,
		MessageContext message,
		Func<IServiceProvider, Task<TResult>> execute)
	{
		ArgumentNullException.ThrowIfNull(execute);

		await using var scope = new MessageExecutionScope(_container, _scopeFactory, descriptor, message, _logger);
		return await execute(scope.ServiceProvider).ConfigureAwait(false);
	}

	/// <summary>
	/// 在描述符指定的执行环境中运行处理器。
	/// </summary>
	/// <param name="descriptor">执行环境描述符。</param>
	/// <param name="message">消息上下文；出站重投递等无入站消息的场景可为 <c>null</c>。</param>
	/// <param name="execute">使用实际服务提供程序执行的处理委托。</param>
	/// <returns>表示处理操作的任务。</returns>
	internal async Task RunAsync(
		MessageExecutionDescriptor descriptor,
		MessageContext message,
		Func<IServiceProvider, Task> execute)
	{
		ArgumentNullException.ThrowIfNull(execute);

		await using var scope = new MessageExecutionScope(_container, _scopeFactory, descriptor, message, _logger);
		await execute(scope.ServiceProvider).ConfigureAwait(false);
	}
}
