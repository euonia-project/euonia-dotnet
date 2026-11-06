using System.Globalization;
using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 用于从文件加载缓存管理器配置，或以流式方式构建新配置的辅助类。
/// <para>
/// 它只负责加载配置。要构建缓存管理器实例，请使用 <c>CacheFactory</c>
/// 并传入配置，或使用 <c>CacheFactory</c> 的 <c>Build</c> 方法！
/// </para>
/// </summary>
/// <see cref="CacheFactory"/>
public class ConfigurationBuilder : ConfigurationBuilderCachePart
{
    private const string HOURS = "h";
    private const string MINUTES = "m";
    private const string SECONDS = "s";

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilder"/> 类的新实例，
    /// 该实例提供流式配置方法。
    /// </summary>
    public ConfigurationBuilder()
    {
    }

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilder"/> 类的新实例，
    /// 该实例提供流式配置方法。
    /// </summary>
    /// <param name="name">缓存管理器的名称。</param>
    public ConfigurationBuilder(string name)
    {
        Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
        Configuration.Name = name;
    }

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilder"/> 类的新实例，
    /// 该实例提供流式配置方法。
    /// 创建一个构建器，用于修改现有的 <paramref name="forConfiguration"/>。
    /// </summary>
    /// <param name="forConfiguration">构建器要针对的配置。</param>
    public ConfigurationBuilder(CacheManagerConfiguration forConfiguration)
        : base(forConfiguration)
    {
    }

    /// <summary>
    /// 初始化 <see cref="ConfigurationBuilder"/> 类的新实例，
    /// 该实例提供流式配置方法。
    /// 创建一个构建器，用于修改现有的 <paramref name="forConfiguration"/>。
    /// </summary>
    /// <param name="name">缓存管理器的名称。</param>
    /// <param name="forConfiguration">构建器要针对的配置。</param>
    public ConfigurationBuilder(string name, CacheManagerConfiguration forConfiguration)
        : base(forConfiguration)
    {
        Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
        Configuration.Name = name;
    }

    /// <summary>
    /// 构建一个 <see cref="CacheManagerConfiguration"/>，可用于创建新的缓存管理器实例。
    /// <para>
    /// 将该配置传入 <see cref="CacheFactory.FromConfiguration{TCacheValue}(CacheManagerConfiguration)"/>
    /// 即可创建有效的缓存管理器。
    /// </para>
    /// </summary>
    /// <param name="settings">
    /// 用于定义缓存句柄及其他属性的配置设置。
    /// </param>
    /// <returns>该 <see cref="CacheManagerConfiguration"/>。</returns>
    public static CacheManagerConfiguration BuildConfiguration(Action<ConfigurationBuilderCachePart> settings)
    {
        Check.EnsureNotNull(settings, nameof(settings));

        var part = new ConfigurationBuilder();
        settings(part);
        return part.Configuration;
    }

    /// <summary>
    /// 构建一个 <see cref="CacheManagerConfiguration"/>，可用于创建新的缓存管理器实例。
    /// <para>
    /// 将该配置传入 <see cref="CacheFactory.FromConfiguration{TCacheValue}(CacheManagerConfiguration)"/>
    /// 即可创建有效的缓存管理器。
    /// </para>
    /// </summary>
    /// <param name="name">缓存管理器的名称。</param>
    /// <param name="settings">
    /// 用于定义缓存句柄及其他属性的配置设置。
    /// </param>
    /// <returns>该 <see cref="CacheManagerConfiguration"/>。</returns>
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
    /// 从配置中解析时间跨度设置。
    /// 配置值可以带 s|h|m 后缀，分别表示秒、小时或分钟……
    /// 根据后缀构造返回的 TimeSpan。
    /// </summary>
    /// <param name="timespanCfgValue">配置中的时间跨度值，可带 <c>s</c>、<c>h</c> 或 <c>m</c> 后缀，分别表示秒、小时或分钟。</param>
    /// <param name="propName">属性名称，用于在解析失败时构造错误消息。</param>
    /// <returns>解析得到的时间跨度；输入为空时返回 <see cref="TimeSpan.Zero"/>，无后缀时按分钟处理。</returns>
    /// <exception cref="InvalidOperationException">配置值无法解析为整数时抛出。</exception>
    public static TimeSpan GetTimeSpan(string timespanCfgValue, string propName)
    {
        if (string.IsNullOrWhiteSpace(timespanCfgValue))
        {
            // 来自 system.configuration 的默认值似乎是空字符串……
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

        // 如果未定义后缀或单位为分钟，则按分钟处理。
        if (!hasSuffix || suffix.Equals(MINUTES, StringComparison.OrdinalIgnoreCase))
        {
            return TimeSpan.FromMinutes(timeoutValue);
        }

        // 秒
        if (suffix.Equals(SECONDS, StringComparison.OrdinalIgnoreCase))
        {
	        return TimeSpan.FromSeconds(timeoutValue);
        }

        // 小时
        if (suffix.Equals(HOURS, StringComparison.OrdinalIgnoreCase))
        {
            return TimeSpan.FromHours(timeoutValue);
        }

        // 最后的兜底选项为秒
        return TimeSpan.FromSeconds(timeoutValue);
    }
}
