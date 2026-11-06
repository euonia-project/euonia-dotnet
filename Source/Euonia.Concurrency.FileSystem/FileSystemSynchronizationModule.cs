using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 文件系统同步模块，作为 <see cref="FileSystemLockModule"/> 的依赖标记，本身不注册任何服务。
/// </summary>
internal class FileSystemSynchronizationModule : ModuleContextBase
{
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
            var options = provider.GetService<IOptions<FileSynchronizationOptions>>()!.Value;
            return new FileSynchronizationFactory(new DirectoryInfo(options.Directory));
        });
    }
}