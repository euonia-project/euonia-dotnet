using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity.Tests;

public class ConfigurationHelperTests
{
    [Fact]
    public void BuildConfiguration_ShouldApplyBuilderActionAndEnvironmentName()
    {
        var configuration = ConfigurationHelper.BuildConfiguration(
            new ConfigurationBuilderOptions
            {
                FileName = "does-not-exist",
                EnvironmentName = "Testing",
                ReloadOnChange = false
            },
            builder => builder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Sample:Value"] = "configured"
                }));

        Assert.Equal("configured", configuration["Sample:Value"]);
    }

    [Fact]
    public void BuildConfiguration_ShouldNotMutateOptions()
    {
        var options = new ConfigurationBuilderOptions
        {
            FileName = "does-not-exist",
            ReloadOnChange = false
        };

        ConfigurationHelper.BuildConfiguration(options);

        Assert.Null(options.BasePath);
    }
}
