using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 根据端点描述符创建运行时兜底的请求委托。
/// </summary>
internal static class WebApiEndpointFactory
{
	/// <summary>
	/// 创建请求委托。
	/// </summary>
	/// <param name="descriptor">端点描述符。</param>
	/// <param name="serviceProvider">根服务提供程序，用于读取 JSON 与日志选项。</param>
	/// <returns>请求委托。</returns>
	public static RequestDelegate Create(WebApiActionDescriptor descriptor, IServiceProvider serviceProvider)
	{
		var jsonOptions = serviceProvider.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions
		                  ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
		var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("Nerosoft.Euonia.Endpoint.Http") ?? NullLogger.Instance;

		return async context =>
		{
			var service = context.RequestServices.GetRequiredService(descriptor.ServiceType);
			var arguments = new object?[descriptor.Parameters.Count];

			for (var index = 0; index < descriptor.Parameters.Count; index++)
			{
				var binding = await WebApiParameterBinder.BindAsync(context, descriptor.Parameters[index], jsonOptions, context.RequestAborted).ConfigureAwait(false);
				if (!binding.Succeeded)
				{
					if (binding.IsInternal)
					{
						logger.LogError("Internal error while binding parameter '{Parameter}' for {Service}.{Method}: {Message}", binding.ErrorName, descriptor.ServiceType.FullName, descriptor.Method.Name, binding.ErrorMessage);
						await Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An error occurred while processing the request.").ExecuteAsync(context).ConfigureAwait(false);
					}
					else
					{
						logger.LogWarning("Invalid value for parameter '{Parameter}' for {Service}.{Method}: {Message}", binding.ErrorName, descriptor.ServiceType.FullName, descriptor.Method.Name, binding.ErrorMessage);
						await Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request parameter", detail: $"Invalid value for parameter '{binding.ErrorName}'.").ExecuteAsync(context).ConfigureAwait(false);
					}

					return;
				}

				arguments[index] = binding.Value;
			}

			object? result;
			try
			{
				result = descriptor.Method.Invoke(service, arguments);
			}
			catch (TargetInvocationException exception) when (exception.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
				return;
			}

			result = await AwaitResultAsync(result).ConfigureAwait(false);
			if (result is IResult httpResult)
			{
				await httpResult.ExecuteAsync(context).ConfigureAwait(false);
				return;
			}

			if (descriptor.ReturnsVoid)
			{
				context.Response.StatusCode = StatusCodes.Status204NoContent;
				return;
			}

			var json = JsonSerializer.Serialize(result, descriptor.ResultType ?? typeof(object), jsonOptions);
			context.Response.ContentType = "application/json; charset=utf-8";
			await context.Response.WriteAsync(json, context.RequestAborted).ConfigureAwait(false);
		};
	}

	private static async Task<object?> AwaitResultAsync(object? result)
	{
		if (result is null) return null;
		if (result is Task task)
		{
			await task.ConfigureAwait(false);
			return GetTaskResult(task);
		}

		if (result is ValueTask valueTask)
		{
			await valueTask.ConfigureAwait(false);
			return null;
		}

		var type = result.GetType();
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			var asTask = type.GetMethod("AsTask")!;
			var valueTaskAsTask = (Task)asTask.Invoke(result, null)!;
			await valueTaskAsTask.ConfigureAwait(false);
			return GetTaskResult(valueTaskAsTask);
		}

		return result;
	}

	private static object? GetTaskResult(Task task) => task.GetType().GetProperty("Result")?.GetValue(task);
}
