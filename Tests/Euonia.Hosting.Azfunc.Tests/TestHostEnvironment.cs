using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 测试用的宿主环境存根。
/// </summary>
/// <param name="environmentName">环境名称。</param>
internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
{
	/// <inheritdoc />
	public string EnvironmentName { get; set; } = environmentName;

	/// <inheritdoc />
	public string ApplicationName { get; set; } = "Euonia.Hosting.Azfunc.Tests";

	/// <inheritdoc />
	public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

	/// <inheritdoc />
	public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
