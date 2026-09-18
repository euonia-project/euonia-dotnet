using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 用于标注类、方法或接口以启用工作单元模式。
/// </summary>
/// <remarks>
/// 该特性可标注在类或方法上（见 <see cref="UnitOfWorkInterceptor"/>）；当标注在方法上时优先于类上的配置，
/// 方法或类上将 <see cref="IsDisabled"/> 设为 <c>true</c> 可针对该范围禁用工作单元。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
public class UnitOfWorkAttribute : Attribute
{
	/// <summary>
	/// 获取或设置一个值，指示该工作单元是否事务化。
	/// </summary>
	/// <value>为 <c>null</c> 时使用默认配置（<see cref="UnitOfWorkOptions"/> 中的设置）。</value>
	public bool? IsTransactional { get; set; }

	/// <summary>
	/// 获取或设置该工作单元的超时时长。
	/// </summary>
	/// <value>为 <c>null</c> 时使用默认配置（<see cref="UnitOfWorkOptions"/> 中的设置）。</value>
	public TimeSpan? Timeout { get; set; }

	/// <summary>
	/// 获取或设置事务的隔离级别（仅在工作单元事务化时生效）。
	/// </summary>
	/// <value>为 <c>null</c> 时使用默认配置（<see cref="UnitOfWorkOptions"/> 中的设置）。</value>
	public IsolationLevel? IsolationLevel { get; set; }

	/// <summary>
	/// 获取或设置一个值，用于阻止为该成员开启新的工作单元。
	/// </summary>
	/// <value>默认值为 <c>false</c>。若已存在处于活动状态的工作单元，则该设置被忽略。</value>
	public bool IsDisabled { get; set; }

	/// <summary>
	/// 初始化 <see cref="UnitOfWorkAttribute"/> 类的新实例。
	/// </summary>
	public UnitOfWorkAttribute()
	{
	}

	/// <summary>
	/// 使用是否事务化初始化 <see cref="UnitOfWorkAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="isTransactional">指示工作单元是否事务化；为 <c>null</c> 时使用默认配置。</param>
	public UnitOfWorkAttribute(bool? isTransactional)
		: this()
	{
		IsTransactional = isTransactional;
	}

	/// <summary>
	/// 使用是否事务化与事务隔离级别初始化 <see cref="UnitOfWorkAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="isTransactional">指示工作单元是否事务化；为 <c>null</c> 时使用默认配置。</param>
	/// <param name="isolationLevel">事务隔离级别；为 <c>null</c> 时使用默认配置。</param>
	public UnitOfWorkAttribute(bool? isTransactional, IsolationLevel? isolationLevel)
		: this(isTransactional)
	{
		IsolationLevel = isolationLevel;
	}

	/// <summary>
	/// 使用是否事务化、事务隔离级别与超时初始化 <see cref="UnitOfWorkAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="isTransactional">指示工作单元是否事务化；为 <c>null</c> 时使用默认配置。</param>
	/// <param name="isolationLevel">事务隔离级别；为 <c>null</c> 时使用默认配置。</param>
	/// <param name="timeout">工作单元的超时时长；为 <c>null</c> 时使用默认配置。</param>
	public UnitOfWorkAttribute(bool? isTransactional, IsolationLevel? isolationLevel, TimeSpan? timeout)
		: this(isTransactional, isolationLevel)
	{
		Timeout = timeout;
	}
}