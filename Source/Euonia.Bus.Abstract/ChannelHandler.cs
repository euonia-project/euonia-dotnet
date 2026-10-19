using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示一个消息处理器，封装了处理器类型、方法和实例。
/// </summary>
/// <param name="HandlerType">处理器的类型。</param>
/// <param name="Method">处理器的方法信息。</param>
/// <param name="Instance">处理器的实例。</param>
/// <param name="ImplementationType">
/// 接口处理器对应的具体实现类型。不同的实现类即使实现同一个接口也视为不同处理器；
/// 非接口处理器为 <see langword="null"/>。
/// </param>
public record ChannelHandler(Type HandlerType, MethodInfo Method, object Instance = null, Type ImplementationType = null);