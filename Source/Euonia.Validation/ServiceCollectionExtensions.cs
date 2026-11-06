using Nerosoft.Euonia.Validation;

// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> 实例中设置对象验证服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 将对象验证器添加到指定的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />。
	/// </summary>
	/// <param name="services">要添加验证器服务的服务集合。</param>
	/// <param name="types">要注册的验证器类型集合；为 <see langword="null"/> 或为空时不注册任何验证器。</param>
	public static void AddValidator(this IServiceCollection services, IEnumerable<Type> types)
	{
		try
		{
			if (types == null || !types.Any())
			{
				return;
			}

			var validatorImplements = types.Where(type => typeof(FluentValidation.IValidator).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract).ToList();

			foreach (var validatorType in validatorImplements)
			{
				var inheritedType = validatorType.GetInterfaces().FirstOrDefault(t => t.IsGenericType);
				if (inheritedType == null)
				{
					continue;
				}

				if (inheritedType.GenericTypeArguments.Length != 1)
				{
					continue;
				}

				var objectType = inheritedType.GenericTypeArguments[0];
				if (!objectType.IsClass || objectType.IsAbstract || objectType.IsEnum)
				{
					continue;
				}

				var interfaceType = typeof(FluentValidation.IValidator<>).MakeGenericType(objectType);
				services.AddSingleton(interfaceType, validatorType);
			}

			services.AddSingleton<IValidatorFactory, DefaultValidatorFactory>();
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			throw;
		}
	}

	/// <summary>
	/// 将对象验证器添加到指定的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />。
	/// </summary>
	/// <param name="services">要添加验证器服务的服务集合。</param>
	/// <param name="factory">延迟提供验证器类型集合的工厂委托；返回 <see langword="null"/> 或空集合时不注册任何验证器。</param>
	public static void AddValidator(this IServiceCollection services, Func<IEnumerable<Type>> factory)
	{
		var types = factory?.Invoke();
		if (types == null || !types.Any())
		{
			return;
		}

		services.AddValidator(types);
	}
}