using System.Reflection;

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供执行枚举操作的方法。
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// 获取枚举的所有值。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <returns>枚举值数组。</returns>
    /// <exception cref="InvalidCastException">当 <typeparamref name="TEnum"/> 不是枚举类型时抛出。</exception>
    public static TEnum[] GetEnumValues<TEnum>()
    {
        var type = typeof(TEnum);

        if (!type.GetTypeInfo().IsEnum)
        {
            throw new InvalidCastException($"The type {type.FullName} is not enum.");
        }

        return (
            from field in type.GetRuntimeFields()
            where field.IsLiteral
            select (TEnum)field.GetValue(type)).ToArray();
    }

    /// <summary>
    /// 获取枚举的所有名称。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <returns>枚举名称数组。</returns>
    public static string[] GetEnumNames<TEnum>()
    {
        var type = typeof(TEnum);
        if (!type.GetTypeInfo().IsEnum)
        {
            throw new InvalidCastException($"The type {type.FullName} is not enum.");
        }

        return (
            from field in type.GetRuntimeFields()
            where field.IsLiteral
            select field.Name).ToArray();
    }

    /// <summary>
    /// 获取枚举字段上指定类型的第一个 <see cref="Attribute"/>。
    /// </summary>
    /// <typeparam name="T">特性类型。</typeparam>
    /// <param name="e">枚举值。</param>
    /// <returns>找到的特性实例，如果未找到则返回默认值。</returns>
    public static T GetAttribute<T>(Enum e)
        where T : Attribute
    {
        T attribute = default;
        var enumType = e.GetType();
        var members = enumType.GetTypeInfo().DeclaredMembers.ToArray();

        if (members.Length == 1)
        {
            var attrs = members[0].GetCustomAttributes(typeof(T), false).ToArray();
            if (attrs.Length > 0)
            {
                attribute = (T)attrs[0];
            }
        }

        {
        }

        return attribute;
    }

    /// <summary>
    /// 获取枚举值的自定义特性。
    /// </summary>
    /// <typeparam name="T">特性类型。</typeparam>
    /// <param name="value">枚举值。</param>
    /// <returns>找到的特性实例，如果未找到则返回默认值。</returns>
    public static T GetCustomerAttribute<T>(Enum value)
        where T : Attribute
    {
        var enumType = value.GetType();
        var name = Enum.GetName(enumType, value);
        if (!string.IsNullOrEmpty(name))
        {
            var fieldInfo = enumType.GetRuntimeField(name);
            if (fieldInfo != null)
            {
                var attr = fieldInfo.GetCustomAttribute<T>();
                if (attr != null)
                {
                    return attr;
                }
            }
        }
        return default;
    }
}