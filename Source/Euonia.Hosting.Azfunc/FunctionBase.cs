using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 函数基类，提供统一的日志记录器以及返回操作结果的辅助方法。
/// </summary>
/// <typeparam name="T">派生函数的类型，用于创建对应类型的日志记录器。</typeparam>
/// <param name="loggerFactory">用于创建类型日志记录器的工厂。</param>
public abstract class FunctionBase<T>(ILoggerFactory loggerFactory)
	where T : FunctionBase<T>
{
	private ILogger<T>? _logger;

	/// <summary>
	/// 获取当前函数类型对应的日志记录器。
	/// </summary>
	protected virtual ILogger<T> Logger => _logger ??= loggerFactory.CreateLogger<T>();

	/// <summary>
	/// 异步执行指定操作，并将返回值包装为 HTTP 200 结果。
	/// </summary>
	/// <typeparam name="TResult">操作返回的结果类型。</typeparam>
	/// <param name="action">要异步执行并返回结果的操作。</param>
	/// <returns>表示异步操作的任务，其结果为包含操作返回值的成功响应。</returns>
	protected virtual async Task<IActionResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
	{
		var result = await action();
		return Ok(result);
	}

	/// <summary>
	/// 异步执行指定操作，并返回无内容的 HTTP 200 结果。
	/// </summary>
	/// <param name="action">要异步执行的操作。</param>
	/// <returns>表示异步操作的任务，其结果为不包含内容的成功响应。</returns>
	protected virtual async Task<IActionResult> ExecuteAsync(Func<Task> action)
	{
		await action();
		return Ok();
	}

	/// <summary>
	/// 构造包含指定值的 HTTP 200 对象结果。
	/// </summary>
	/// <param name="value">要作为响应内容返回的对象。</param>
	/// <returns>封装指定值的对象结果。</returns>
	protected virtual IActionResult Ok(object? value)
	{
		return new OkObjectResult(value);
	}

	/// <summary>
	/// 构造无响应内容的 HTTP 200 结果。
	/// </summary>
	/// <returns>表示成功但不包含内容的结果。</returns>
	protected virtual IActionResult Ok()
	{
		return new OkResult();
	}
}
