using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Concurrency.FileSystem;

internal class FileSystemSynchronizationModule : ModuleContextBase
{
}

/// <summary>
/// The FileSystemLockModule class contains methods used to configure the file system lock.
/// </summary>
[DependsOn(typeof(FileSystemSynchronizationModule))]
public class FileSystemLockModule : ModuleContextBase
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ILockFactory>(provider =>
        {
            var options = provider.GetService<IOptions<FileSynchronizationOptions>>().Value;
            return new FileSynchronizationFactory(new DirectoryInfo(options.Directory));
        });
    }
}