namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息转换委托，用于将源对象转换为指定目标类型。
/// </summary>
/// <param name="source">要转换的源消息对象。</param>
/// <param name="targetType">转换的目标类型。</param>
/// <returns>转换后的目标类型实例。</returns>
public delegate object MessageConvert(object source, Type targetType);