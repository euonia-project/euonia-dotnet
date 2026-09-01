using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 为 <see cref="ApplicationInitializationContext"/> 提供扩展方法的静态类。
/// 
/// 这些扩展方法用于从初始化上下文的 <see cref="IServiceProvider"/> 中检索常用的 ASP.NET Core 服务实例，
/// 例如 <see cref="IApplicationBuilder"/>、<see cref="IWebHostEnvironment"/> 和 <see cref="IConfiguration"/>。
/// 将查找逻辑集中到此处可以简化调用方代码并提高可读性。
/// </summary>
public static class ApplicationInitializationContextExtensions
{
	/// <summary>
	/// 为 <see cref="ApplicationInitializationContext"/> 提供扩展方法。
	/// </summary>
	/// <param name="context">应用程序初始化上下文；包含服务提供器。</param>
	extension(ApplicationInitializationContext context)
	{
		/// <summary>
		/// 从 <paramref name="context"/> 的服务容器中获取 <see cref="IApplicationBuilder"/> 实例。
		/// </summary>
		/// <returns>已解析的 <see cref="IApplicationBuilder"/> 实例。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="context"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="InvalidOperationException">
		/// 当未在容器中注册 <see cref="IObjectAccessor{IApplicationBuilder}"/> 或其 Value 为 <c>null</c> 时，DI 将抛出异常。
		/// </exception>
		/// <remarks>
		/// 该方法通过 <see cref="IObjectAccessor{T}"/> 包装器来访问 <see cref="IApplicationBuilder"/>。
		/// 使用此模式可以将由框架或宿主创建的 IApplicationBuilder 注册到 DI 中，并在需要时从容器中检索。
		/// </remarks>
		public IApplicationBuilder GetApplicationBuilder()
		{
			return context.ServiceProvider.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value;
		}

		/// <summary>
		/// 从 <paramref name="context"/> 的服务容器中获取必需的 <see cref="IWebHostEnvironment"/> 实例。
		/// </summary>
		/// <returns>已解析的 <see cref="IWebHostEnvironment"/> 实例。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="context"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="InvalidOperationException">当未在容器中注册 <see cref="IWebHostEnvironment"/> 时由 DI 抛出。</exception>
		/// <remarks>
		/// <see cref="IWebHostEnvironment"/> 提供当前运行环境信息（例如 EnvironmentName、ContentRootPath 等），
		/// 在应用初始化阶段经常用于根据环境选择不同的配置或行为。
		/// </remarks>
		public IWebHostEnvironment GetEnvironment()
		{
			return context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
		}

		/// <summary>
		/// 尝试从 <paramref name="context"/> 的服务容器中获取 <see cref="IWebHostEnvironment"/>，若未注册则返回 <c>null</c>。
		/// </summary>
		/// <returns>
		/// 已解析的 <see cref="IWebHostEnvironment"/> 实例，或者当容器中未注册该服务时返回 <c>null</c>。
		/// </returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="context"/> 为 <c>null</c> 时抛出。</exception>
		/// <remarks>
		/// 与 <see cref="GetEnvironment(ApplicationInitializationContext)"/> 不同，本方法不会在服务缺失时抛出异常，
		/// 适用于初始化的早期阶段或测试场景中环境服务可能不存在的情况。
		/// </remarks>
		public IWebHostEnvironment GetEnvironmentOrNull()
		{
			return context.ServiceProvider.GetService<IWebHostEnvironment>();
		}

		/// <summary>
		/// 从 <paramref name="context"/> 的服务容器中获取必需的 <see cref="IConfiguration"/> 实例。
		/// </summary>
		/// <returns>已解析的 <see cref="IConfiguration"/> 实例。</returns>
		/// <exception cref="ArgumentNullException">当 <paramref name="context"/> 为 <c>null</c> 时抛出。</exception>
		/// <exception cref="InvalidOperationException">当未在容器中注册 <see cref="IConfiguration"/> 时由 DI 抛出。</exception>
		/// <remarks>
		/// 返回的配置对象用于读取应用程序设置（例如 appsettings.json、环境变量等），
		/// 在应用初始化时常用于读取连接字符串、功能开关或其他配置项。
		/// </remarks>
		public IConfiguration GetConfiguration()
		{
			return context.ServiceProvider.GetRequiredService<IConfiguration>();
		}
	}
}