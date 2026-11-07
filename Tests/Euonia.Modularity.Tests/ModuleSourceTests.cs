using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class ModuleSourceTests
{
    [Fact]
    public void FileModuleSource_ShouldDiscoverModuleTypes()
    {
        var source = new FileModuleSource(typeof(PluginModule).Assembly.Location);

        var modules = source.GetModules().ToArray();

        Assert.Contains(typeof(PluginModule), modules);
        Assert.Contains(typeof(StartupModule), modules);
    }

    [Fact]
    public void ApplicationCreationOptions_ShouldAddFolderAndFileSources()
    {
        var options = new ApplicationCreationOptions(new ServiceCollection());

        options.AddModuleFolder(AppContext.BaseDirectory);
        options.AddModuleFiles(typeof(PluginModule).Assembly.Location);

        Assert.Equal(2, options.ModuleSources.Count);
        Assert.IsType<FolderModuleSource>(options.ModuleSources[0]);
        Assert.IsType<FileModuleSource>(options.ModuleSources[1]);
    }
}
