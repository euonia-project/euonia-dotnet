using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 为 <see cref="IApplicationBuilder"/> 和 <see cref="IServiceCollection"/> 提供流控中间件扩展。
/// </summary>
public static class RateLimitingApplicationBuilderExtensions
{
	/// <summary>
	/// 启用 ASP.NET Core 限流中间件；需先调用 <c>AddEuoniaRateLimiting</c> 或 <c>AddRateLimiter</c>。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>传入的应用构建器，以便链式调用。</returns>
	public static IApplicationBuilder UseEuoniaRateLimiting(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);
		return app.UseRateLimiter();
	}

	/// <summary>
	/// 注册 ASP.NET Core 限流中间件，并按客户端 IP 配置默认的双窗口（分钟 + 小时）策略。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">可选的流控配置回调。</param>
	/// <returns>传入的服务集合，以便链式调用。</returns>
	public static IServiceCollection AddEuoniaRateLimiting(this IServiceCollection services, Action<EuoniaRateLimitingOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		var options = new EuoniaRateLimitingOptions();
		configure?.Invoke(options);
		ArgumentNullException.ThrowIfNull(options.DefaultLimiter);
		ArgumentOutOfRangeException.ThrowIfNullOrWhiteSpace(options.PolicyName);
		options.DefaultLimiter.Validate();

		// 分区限流器由 MemoryCache 承载并在驱逐时 Dispose，避免按 IP 分区时自动补充定时器永久泄漏；
		// 注册为单例实例，容器释放时统一清理。
		var store = new DualWindowRateLimiterStore(options.DefaultLimiter);
		services.AddSingleton(store);

		services.AddSingleton(options);
		services.AddRateLimiter(limiter =>
		{
			limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			limiter.OnRejected = async (context, cancellationToken) =>
			{
				var response = context.HttpContext.Response;

				response.ContentType = "application/json";
				response.StatusCode = StatusCodes.Status429TooManyRequests;

				int? retryAfterSeconds = null;

				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
				{
					retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
					response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
				}

				await response.WriteAsJsonAsync(new
				{
					code = StatusCodes.Status429TooManyRequests,
					message = "请求过于频繁，请稍后重试",
					retryAfterSeconds
				}, cancellationToken);
			};

			if (options.EnableDefaultPolicy)
			{
				limiter.AddPolicy(options.PolicyName, httpContext =>
				{
					var key = options.PartitionKeySelector?.Invoke(httpContext);

					if (string.IsNullOrWhiteSpace(key))
					{
						key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
					}

					// 框架会缓存分区并在空闲驱逐后重新调用工厂；返回共享存储支撑的包装限流器，
					// 保证同一分区计数不因框架驱逐而重置，真实限流器的生命周期由存储管理。
					return RateLimitPartition.Get(key, _ => new StoreBackedRateLimiter(store, key));
				});
			}
		});

		return services;
	}
}
