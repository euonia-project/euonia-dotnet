using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 
/// </summary>
public class ServiceConfigurationContext
{
    /// <summary>
    /// 
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 
    /// </summary>
    public IDictionary<string, object> Items { get; }

    /// <summary>
    /// Gets/sets arbitrary named objects those can be stored during
    /// the service registration phase and shared between modules.
    ///
    /// This is a shortcut usage of the <see cref="Items"/> dictionary.
    /// Returns null if given key is not found in the <see cref="Items"/> dictionary.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object? this[string key]
    {
        get => Items.GetOrDefault(key);
        set => Items[key] = value!;
    }

    /// <summary>
    /// Initialize new instance of <see cref="ServiceConfigurationContext"/>.
    /// </summary>
    /// <param name="services"></param>
    public ServiceConfigurationContext(IServiceCollection services)
    {
        Services = Check.EnsureNotNull(services, nameof(services));
        Items = new Dictionary<string, object>();
    }
}