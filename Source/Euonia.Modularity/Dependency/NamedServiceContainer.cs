using System.Collections.Concurrent;

namespace System;

/// <summary>
/// 命名服务容器。
/// </summary>
/// <typeparam name="TService">服务实例的类型，作为该容器键值对应的服务类型参数。</typeparam>
public class NamedServiceContainer<TService> : ConcurrentDictionary<string, Type>
{
}