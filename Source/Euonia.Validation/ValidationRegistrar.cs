using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// This class is used by the <see cref="ValidationModule"/> to register the validation services.
/// </summary>
public class ValidationRegistrar : DefaultAutomaticRegistration
{
    /// <inheritdoc />
    protected override bool IsAutomaticRegistrationDisabled(Type type)
    {
        return !type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValidator<>)) ||
               base.IsAutomaticRegistrationDisabled(type);
    }

    /// <inheritdoc />
    protected override ServiceLifetime? GetDefaultLifeTimeOfType(Type type)
    {
        return ServiceLifetime.Transient;
    }

    /// <inheritdoc />
    protected override List<Type> GetExposedServiceTypes(Type type)
    {
        return new List<Type>
        {
            typeof(IValidator<>).MakeGenericType(GetFirstGenericArgument(type, 1))
        };
    }

    private static Type GetFirstGenericArgument(Type type, int depth)
    {
        const int maxFindDepth = 8;

        if (type == null || depth >= maxFindDepth)
        {
            return null;
        }

        if (type.IsGenericType && type.GetGenericArguments().Length >= 1)
        {
            return type.GetGenericArguments()[0];
        }

        // 直接实现 IValidator<T> 的类型没有泛型基类（如 AbstractValidator<T>），
        // 若只沿 BaseType 向上查找会走到 null。
        var validatorInterface = type.GetInterfaces()
                                     .FirstOrDefault(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IValidator<>));
        if (validatorInterface != null)
        {
            return validatorInterface.GetGenericArguments()[0];
        }

        return GetFirstGenericArgument(type.BaseType, depth + 1);
    }
}