using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 表示工作单元的配置选项。
/// </summary>
public interface IUnitOfWorkOptions
{
	/// <summary>
	/// 获取一个值，指示该工作单元是否应在事务中执行。
	/// </summary>
	/// <value>若需在事务中执行则为 <c>true</c>，否则为 <c>false</c>。</value>
	bool IsTransactional { get; }

	/// <summary>
	/// 获取事务使用的隔离级别（若已指定）。
	/// </summary>
	/// <value>事务隔离级别；为 <see langword="null"/> 时表示未指定，使用数据存储的默认级别。</value>
	IsolationLevel? IsolationLevel { get; }

	/// <summary>
	/// 获取工作单元的超时时长（若已指定）。
	/// </summary>
	/// <value>超时时长；为 <see langword="null"/> 时表示未显式设置，使用系统或默认超时。</value>
	TimeSpan? Timeout { get; }
}