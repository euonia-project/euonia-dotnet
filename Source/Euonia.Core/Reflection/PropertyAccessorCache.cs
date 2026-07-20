using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供对类型 <typeparamref name="T"/> 的属性访问器 Lambda 表达式的缓存。
/// </summary>
/// <typeparam name="T">要缓存属性访问器的类型，必须是引用类型。</typeparam>
public static class PropertyAccessorCache<T> where T : class
{
    private static readonly Dictionary<string, LambdaExpression> _cache = new();

    static PropertyAccessorCache()
    {
        var t = typeof(T);
        var parameter = Expression.Parameter(t, "p");
        foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var lambdaExpression = Expression.Lambda(propertyAccess, parameter);
            _cache[property.Name] = lambdaExpression;
        }
    }

    /// <summary>
    /// 获取指定属性的 Lambda 表达式。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>属性的 Lambda 表达式，如果未找到则返回 null。</returns>
    public static LambdaExpression Get(string propertyName)
    {
        return _cache.GetValueOrDefault(propertyName);
    }
}