using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 设置依赖 <see cref="ValidationModule"/> 以导入对象验证服务。
/// </summary>
public class ValidationModule : ModuleContextBase
{
	/// <inheritdoc />
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddAutomaticRegistrar(new ValidationRegistrar());
		context.Services.AddSingleton<IValidatorFactory, DefaultValidatorFactory>();
	}

	/// <inheritdoc/>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		var factory = context.ServiceProvider.GetService<IValidatorFactory>();
		if (factory != null)
		{
			ValidatorFactory.SetCurrent(factory);
		}
	}
}