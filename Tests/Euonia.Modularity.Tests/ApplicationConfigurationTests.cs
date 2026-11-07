using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity.Tests;

public class ApplicationConfigurationTests
{
    [Fact]
    public void Create_ShouldUseExplicitConfiguration()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["Sample:Value"] = "explicit"
                                })
                            .Build();

        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services, configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Same(configuration, application.Configuration);
        Assert.Same(configuration, provider.GetRequiredService<IConfiguration>());
        Assert.Equal("explicit", application.Configuration["Sample:Value"]);
    }

    [Fact]
    public void Create_ShouldBuildConfigurationWhenNotProvided()
    {
        var services = new ServiceCollection();
        var application = ApplicationFactory.Create<LifecycleModule>(services);

        Assert.NotNull(application.Configuration);
    }
}
