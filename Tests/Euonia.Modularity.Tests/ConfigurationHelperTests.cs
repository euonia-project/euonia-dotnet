using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity.Tests;

/// <summary>
/// 针对 <see cref="ConfigurationHelper"/> 构建行为的测试。
/// </summary>
[Collection("EnvironmentVariables")]
public class ConfigurationHelperTests
{
    [Fact]
    public void BuildConfiguration_ShouldApplyBuilderActionAndEnvironmentName()
    {
        var key = $"EuoniaTest_{Guid.NewGuid():N}:Value";
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
                    [key] = "configured"
                }));

        Assert.Equal("configured", configuration[key]);
    }

    /// <summary>
    /// 环境变量源追加在 builderAction 之后，因此环境变量高于 builderAction 追加的配置源。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldPreferEnvironmentVariablesOverBuilderAction()
    {
        var key = $"EuoniaTest_{Guid.NewGuid():N}:BuilderAction";
        var environmentVariable = key.Replace(":", "__", StringComparison.Ordinal);
        var original = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, "from-environment");

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
                        [key] = "from-builder-action"
                    }));

            Assert.Equal("from-environment", configuration[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, original);
        }
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
