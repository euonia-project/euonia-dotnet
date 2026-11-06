using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Authentication;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 提供将异常映射为 HTTP 状态码、错误明细与错误消息的扩展方法。
/// </summary>
internal static class ExceptionExtensions
{
	/// <summary>
	/// 为 <see cref="Exception"/> 提供响应输出所需的转换成员。
	/// </summary>
	extension(Exception exception)
	{
		/// <summary>
		/// 根据异常类型获取对应的 HTTP 状态码。
		/// </summary>
		/// <returns>异常映射得到的 HTTP 状态码；无法识别时返回 <see cref="HttpStatusCode.InternalServerError"/>。</returns>
		public HttpStatusCode GetStatusCode()
		{
			return exception switch
			{
				HttpStatusException ex => ex.StatusCode,
				ValidationException => HttpStatusCode.BadRequest,
				Nerosoft.Euonia.Validation.ValidationException => HttpStatusCode.BadRequest,
				AuthenticationException => HttpStatusCode.Unauthorized,
				UnauthorizedAccessException => HttpStatusCode.Forbidden,
				NotImplementedException => HttpStatusCode.NotImplemented,
				TimeoutException => HttpStatusCode.GatewayTimeout,
				AggregateException aggEx => aggEx.InnerExceptions.Count == 0 ? HttpStatusCode.InternalServerError : aggEx.InnerExceptions[0].GetStatusCode(),
				TargetInvocationException ex => ex.InnerException?.GetStatusCode() ?? HttpStatusCode.InternalServerError,
				_ => exception.GetType().GetCustomAttribute<HttpStatusCodeAttribute>()?.StatusCode ?? HttpStatusCode.InternalServerError
			};
		}

		/// <summary>
		/// 提取验证异常中的字段错误明细。
		/// </summary>
		/// <returns>以属性名为键、错误消息数组为值的字典；异常不是验证异常时返回 <c>null</c>。</returns>
		public IReadOnlyDictionary<string, string?[]>? GetErrorDetails()
		{
			var ex = exception;
			while (true)
			{
				if (ex is AggregateException)
				{
					ex = ex.InnerException;
					continue;
				}

				if (ex is Nerosoft.Euonia.Validation.ValidationException euoniaValidationException)
				{
					return euoniaValidationException.Errors
					                                 .Where(result => !string.IsNullOrWhiteSpace(result.PropertyName))
					                                 .GroupBy(result => result.PropertyName)
					                                 .ToDictionary(group => group.Key, group => group.Select(result => (string?)result.ErrorMessage).ToArray());
				}

				if (ex is not ValidationException validationException)
				{
					return null;
				}

				return validationException.ValidationResult
				                          .MemberNames
				                          .ToDictionary(t => t, _ => new[]
				                          {
					                          validationException.ValidationResult.ErrorMessage
				                          });

				// return validationException.ValidationResult.MemberNames
				//                           .Distinct()
				//                           .ToDictionary(
				// 	                          t => t,
				// 	                          t => validationException.ValidationResult.MemberNames
				// 	                                                  .Where(x => (x ?? "_") == t)
				// 	                                                  .Select(x => validationException.ValidationResult.ErrorMessage)
				// 	                                                  .ToArray());
			}
		}

		/// <summary>
		/// 获取用于响应的错误消息，聚合异常与反射调用异常会递归取其内部异常的消息。
		/// </summary>
		/// <returns>最内层可用的错误消息；无内部异常时返回异常自身的消息。</returns>
		public string GetErrorMessage()
		{
			return exception switch
			{
				AggregateException ex => ex.InnerException == null ? ex.Message : ex.InnerException.GetErrorMessage(),
				TargetInvocationException ex => ex.InnerException == null ? ex.Message : ex.InnerException.GetErrorMessage(),
				_ => exception.Message
			};
		}
	}
}