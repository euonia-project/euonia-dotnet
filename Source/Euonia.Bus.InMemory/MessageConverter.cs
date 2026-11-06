using Nerosoft.Euonia.Reflection;
using Newtonsoft.Json;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 在消息处理管道内把接收到的载荷转换为目标类型的辅助类，兼容直接赋值、类型转换与 JSON 反序列化。
/// </summary>
internal class MessageConverter
{
    /// <summary>
    /// 把 <paramref name="source"/> 转换为 <paramref name="targetType"/> 类型的值。
    /// </summary>
    /// <param name="source">待转换的源对象；为 <see langword="null"/> 时直接返回 <see langword="null"/>。</param>
    /// <param name="targetType">期望得到的目标类型。</param>
    /// <returns>转换后的值；无法识别转换方式时返回 <see langword="null"/>。</returns>
    public static object? Convert(object source, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        if (targetType == source.GetType())
        {
            return source;
        }

        if (targetType == typeof(object))
        {
            return source;
        }

        if (targetType == typeof(Guid))
        {
            return TypeHelper.CoerceValue<Guid>(source.GetType(), source);
        }

        if (targetType.IsAssignableTo(typeof(IConvertible)))
        {
            return TypeHelper.CoerceValue(targetType, source.GetType(), source);
        }

        if (source is string content)
        {
            return JsonConvert.DeserializeObject(content, targetType);
        }

        return null;
    }
}