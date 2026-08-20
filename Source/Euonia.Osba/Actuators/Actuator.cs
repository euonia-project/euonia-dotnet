using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 执行器入口，用于为指定的可编辑对象类型创建 <see cref="ActuatorBuilder{TTarget}"/>，以构建获取、创建、删除等操作。
/// </summary>
/// <remarks>
/// 可通过构造函数注入或静态方法直接使用。静态方法 <see cref="For{TTarget}(IServiceProvider)"/> 支持从 <see cref="IServiceProvider"/> 中自动解析依赖，
/// 并将已注册的 <c>IActuatorBehavior&lt;TTarget&gt;</c> 服务自动接入执行管道。
/// </remarks>
/// <param name="provider">用于解析 <see cref="IObjectFactory"/> 与执行管道（<c>IPipeline&lt;TTarget, TTarget&gt;</c>）的服务提供程序。</param>
internal sealed class Actuator(IServiceProvider provider) : IActuator
{
	/// <summary>
	/// 为指定类型的可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	/// <remarks>
	/// 该实例方法委托给静态重载 <see cref="For{TTarget}(IServiceProvider)"/>，使用构造函数注入的服务提供程序解析依赖。
	/// </remarks>
	public ActuatorBuilder<TTarget> For<TTarget>()
		where TTarget : EditableObject<TTarget>
	{
		return For<TTarget>(provider);
	}

	/// <summary>
	/// 使用指定的对象工厂和执行管道为可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="factory">对象工厂，用于获取或创建目标对象。</param>
	/// <param name="pipeline">执行管道，用于处理目标对象的操作请求。</param>
	/// <param name="behaviors">执行器行为的集合，用于在执行管道中处理目标对象的操作请求。</param>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	/// <remarks>
	/// 该重载绕过依赖注入容器，直接使用给定的工厂与管道构建执行器，适用于手动组合依赖的场景。
	/// </remarks>
	public static ActuatorBuilder<TTarget> For<TTarget>([NotNull] IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline, IEnumerable<IActuatorBehavior<TTarget>> behaviors = null)
		where TTarget : EditableObject<TTarget>
	{
		if (behaviors != null)
		{
			foreach (var behavior in behaviors)
			{
				pipeline.Use(behavior.HandleAsync);
			}
		}

		{
		}
		return new ActuatorBuilder<TTarget>(factory, pipeline);
	}

	/// <summary>
	/// 从服务提供程序中自动解析依赖，为可编辑对象创建构建器。
	/// </summary>
	/// <typeparam name="TTarget">可编辑对象的具体类型，必须继承自 <see cref="EditableObject{TTarget}"/>。</typeparam>
	/// <param name="provider">用于解析 <see cref="IObjectFactory"/>、<see cref="IPipeline{TTarget, TTarget}"/> 与 <see cref="IActuatorBehavior{TTarget}"/> 的服务提供程序。</param>
	/// <returns>用于配置和执行操作的 <see cref="ActuatorBuilder{TTarget}"/> 实例。</returns>
	/// <exception cref="InvalidOperationException">服务提供程序中未注册 <see cref="IObjectFactory"/> 时抛出。</exception>
	/// <remarks>
	/// 解析规则：<see cref="IObjectFactory"/> 必须已注册；<see cref="IPipeline{TTarget, TTarget}"/> 未注册时回退为 <see cref="DefaultPipelineProvider{TTarget, TTarget}"/>；
	/// 已注册的全部 <see cref="IActuatorBehavior{TTarget}"/> 服务会按序追加到执行管道中。
	/// </remarks>
	public static ActuatorBuilder<TTarget> For<TTarget>([NotNull] IServiceProvider provider)
		where TTarget : EditableObject<TTarget>
	{
		var factory = provider.GetRequiredService<IObjectFactory>();
		var pipeline = provider.GetService<IPipeline<TTarget, TTarget>>();

		pipeline ??= new DefaultPipelineProvider<TTarget, TTarget>(provider);
		var behaviors = provider.GetServices<IActuatorBehavior<TTarget>>();
		return For(factory, pipeline, behaviors);
	}
}