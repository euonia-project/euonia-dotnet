using System.Globalization;
using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Helper class to load cache manager configurations from file or to build new configurations
/// in a fluent way.
/// <para>
/// This only loads configurations. To build a cache manager instance, use <c>CacheFactory</c>
/// and pass in the configuration. Or use the <c>Build</c> methods of <c>CacheFactory</c>!
/// </para>
/// </summary>
/// <see cref="CacheFactory"/>
public class ConfigurationBuilder : ConfigurationBuilderCachePart
{
    private const string HOURS = "h";
    private const string MINUTES = "m";
    private const string SECONDS = "s";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
    /// which provides fluent configuration methods.
    /// </summary>
    public ConfigurationBuilder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
    /// which provides fluent configuration methods.
    /// </summary>
    /// <param name="name">The name of the cache manager.</param>
    public ConfigurationBuilder(string name)
    {
        Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
        Configuration.Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
    /// which provides fluent configuration methods.
    /// Creates a builder which allows to modify the existing <paramref name="forConfiguration"/>.
    /// </summary>
    /// <param name="forConfiguration">The configuration the builder should be instantiated for.</param>
    public ConfigurationBuilder(CacheManagerConfiguration forConfiguration)
        : base(forConfiguration)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
    /// which provides fluent configuration methods.
    /// Creates a builder which allows to modify the existing <paramref name="forConfiguration"/>.
    /// </summary>
    /// <param name="name">The name of the cache manager.</param>
    /// <param name="forConfiguration">The configuration the builder should be instantiated for.</param>
    public ConfigurationBuilder(string name, CacheManagerConfiguration forConfiguration)
        : base(forConfiguration)
    {
        Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
        Configuration.Name = name;
    }

    /// <summary>
    /// Builds a <see cref="CacheManagerConfiguration"/> which can be used to create a new cache
    /// manager instance.
    /// <para>
    /// Pass the configuration to <see cref="CacheFactory.FromConfiguration{TCacheValue}(CacheManagerConfiguration)"/>
    /// to create a valid cache manager.
    /// </para>
    /// </summary>
    /// <param name="settings">
    /// The configuration settings to define the cache handles and other properties.
    /// </param>
    /// <returns>The <see cref="CacheManagerConfiguration"/>.</returns>
    public static CacheManagerConfiguration BuildConfiguration(Action<ConfigurationBuilderCachePart> settings)
    {
        Check.EnsureNotNull(settings, nameof(settings));

        var part = new ConfigurationBuilder();
        settings(part);
        return part.Configuration;
    }

    /// <summary>
    /// Builds a <see cref="CacheManagerConfiguration"/> which can be used to create a new cache
    /// manager instance.
    /// <para>
    /// Pass the configuration to <see cref="CacheFactory.FromConfiguration{TCacheValue}(CacheManagerConfiguration)"/>
    /// to create a valid cache manager.
    /// </para>
    /// </summary>
    /// <param name="name">The name of the cache manager.</param>
    /// <param name="settings">
    /// The configuration settings to define the cache handles and other properties.
    /// </param>
    /// <returns>The <see cref="CacheManagerConfiguration"/>.</returns>
    public static CacheManagerConfiguration BuildConfiguration(string name, Action<ConfigurationBuilderCachePart> settings)
    {
        Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
        Check.EnsureNotNull(settings, nameof(settings));

        var part = new ConfigurationBuilder();
        settings(part);
        part.Configuration.Name = name;
        return part.Configuration;
    }

    /// <summary>
    /// Parses the timespan setting from configuration.
    /// Cfg value can be suffixed with s|h|m for seconds hours or minutes...
    /// Depending on the suffix we have to construct the returned TimeSpan.
    /// </summary>
    /// <param name="timespanCfgValue"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static TimeSpan GetTimeSpan(string timespanCfgValue, string propName)
    {
        if (string.IsNullOrWhiteSpace(timespanCfgValue))
        {
            // default value coming from the system.configuration seems to be empty string...
            return TimeSpan.Zero;
        }

        var normValue = timespanCfgValue.ToUpper(CultureInfo.InvariantCulture);
        
        var hasSuffix = Regex.IsMatch(normValue, @"\b[0-9]+[S|H|M]\b");

        var suffix = hasSuffix ? new string(normValue.Last(), 1) : string.Empty;

        if (!int.TryParse(hasSuffix ? normValue[..^1] : normValue, out var timeoutValue))
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, Resources.IDS_CONFIGURATION_PROPERTY_PARSE_FAILED, timespanCfgValue, propName));
        }

        // if minutes or no suffix is defined, we use minutes.
        if (!hasSuffix || suffix.Equals(MINUTES, StringComparison.OrdinalIgnoreCase))
        {
            return TimeSpan.FromMinutes(timeoutValue);
        }

        // seconds
        if (suffix.Equals(SECONDS, StringComparison.OrdinalIgnoreCase))
        {
	        return TimeSpan.FromSeconds(timeoutValue);
        }

        // hours
        if (suffix.Equals(HOURS, StringComparison.OrdinalIgnoreCase))
        {
            return TimeSpan.FromHours(timeoutValue);
        }

        // last option would be seconds
        return TimeSpan.FromSeconds(timeoutValue);
    }
}
