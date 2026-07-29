using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Authentication;

namespace Nerosoft.Euonia.Hosting;

internal static class ExceptionExtensions
{
	extension(Exception exception)
	{
		public HttpStatusCode GetStatusCode()
		{
			return exception switch
			{
				HttpStatusException ex => ex.StatusCode,
				ValidationException => HttpStatusCode.BadRequest,
				AuthenticationException => HttpStatusCode.Unauthorized,
				UnauthorizedAccessException => HttpStatusCode.Forbidden,
				NotImplementedException => HttpStatusCode.NotImplemented,
				TimeoutException => HttpStatusCode.GatewayTimeout,
				AggregateException aggEx => aggEx.InnerExceptions.Count == 0 ? HttpStatusCode.InternalServerError : aggEx.InnerExceptions[0].GetStatusCode(),
				TargetInvocationException ex => ex.InnerException?.GetStatusCode() ?? HttpStatusCode.InternalServerError,
				_ => exception.GetType().GetCustomAttribute<HttpStatusCodeAttribute>()?.StatusCode ?? HttpStatusCode.InternalServerError
			};
		}

		public IReadOnlyDictionary<string, string[]> GetErrorDetails()
		{
			var ex = exception;
			while (true)
			{
				if (ex is AggregateException)
				{
					ex = ex.InnerException;
					continue;
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