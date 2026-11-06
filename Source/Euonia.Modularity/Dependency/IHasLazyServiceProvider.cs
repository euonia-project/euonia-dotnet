namespace System;

/// <summary>
/// 由需要延迟解析服务的组件实现，用于暴露 <see cref="ILazyServiceProvider"/>。
/// </summary>
public interface IHasLazyServiceProvider
{
    /// <summary>
    /// 获取或设置用于按需解析服务的延迟服务提供程序。
    /// </summary>
    ILazyServiceProvider LazyServiceProvider { get; set; }
}