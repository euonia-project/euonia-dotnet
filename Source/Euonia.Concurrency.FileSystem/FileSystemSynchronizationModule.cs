using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 文件系统同步模块，作为 <see cref="FileSystemLockModule"/> 的依赖标记，
/// 负责从配置节 <see cref="FileSynchronizationOptions.ConfigurationSection"/> 绑定 <see cref="FileSynchronizationOptions"/>。
/// </summary>
internal class FileSystemSynchronizationModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 模块自动绑定配置节，作为最低优先级的默认值：宿主无论在模块注册之前还是之后调用
        // Configure<FileSynchronizationOptions>，显式配置都会覆盖配置节与环境变量的值。
        context.Services.AddOptions<FileSynchronizationOptions>()
               .BindConfigurationIfAvailable(FileSynchronizationOptions.ConfigurationSection);
    }
}

/// <summary>
/// FileSystemLockModule 类包含用于配置文件系统锁的方法。
/// </summary>
[DependsOn(typeof(FileSystemSynchronizationModule))]
public class FileSystemLockModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ILockFactory>(provider =>
        {
            var directory = provider.GetService<IOptions<FileSynchronizationOptions>>()?.Value.Directory;

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException(
                    "FileSynchronizationOptions.Directory is not configured. " +
                    $"Set the '{FileSynchronizationOptions.ConfigurationSection}:Directory' configuration value " +
                    $"or the '{FileSynchronizationOptions.ConfigurationSection}__Directory' environment variable.");
            }

            return new FileSynchronizationFactory(new DirectoryInfo(directory));
        });
    }
}