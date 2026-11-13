using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Concurrency.FileSystem;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.FileSystem.Tests;

/// <summary>
/// 串行执行所有读写进程环境变量的测试，避免同一程序集内的环境变量相互串扰。
/// </summary>
[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;

/// <summary>
/// 验证 <see cref="FileSystemLockModule"/> 依赖的 <see cref="FileSystemSynchronizationModule"/>
/// 自动绑定 <c>FileSynchronization</c> 配置节，以及未配置目录时的可读错误。
/// </summary>
[Collection("EnvironmentVariables")]
public sealed class FileSystemOptionsConfigurationTests
{
    private const string ConfigurationKey = "FileSynchronization:Directory";
    private const string EnvironmentVariableName = "FileSynchronization__Directory";

    /// <summary>
    /// <c>FileSynchronization__Directory</c> 应覆盖初始配置中的值，且模块能据此创建 <see cref="ILockFactory"/>。
    /// </summary>
    [Fact]
    public void FileSystemLockModule_ShouldBindDirectoryFromEnvironmentVariable()
    {
        var environmentDirectory = Path.Combine(Path.GetTempPath(), $"euonia-locks-{Guid.NewGuid():N}");
        var original = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, environmentDirectory);

            var startupConfiguration = new ConfigurationBuilder()
                                       .AddInMemoryCollection(
                                           new Dictionary<string, string?>
                                           {
                                               [ConfigurationKey] = Path.Combine(Path.GetTempPath(), "euonia-locks-from-config")
                                           })
                                       .Build();

            var services = new ServiceCollection();
            var application = ApplicationFactory.Create<FileSystemLockModule>(services, startupConfiguration);

            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<FileSynchronizationOptions>>().Value;
            var factory = provider.GetRequiredService<ILockFactory>();

            Assert.Equal(environmentDirectory, options.Directory);
            Assert.IsType<FileSynchronizationFactory>(factory);

            // 初始配置本身未被修改，环境变量是在包装后的分层视图中生效。
            Assert.Equal(
                Path.Combine(Path.GetTempPath(), "euonia-locks-from-config"),
                startupConfiguration[ConfigurationKey]);
            Assert.Equal(environmentDirectory, application.Configuration[ConfigurationKey]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
        }
    }

    /// <summary>
    /// 未配置 <c>FileSynchronization:Directory</c> 时，解析 <see cref="ILockFactory"/> 应抛出
    /// 包含配置键与环境变量名的可读 <see cref="InvalidOperationException"/>。
    /// </summary>
    [Fact]
    public void FileSystemLockModule_WhenDirectoryNotConfigured_ShouldThrowReadableException()
    {
        var original = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, null);

            var services = new ServiceCollection();
            ApplicationFactory.Create<FileSystemLockModule>(services, new ConfigurationBuilder().Build());

            using var provider = services.BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ILockFactory>());

            Assert.Contains("FileSynchronization:Directory", exception.Message);
            Assert.Contains("FileSynchronization__Directory", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
        }
    }

    /// <summary>
    /// 宿主在模块注册之后显式配置的选项仍应优先于配置节与环境变量。
    /// </summary>
    [Fact]
    public void FileSystemLockModule_ExplicitConfigureShouldWinOverConfiguration()
    {
        var environmentDirectory = Path.Combine(Path.GetTempPath(), $"euonia-locks-{Guid.NewGuid():N}");
        var explicitDirectory = Path.Combine(Path.GetTempPath(), $"euonia-locks-explicit-{Guid.NewGuid():N}");
        var original = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, environmentDirectory);

            var services = new ServiceCollection();
            ApplicationFactory.Create<FileSystemLockModule>(services, new ConfigurationBuilder().Build());
            services.Configure<FileSynchronizationOptions>(options => options.Directory = explicitDirectory);

            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<FileSynchronizationOptions>>().Value;
            var factory = provider.GetRequiredService<ILockFactory>();

            Assert.Equal(explicitDirectory, options.Directory);
            Assert.IsType<FileSynchronizationFactory>(factory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
        }
    }

    /// <summary>
    /// 在 <c>ApplicationFactory.Create&lt;FileSystemLockModule&gt;</c> **之前**注册的显式配置也应优先于
    /// 模块自动绑定与环境变量（M1 回归）。
    /// </summary>
    [Fact]
    public void FileSystemLockModule_ExplicitConfigureRegisteredBeforeModuleShouldWin()
    {
        var environmentDirectory = Path.Combine(Path.GetTempPath(), $"euonia-locks-{Guid.NewGuid():N}");
        var explicitDirectory = Path.Combine(Path.GetTempPath(), $"euonia-locks-explicit-{Guid.NewGuid():N}");
        var original = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, environmentDirectory);

            var services = new ServiceCollection();
            services.Configure<FileSynchronizationOptions>(options => options.Directory = explicitDirectory);
            ApplicationFactory.Create<FileSystemLockModule>(services, new ConfigurationBuilder().Build());

            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<FileSynchronizationOptions>>().Value;
            var factory = provider.GetRequiredService<ILockFactory>();

            Assert.Equal(explicitDirectory, options.Directory);
            Assert.IsType<FileSynchronizationFactory>(factory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, original);
        }
    }
}
