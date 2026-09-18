using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 创建与配置工作单元时使用的选项。
/// </summary>
public class UnitOfWorkOptions : IUnitOfWorkOptions
{
	/// <summary>
	/// 获取或设置一个值，指示该工作单元是否应在事务中执行。
	/// </summary>
	/// <value>若需在事务中执行则为 <c>true</c>，否则为 <c>false</c>。</value>
	public bool IsTransactional { get; set; }

	/// <summary>
	/// 获取或设置事务使用的隔离级别。
	/// </summary>
	/// <value>事务隔离级别；为 <c>null</c> 时使用数据存储的默认级别。</value>
	public IsolationLevel? IsolationLevel { get; set; }

	/// <summary>
	/// 获取或设置工作单元的超时时长。
	/// </summary>
	/// <value>超时时长；为 <c>null</c> 时使用系统或默认超时。</value>
	public TimeSpan? Timeout { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkOptions"/> class.
	/// </summary>
	public UnitOfWorkOptions()
	{
	}

	/// <summary>
	/// 使用指定的配置初始化 <see cref="UnitOfWorkOptions"/> 类的新实例。
	/// </summary>
	/// <param name="isTransactional">指示工作单元是否在事务中执行。</param>
	/// <param name="isolationLevel">事务隔离级别；为 <c>null</c> 时使用默认级别。</param>
	/// <param name="timeout">工作单元的超时时长；为 <c>null</c> 时使用默认超时。</param>
	public UnitOfWorkOptions(bool isTransactional = false, IsolationLevel? isolationLevel = null, TimeSpan? timeout = null)
	{
		IsTransactional = isTransactional;
		IsolationLevel = isolationLevel;
		Timeout = timeout;
	}

	/// <summary>
	/// 使用当前实例的值补全指定选项中的未设置项。
	/// </summary>
	/// <param name="options">待补全的选项实例；其 <see cref="IsolationLevel"/> 与 <see cref="Timeout"/> 为 <c>null</c> 时将被覆盖。</param>
	/// <returns>补全后的 <paramref name="options"/> 实例（与传入的实例为同一对象）。</returns>
	/// <remarks>
	/// 该方法仅作用于 <paramref name="options"/>，不会修改当前实例；
	/// 且不会覆盖 <see cref="IsTransactional"/>，需要事务语义时应由调用方显式设置。
	/// </remarks>
	public UnitOfWorkOptions Normalize(UnitOfWorkOptions options)
	{
		options.IsolationLevel ??= IsolationLevel;

		options.Timeout ??= Timeout;

		return options;
	}
}