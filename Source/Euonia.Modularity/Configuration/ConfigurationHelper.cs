using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 提供按约定构建应用程序配置的辅助方法。
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// 加载 JSON 配置文件、环境变量、用户机密与命令行参数，构建应用程序配置。
    /// </summary>
    /// <param name="options">配置构建选项；为 <see langword="null"/> 时使用默认选项（当前目录下的 <c>appsettings.json</c>）。</param>
    /// <param name="builderAction">在构建前对 <see cref="IConfigurationBuilder"/> 进行额外配置的可选委托。</param>
    /// <returns>构建完成的配置根。</returns>
    public static IConfigurationRoot BuildConfiguration(ConfigurationBuilderOptions? options = null, Action<IConfigurationBuilder>? builderAction = null)
    {
        options ??= new ConfigurationBuilderOptions();

        if (options.BasePath == null || options.BasePath.IsNullOrEmpty())
        {
            options.BasePath = Directory.GetCurrentDirectory();
        }

        var builder = new ConfigurationBuilder()
                      .SetBasePath(options.BasePath!)
                      .AddJsonFile(options.FileName + ".json", optional: true, reloadOnChange: true);

        if (options.EnvironmentName != null && !options.EnvironmentName.IsNullOrEmpty())
        {
            builder = builder.AddJsonFile($"{options.FileName}.{options.EnvironmentName}.json", optional: true, reloadOnChange: true);
        }

        if (options.EnvironmentName == "Development")
        {
            if (options.UserSecretsId != null)
            {
                builder.AddUserSecrets(options.UserSecretsId);
            }
            else if (options.UserSecretsAssembly != null)
            {
                builder.AddUserSecrets(options.UserSecretsAssembly, true);
            }
        }

        builder = builder.AddEnvironmentVariables(options.EnvironmentVariablesPrefix);

        if (options.CommandLineArgs != null)
        {
            builder = builder.AddCommandLine(options.CommandLineArgs);
        }

        builderAction?.Invoke(builder);

        return builder.Build();
    }
}