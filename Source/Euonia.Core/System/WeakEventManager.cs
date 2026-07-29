using System.Reflection;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 来自 <c>https://github.com/xamarin/Xamarin.Forms/blob/main/Xamarin.Forms.Core/WeakEventManager.cs</c>
/// </summary>
/// <remarks>
/// 补丁来自 <c>https://github.com/jonathanpeppers/maui/blob/d7b45739b0ffa6fb393321fdddc9317ffdaa1696/src/Core/src/WeakEventManager.cs</c>
/// </remarks>
public sealed class WeakEventManager
{
    private readonly Dictionary<string, List<Subscription>> _eventHandlers = new();

    /// <summary>
    /// 添加事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="eventName">事件名称。</param>
    /// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void AddEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, [CallerMemberName] string eventName = null)
        where TEventArgs : EventArgs
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    /// <summary>
    /// 添加事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器委托。</param>
    /// <param name="eventName">事件名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void AddEventHandler(Delegate handler, [CallerMemberName] string eventName = "")
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    /// <summary>
    /// 引发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="args">事件参数。</param>
    /// <param name="eventName">事件名称。</param>
    public void HandleEvent(object sender, object args, string eventName)
    {
        var handlers = GetEventHandler(eventName);

        foreach (var (subscriber, handler) in handlers)
        {
            handler.Invoke(subscriber, new[] { sender, args });
        }
    }

    /// <summary>
    /// 移除事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="eventName">事件名称。</param>
    /// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void RemoveEventHandler<TEventArgs>(EventHandler<TEventArgs> handler, [CallerMemberName] string eventName = null)
        where TEventArgs : EventArgs
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    /// <summary>
    /// 移除事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器委托。</param>
    /// <param name="eventName">事件名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void RemoveEventHandler(Delegate handler, [CallerMemberName] string eventName = "")
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    private void AddEventHandler(string eventName, object handlerTarget, MethodInfo methodInfo)
    {
        if (!_eventHandlers.TryGetValue(eventName, out var targets))
        {
            targets = new List<Subscription>();
            _eventHandlers.Add(eventName, targets);
        }

        if (handlerTarget == null)
        {
            // 此事件处理器是一个静态方法
            targets.Add(new Subscription(null, methodInfo));
            return;
        }

        targets.Add(new Subscription(new WeakReference(handlerTarget), methodInfo));
    }

    private void RemoveEventHandler(string eventName, object handlerTarget, MemberInfo methodInfo)
    {
        if (!_eventHandlers.TryGetValue(eventName, out var subscriptions))
        {
            return;
        }

        for (var n = subscriptions.Count - 1; n >= 0; n--)
        {
            var current = subscriptions[n];

            if (current.Subscriber != null && !current.Subscriber.IsAlive)
            {
                // 如果订阅者已不可用，移除并继续
                subscriptions.RemoveAt(n);
                continue;
            }

            if (current.Subscriber?.Target == handlerTarget && current.Handler.Name == methodInfo.Name)
            {
                // 找到匹配项，可以中断
                subscriptions.RemoveAt(n);
                break;
            }
        }
    }

    /// <summary>
    /// 添加事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="eventName">事件名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void AddEventHandler(EventHandler handler, [CallerMemberName] string eventName = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        AddEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    #region 扩展

    /// <summary>
    /// 获取指定事件的事件处理器。
    /// </summary>
    /// <param name="eventName">事件名称。</param>
    /// <returns>订阅者和处理器的列表。</returns>
    private List<(object subscriber, MethodInfo handler)> GetEventHandler(string eventName)
    {
        var toRaise = new List<(object subscriber, MethodInfo handler)>();
        var toRemove = new List<Subscription>();

        if (_eventHandlers.TryGetValue(eventName, out var target))
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < target.Count; i++)
            {
                var subscription = target[i];
                var isStatic = subscription.Subscriber == null;
                if (isStatic)
                {
                    // 对于静态方法，我们只传递 null 作为 MethodInfo.Invoke 的第一个参数
                    toRaise.Add((null, subscription.Handler));
                    continue;
                }

                var subscriber = subscription.Subscriber.Target;

                if (subscriber == null)
                {
                    // 订阅者已被回收，因此无需保留此订阅
                    toRemove.Add(subscription);
                }
                else
                {
                    toRaise.Add((subscriber, subscription.Handler));
                }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < toRemove.Count; i++)
            {
                var subscription = toRemove[i];
                target.Remove(subscription);
            }
        }

        {
        }
        return toRaise;
    }

    /// <summary>
    /// 引发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="args">事件参数。</param>
    /// <param name="eventName">事件名称。</param>
    /// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
    public void HandleEvent<TEventArgs>(object sender, TEventArgs args, string eventName)
        where TEventArgs : EventArgs
    {
        var handlers = GetEventHandler(eventName);

        foreach (var (subscriber, handler) in handlers)
        {
            handler.Invoke(subscriber, new[] { sender, args });
        }
    }

    /// <summary>
    /// 并行引发事件。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="args">事件参数。</param>
    /// <param name="eventName">事件名称。</param>
    /// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
    public void HandleEventParallel<TEventArgs>(object sender, TEventArgs args, string eventName)
        where TEventArgs : EventArgs
    {
        var handlers = GetEventHandler(eventName);

        Parallel.ForEach(handlers, (item, _) =>
        {
            try
            {
                item.handler.Invoke(item.subscriber, new[] { sender, args });
            }
            catch (Exception)
            {
                // 忽略异常
            }
        });
    }

    /// <summary>
    /// 引发事件并忽略异常。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="args">事件参数。</param>
    /// <param name="eventName">事件名称。</param>
    /// <typeparam name="TEventArgs">事件参数的类型。</typeparam>
    public void HandleEventSafely<TEventArgs>(object sender, TEventArgs args, string eventName)
        where TEventArgs : EventArgs
    {
        var handlers = GetEventHandler(eventName);

        foreach (var (subscriber, handler) in handlers)
        {
            try
            {
                handler.Invoke(subscriber, new[] { sender, args });
            }
            catch (Exception)
            {
                // 忽略异常
            }
        }
    }

    /// <summary>
    /// 移除事件处理器。
    /// </summary>
    /// <param name="handler">事件处理器。</param>
    /// <param name="eventName">事件名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 或 <paramref name="handler"/> 为 null 时抛出。</exception>
    public void RemoveEventHandler(EventHandler handler, [CallerMemberName] string eventName = null)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        RemoveEventHandler(eventName, handler.Target, handler.GetMethodInfo());
    }

    /// <summary>
    /// 移除所有事件处理器。
    /// </summary>
    public void RemoveEventHandlers()
    {
        _eventHandlers.Clear();
    }

    /// <summary>
    /// 移除指定事件的所有事件处理器。
    /// </summary>
    /// <param name="eventName">事件名称。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="eventName"/> 为 null 或空时抛出。</exception>
    public void RemoveEventHandlers(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        _eventHandlers.Remove(eventName);
    }

    #endregion

    private readonly struct Subscription : IEquatable<Subscription>
    {
        public Subscription(WeakReference subscriber, MethodInfo handler)
        {
            Subscriber = subscriber;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public readonly WeakReference Subscriber;
        public readonly MethodInfo Handler;

        public bool Equals(Subscription other) => Subscriber == other.Subscriber && Handler == other.Handler;

        public override bool Equals(object obj) => obj is Subscription other && Equals(other);

        public override int GetHashCode() => Subscriber?.GetHashCode() ?? 0 ^ Handler.GetHashCode();
    }
}
