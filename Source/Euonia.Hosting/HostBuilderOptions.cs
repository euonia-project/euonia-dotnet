using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 用于构建服务器主机的配置选项。
/// </summary>
public class HostBuilderOptions
{
	/// <summary>
	/// 用于设置应用程序名称的环境变量名。
	/// </summary>
	public const string ApplicationNameVariable = "SERVICE_NAME";

	/// <summary>
	/// 获取或设置一个值，指示应用程序中是否启用 HTTP/2 协议。
	/// </summary>
	public bool EnableHttp2 { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示应用程序启动期间是否捕获异常。
	/// </summary>
	public bool CaptureStartupErrors { get; set; } = true;

	/// <summary>
	/// 获取或设置应用程序名称。
	/// </summary>
	public object ApplicationName { get; set; } = Assembly.GetEntryAssembly()?.GetName();

	/// <summary>
	/// 获取或设置用于处理 <see cref="IWebHostBuilder"/> 的操作。
	/// </summary>
	public Action<IWebHostBuilder> ConfigureWebHostBuilder { get; set; }

	/// <summary>
	/// 获取或设置用于处理 <see cref="IHostBuilder"/> 的操作。
	/// </summary>
	public Action<IHostBuilder> ConfigureHostBuilder { get; set; }
}