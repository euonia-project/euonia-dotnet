using System.Reflection;

// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 将字符串值解析为枚举类型 <typeparamref name="T"/> 的方法。
/// </summary>
/// <typeparam name="T">要解析的枚举类型。</typeparam>
public static class EnumParser<T>
{
    private static readonly Dictionary<string, T> _dictionary = new();

    static EnumParser()
    {
        var type = typeof(T);
        if (!type.GetTypeInfo().IsEnum)
        {
            throw new InvalidCastException($"The type {type.FullName} is not enum.");
        }

        var names = Enum.GetNames(type);
        var values = (T[])Enum.GetValues(type);

        for (var i = 0; i < names.Length; i++)
        {
            _dictionary.Add(names[i], values[i]);
        }
    }

    /// <summary>
    /// 尝试将字符串值解析为枚举类型 <typeparamref name="T"/>。
    /// </summary>
    /// <param name="name">要解析的字符串名称。</param>
    /// <param name="value">解析成功时输出枚举值。</param>
    /// <returns>如果解析成功，则为 true；否则为 false。</returns>
    public static bool TryParse(string name, out T value)
    {
        return _dictionary.TryGetValue(name, out value);
    }

    /// <summary>
    /// 将字符串值解析为枚举类型 <typeparamref name="T"/>。
    /// </summary>
    /// <param name="name">要解析的字符串名称。</param>
    /// <returns>解析得到的枚举值。</returns>
    /// <exception cref="KeyNotFoundException">当 <paramref name="name"/> 不是有效的枚举名称时抛出。</exception>
    public static T Parse(string name)
    {
        return _dictionary[name];
    }
}