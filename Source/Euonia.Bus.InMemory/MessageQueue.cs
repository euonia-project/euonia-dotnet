using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// The in-memory message queue.
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal sealed class MessageQueue
{
    private static readonly ConcurrentDictionary<string, MessageQueue> _container = new();

    private readonly ConcurrentQueue<object> _queue = new();

    public event EventHandler<MessageProcessedEventArgs>? MessagePushed;

    internal void Enqueue(object message, IMessageContext messageContext, MessageProcessType processType)
    {
        _queue.Enqueue(message);

        MessagePushed?.Invoke(this, new MessageProcessedEventArgs(message, messageContext, processType));
    }

    internal object? Dequeue()
    {
        var succeed = _queue.TryDequeue(out var message);
        return succeed ? message : null;
    }

    /// <summary>
    /// Gets the message queue by the specified name.
    /// </summary>
    /// <param name="name">Message queue name.</param>
    /// <returns></returns>
    internal static MessageQueue GetQueue(string name) => _container.GetOrAdd(name, new MessageQueue());

    /// <summary>
    /// Gets the message queue by the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static MessageQueue GetQueue<T>() => GetQueue(typeof(T));

    /// <summary>
    /// Gets the message queue by the specified type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static MessageQueue GetQueue(Type type) => GetQueue(type.FullName!);

    /// <summary>
    /// Register new message queue for specified name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="queue"></param>
    internal static void AddQueue(string name, MessageQueue queue) => _container.TryAdd(name, queue);

    /// <summary>
    /// Register new message queue for specified type.
    /// </summary>
    /// <param name="queue"></param>
    /// <typeparam name="T"></typeparam>
    internal static void AddQueue<T>(MessageQueue queue) => AddQueue(typeof(T), queue);

    /// <summary>
    /// Register new message queue for specified type.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="queue"></param>
    internal static void AddQueue(Type type, MessageQueue queue) => AddQueue(type.FullName!, queue);
}