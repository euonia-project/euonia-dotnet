using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// The options used to create a unit of work.
/// </summary>
public class UnitOfWorkOptions : IUnitOfWorkOptions
{
	/// <summary>
	/// 
	/// </summary>
	public bool IsTransactional { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public IsolationLevel? IsolationLevel { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public TimeSpan? Timeout { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkOptions"/> class.
	/// </summary>
	public UnitOfWorkOptions()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UnitOfWorkOptions"/> class.
	/// </summary>
	/// <param name="isTransactional"></param>
	/// <param name="isolationLevel"></param>
	/// <param name="timeout"></param>
	public UnitOfWorkOptions(bool isTransactional = false, IsolationLevel? isolationLevel = null, TimeSpan? timeout = null)
	{
		IsTransactional = isTransactional;
		IsolationLevel = isolationLevel;
		Timeout = timeout;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="options"></param>
	/// <returns></returns>
	public UnitOfWorkOptions Normalize(UnitOfWorkOptions options)
	{
		options.IsolationLevel ??= IsolationLevel;

		options.Timeout ??= Timeout;

		return options;
	}
}