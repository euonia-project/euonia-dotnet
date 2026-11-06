namespace System;

/// <summary>
/// 用于按指定名称注入或解析服务的委托。
/// </summary>
/// <param name="name">服务名称。</param>
/// <typeparam name="TService">服务类型。</typeparam>
public delegate TService NamedService<out TService>(string name);