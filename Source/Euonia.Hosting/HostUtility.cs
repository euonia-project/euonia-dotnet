using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 服务器主机构建工具类。
/// </summary>
public static class HostUtility
{
	/// <summary>
	/// 使用指定的选项委托运行启动实例。
	/// </summary>
	/// <param name="args">命令行参数。</param>
	/// <param name="optionsAction">用于配置 <see cref="HostBuilderOptions"/> 的委托。</param>
	/// <param name="createHostBuilder">自定义主机构建器工厂；为 <c>null</c> 时使用默认构建逻辑。</param>
	/// <typeparam name="TStartup">启动类型。</typeparam>
	public static void Run<TStartup>(string[] args, Action<HostBuilderOptions> optionsAction = null, Func<string[], HostBuilderOptions, IHostBuilder> createHostBuilder = null)
		where TStartup : class
	{
		var options = new HostBuilderOptions();
		optionsAction?.Invoke(options);
		Run<TStartup>(args, options, createHostBuilder);
	}

	/// <summary>
	/// 使用指定的选项运行启动实例。启用 HTTP/2 时设置相关开关，并使用默认或自定义的主机构建器构建并运行主机。
	/// </summary>
	/// <typeparam name="TStartup">启动类型。</typeparam>
	/// <param name="args">命令行参数。</param>
	/// <param name="options">主机配置选项。</param>
	/// <param name="createHostBuilder">自定义主机构建器工厂；为 <c>null</c> 时使用默认构建逻辑。</param>
	public static void Run<TStartup>(string[] args, HostBuilderOptions options, Func<string[], HostBuilderOptions, IHostBuilder> createHostBuilder = null)
		where TStartup : class
	{
		options ??= new HostBuilderOptions();
		if (options.EnableHttp2)
		{
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
		}

		createHostBuilder ??= CreateHostBuilder<TStartup>;

		var builder = createHostBuilder(args, options);

		options.ConfigureHostBuilder?.Invoke(builder);
		builder.Build().Run();
	}

	/// <summary>
	/// 使用指定的选项异步运行启动实例。启用 HTTP/2 时设置相关开关，并使用默认或自定义的主机构建器构建并运行主机。
	/// </summary>
	/// <param name="args">命令行参数。</param>
	/// <param name="optionsAction">用于配置 <see cref="HostBuilderOptions"/> 的委托。</param>
	/// <param name="createHostBuilder">自定义主机构建器工厂；为 <c>null</c> 时使用默认构建逻辑。</param>
	/// <typeparam name="TStartup">启动类型。</typeparam>
	/// <returns>表示异步操作的任务。</returns>
	public static Task RunAsync<TStartup>(string[] args, Action<HostBuilderOptions> optionsAction = null, Func<string[], HostBuilderOptions, IHostBuilder> createHostBuilder = null)
		where TStartup : class
	{
		var options = new HostBuilderOptions();
		optionsAction?.Invoke(options);
		return RunAsync<TStartup>(args, options, createHostBuilder);
	}

	/// <summary>
	/// 使用指定的选项异步运行启动实例。启用 HTTP/2 时设置相关开关，并使用默认或自定义的主机构建器构建并运行主机。
	/// </summary>
	/// <param name="args">命令行参数。</param>
	/// <param name="options">主机配置选项。</param>
	/// <param name="createHostBuilder">自定义主机构建器工厂；为 <c>null</c> 时使用默认构建逻辑。</param>
	/// <typeparam name="TStartup">启动类型。</typeparam>
	/// <returns>表示异步操作的任务。</returns>
	public static Task RunAsync<TStartup>(string[] args, HostBuilderOptions options, Func<string[], HostBuilderOptions, IHostBuilder> createHostBuilder = null)
		where TStartup : class
	{
		options ??= new HostBuilderOptions();
		if (options.EnableHttp2)
		{
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
		}

		createHostBuilder ??= CreateHostBuilder<TStartup>;

		var builder = createHostBuilder(args, options);

		options.ConfigureHostBuilder?.Invoke(builder);
		return builder.Build().RunAsync();
	}

	/// <summary>
	/// 使用指定的选项运行启动实例。启用 HTTP/2 时设置相关开关，并使用默认或自定义的主机构建器构建并运行主机。
	/// </summary>
	/// <typeparam name="TStartup">启动类型。</typeparam>
	/// <param name="args">命令行参数。</param>
	/// <param name="options">主机配置选项。</param>
	/// <returns>配置完成的 <see cref="IHostBuilder"/> 实例。</returns>
	private static IHostBuilder CreateHostBuilder<TStartup>(string[] args, HostBuilderOptions options)
		where TStartup : class
	{
		var host = Host.CreateDefaultBuilder(args);
		host = host.ConfigureServices((context, _) =>
		{
			Environment.SetEnvironmentVariable(HostBuilderOptions.ApplicationNameVariable, context.HostingEnvironment.ApplicationName);
		});

		host = host.ConfigureWebHostDefaults(builder =>
		{
			builder.UseStartup<TStartup>()
			       .CaptureStartupErrors(options.CaptureStartupErrors);

			options.ConfigureWebHostBuilder?.Invoke(builder);
		});

		return host;
	}
}