using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 按分区键对函数执行双窗口（分钟 + 小时）限流。
/// </summary>
internal sealed class RateLimitingMiddleware : IFunctionsWorkerMiddleware, IDisposable
{
	private readonly EuoniaFunctionsOptions _options;
	private readonly ILogger<RateLimitingMiddleware> _logger;
	private readonly MemoryCache _limiters = new(new MemoryCacheOptions());

	/// <summary>
	/// 初始化 <see cref="RateLimitingMiddleware"/>。
	/// </summary>
	/// <param name="options">函数宿主选项。</param>
	/// <param name="logger">日志记录器。</param>
	public RateLimitingMiddleware(EuoniaFunctionsOptions options, ILogger<RateLimitingMiddleware> logger)
	{
		_options = options;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		var httpContext = context.GetHttpContext();

		if (httpContext is null)
		{
			await next(context);
			return;
		}

		var key = _options.RateLimitPartitionKeySelector?.Invoke(httpContext);

		if (string.IsNullOrWhiteSpace(key))
		{
			key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		}

		// 每个函数独立计数，避免一个热点函数耗尽同一 IP 在其它函数上的配额。
		key = context.FunctionDefinition.Name + ":" + key;

		var limiter = _limiters.GetOrCreate(key, entry =>
		{
			entry.SlidingExpiration = TimeSpan.FromHours(1);
			entry.RegisterPostEvictionCallback(static (_, value, _, _) => (value as IDisposable)?.Dispose());
			return new DualWindowRateLimiter(_options.RateLimiting);
		});

		if (limiter is null)
		{
			await next(context);
			return;
		}

		using var lease = limiter.AttemptAcquire();

		if (!lease.IsAcquired)
		{
			_logger.LogWarning("Function {Function} rate limit exceeded for partition {Partition}.", context.FunctionDefinition.Name, key);

			var retryAfterSeconds = 1;

			if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
			{
				retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
			}

			httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			httpContext.Response.ContentType = "application/json";
			httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

			await httpContext.Response.WriteAsJsonAsync(new
			{
				code = StatusCodes.Status429TooManyRequests,
				message = "请求过于频繁，请稍后重试",
				retryAfterSeconds
			}, context.CancellationToken);

			return;
		}

		await next(context);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_limiters.Dispose();
	}
}
