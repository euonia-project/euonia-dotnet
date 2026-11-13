using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 内存消息队列。
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal sealed class MessageQueue
{
    private static readonly ConcurrentDictionary<string, MessageQueue> _container = new();

    private readonly ConcurrentQueue<object> _queue = new();

    /// <summary>
    /// 消息入队后触发，携带消息载荷、消息上下文与处理类型，供订阅方继续分发。
    /// </summary>
    public event EventHandler<MessageProcessedEventArgs>? MessagePushed;

    /// <summary>
    /// 将消息追加到队列尾部，并同步通知 <see cref="MessagePushed"/> 订阅者。
    /// </summary>
    /// <param name="message">要入队的消息载荷。</param>
    /// <param name="messageContext">消息所属的上下文。</param>
    /// <param name="processType">本次入队对应的处理类型。</param>
    internal void Enqueue(object message, IMessageContext messageContext, MessageProcessType processType)
    {
        _queue.Enqueue(message);

        MessagePushed?.Invoke(this, new MessageProcessedEventArgs(message, messageContext, processType));
    }

    /// <summary>
    /// 从队列头部取出一个消息。
    /// </summary>
    /// <returns>出队的消息；队列为空时返回 <see langword="null"/>。</returns>
    internal object? Dequeue()
    {
        var succeed = _queue.TryDequeue(out var message);
        return succeed ? message : null;
    }

    /// <summary>
    /// 获取指定名称的消息队列。
    /// </summary>
    /// <param name="name">消息队列名称，作为队列在内部容器中的唯一键。</param>
    /// <returns>与 <paramref name="name"/> 关联的队列；若尚不存在，则创建并注册一个空队列后返回。</returns>
    internal static MessageQueue GetQueue(string name) => _container.GetOrAdd(name, new MessageQueue());

    /// <summary>
    /// 获取指定类型的消息队列。
    /// </summary>
    /// <typeparam name="T">消息类型，其 <see cref="Type.FullName"/> 用作队列名称。</typeparam>
    /// <returns>与 <typeparamref name="T"/> 关联的队列；若尚不存在，则创建并注册一个空队列后返回。</returns>
    internal static MessageQueue GetQueue<T>() => GetQueue(typeof(T));

    /// <summary>
    /// 获取指定类型的消息队列。
    /// </summary>
    /// <param name="type">消息类型，使用其 <see cref="Type.FullName"/>（非 null）作为队列名称。</param>
    /// <returns>与该类型关联的队列；若尚不存在，则创建并注册一个空队列后返回。</returns>
    internal static MessageQueue GetQueue(Type type) => GetQueue(type.FullName!);

    /// <summary>
    /// 为指定名称注册新的消息队列。
    /// </summary>
    /// <param name="name">消息队列名称，作为队列在内部容器中的唯一键。</param>
    /// <param name="queue">要注册的消息队列实例。</param>
    /// <remarks>若容器中已存在同名队列，则保留原有队列，不进行覆盖。</remarks>
    internal static void AddQueue(string name, MessageQueue queue) => _container.TryAdd(name, queue);

    /// <summary>
    /// 为指定类型注册新的消息队列。
    /// </summary>
    /// <typeparam name="T">消息类型，其 <see cref="Type.FullName"/>（非 null）用作队列名称。</typeparam>
    /// <param name="queue">要注册的消息队列实例。</param>
    internal static void AddQueue<T>(MessageQueue queue) => AddQueue(typeof(T), queue);

    /// <summary>
    /// 为指定类型注册新的消息队列。
    /// </summary>
    /// <param name="type">消息类型，使用其 <see cref="Type.FullName"/>（非 null）作为队列名称。</param>
    /// <param name="queue">要注册的消息队列实例。</param>
    internal static void AddQueue(Type type, MessageQueue queue) => AddQueue(type.FullName!, queue);
}