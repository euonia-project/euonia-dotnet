using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 提供 <see cref="IServiceContext"/> 的默认实现，作为应用服务上下文的基础类型。
/// </summary>
/// <remarks>
/// 默认启用应用服务与管道行为的自动注册；派生类型可按需重写相应属性，
/// 或在 <see cref="ConfigureServices"/> 中注册应用所需的额外服务。
/// </remarks>
public abstract class ServiceContextBase : IServiceContext
{
    /// <inheritdoc />
    // 使用运行时实际类型所在程序集而非 GetExecutingAssembly，
    // 避免派生上下文类与基类位于不同程序集时扫描到错误的程序集。
    public Assembly Assembly => GetType().Assembly;

    /// <inheritdoc />
    public virtual bool AutoRegisterApplicationService => true;

    /// <inheritdoc />
    public virtual bool AutoRegisterPipelineBehaviors => true;

	/// <summary>
	/// 获取应用服务的生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。
	/// </summary>
	public virtual ServiceLifetime ApplicationServiceLifetime => ServiceLifetime.Scoped;

	/// <inheritdoc />
	public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}