using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 指示自动注册扫描忽略应用此特性的类型。
/// </summary>
/// <remarks>
/// 该特性仅影响约定式自动注册（<see cref="DefaultAutomaticRegistration"/>）；
/// 显式调用 <see cref="IServiceCollection"/> 的注册方法不受影响。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class DisableAutomaticRegistrationAttribute : Attribute
{
}
