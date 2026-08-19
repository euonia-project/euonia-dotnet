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
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    /// <inheritdoc />
    public virtual bool AutoRegisterApplicationService => true;

    /// <inheritdoc />
    public virtual bool AutoRegisterPipelineBehaviors => true;

    /// <inheritdoc />
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}