using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器入口，用于为指定的可编辑对象类型创建 <see cref="ActuatorBuilder{TTarget}"/>，以构建获取、创建、删除等操作。
/// </summary>
/// <remarks>
/// 可通过构造函数注入或静态方法直接使用。静态方法 <see cref="For{TTarget}(IServiceProvider)"/> 支持从 <see cref="IServiceProvider"/> 中自动解析依赖。
/// </remarks>
/// <param name="provider">用于解析 <see cref="IObjectFactory"/> 和 <see cref="IPipeline{TRequest, TResponse}"/> 的服务提供程序。</param>
public sealed class Actuator(IServiceProvider provider)
{
	/// <summary>
	/// 为指定类型的可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	public ActuatorBuilder<TTarget> For<TTarget>()
		where TTarget : EditableObject<TTarget>
	{
		return For<TTarget>(provider);
	}

	/// <summary>
	/// 使用指定的工厂和工作单元管理器为可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="factory">对象工厂。</param>
	/// <param name="pipeline">工作单元管理器。</param>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	public static ActuatorBuilder<TTarget> For<TTarget>([NotNull] IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline)
		where TTarget : EditableObject<TTarget>
	{
		return new ActuatorBuilder<TTarget>(factory, pipeline);
	}

	/// <summary>
	/// 从服务提供程序中自动解析依赖，为可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="provider">用于解析 <see cref="IObjectFactory"/> 和 <see cref="IUnitOfWorkManager"/> 的服务提供程序。</param>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	public static ActuatorBuilder<TTarget> For<TTarget>([NotNull] IServiceProvider provider)
		where TTarget : EditableObject<TTarget>
	{
		var factory = provider.GetRequiredService<IObjectFactory>();
		var pipeline = provider.GetService<IPipeline<TTarget, TTarget>>();

		pipeline ??= new DefaultPipelineProvider<TTarget, TTarget>(provider);

		var behaviors = provider.GetServices<IActuatorBehavior<TTarget>>()?.ToList() ?? [];

		foreach( var behavior in behaviors)
		{
			pipeline.Use(behavior.HandleAsync);
		}

		return new ActuatorBuilder<TTarget>(factory!, pipeline!);
	}
}
