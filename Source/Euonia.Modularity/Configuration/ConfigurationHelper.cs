using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供按约定构建应用程序配置的辅助方法。
/// </summary>
public static class ConfigurationHelper
{
    private const string DevelopmentEnvironmentName = "Development";

    /// <summary>
    /// 加载 JSON 配置文件、环境变量、用户机密与命令行参数，构建应用程序配置。
    /// </summary>
    /// <param name="options">配置构建选项；为 <see langword="null"/> 时使用默认选项（当前目录下的 <c>appsettings.json</c>）。</param>
    /// <param name="builderAction">在构建前对 <see cref="IConfigurationBuilder"/> 进行额外配置的可选委托。</param>
    /// <returns>构建完成的配置根。</returns>
    public static IConfigurationRoot BuildConfiguration(ConfigurationBuilderOptions? options = null, Action<IConfigurationBuilder>? builderAction = null)
    {
        options ??= new ConfigurationBuilderOptions();

        var basePath = string.IsNullOrWhiteSpace(options.BasePath)
            ? Directory.GetCurrentDirectory()
            : options.BasePath!;

        var environmentName = ResolveEnvironmentName(options.EnvironmentName);

        var builder = new ConfigurationBuilder()
                      .SetBasePath(basePath)
                      .AddJsonFile($"{options.FileName}.json", optional: true, reloadOnChange: options.ReloadOnChange);

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.AddJsonFile($"{options.FileName}.{environmentName}.json", optional: true, reloadOnChange: options.ReloadOnChange);
        }

        if (string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(options.UserSecretsId))
            {
                builder.AddUserSecrets(options.UserSecretsId!);
            }
            else if (options.UserSecretsAssembly != null)
            {
                builder.AddUserSecrets(options.UserSecretsAssembly, true);
            }
        }

        builder.AddEnvironmentVariables(options.EnvironmentVariablesPrefix);

        if (options.CommandLineArgs is { Length: > 0 })
        {
            builder.AddCommandLine(options.CommandLineArgs);
        }

        builderAction?.Invoke(builder);

        return builder.Build();
    }

    private static string? ResolveEnvironmentName(string? configuredEnvironment)
    {
        if (!string.IsNullOrWhiteSpace(configuredEnvironment))
        {
            return configuredEnvironment;
        }

        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    }
}
