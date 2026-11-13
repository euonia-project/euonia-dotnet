namespace System;

/// <summary>
/// 表示继承的类/接口将作为瞬态服务注入。
/// </summary>
public interface ITransientDependency
{
}

/// <summary>
/// 表示继承的类/接口将作为单例服务注入。
/// </summary>
public interface ISingletonDependency
{
}

/// <summary>
/// 表示继承的类/接口将作为作用域服务注入。
/// </summary>
public interface IScopedDependency
{
}