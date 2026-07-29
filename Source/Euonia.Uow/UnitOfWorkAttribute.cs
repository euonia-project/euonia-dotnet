using System.Data;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// The attribute used to mark a class or method to use UOW pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
public class UnitOfWorkAttribute : Attribute
{
	/// <summary>
	/// Gets or sets a value to indicates whether this UOW is transactional or not.
	/// Uses default value if not supplied.
	/// </summary>
	public bool? IsTransactional { get; set; }

	/// <summary>
	/// Gets or sets the timeout value of this UOW.
	/// Uses default value if not supplied.
	/// </summary>
	public TimeSpan? Timeout { get; set; }

	/// <summary>
	/// If this UOW is transactional, this option indicated the isolation level of the transaction.
	/// Uses default value if not supplied.
	/// </summary>
	public IsolationLevel? IsolationLevel { get; set; }

	/// <summary>
	/// Used to prevent starting a unit of work for the method.
	/// If there is already a started unit of work, this property is ignored.
	/// Default: false.
	/// </summary>
	public bool IsDisabled { get; set; }

	/// <summary>
	/// Initialize a new instance of <see cref="UnitOfWorkAttribute"/> class.
	/// </summary>
	public UnitOfWorkAttribute()
	{
	}

	/// <summary>
	/// Initialize a new instance of <see cref="UnitOfWorkAttribute"/> class.
	/// </summary>
	/// <param name="isTransactional"></param>
	public UnitOfWorkAttribute(bool? isTransactional)
		: this()
	{
		IsTransactional = isTransactional;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="UnitOfWorkAttribute"/> class.
	/// </summary>
	/// <param name="isTransactional"></param>
	/// <param name="isolationLevel"></param>
	public UnitOfWorkAttribute(bool? isTransactional, IsolationLevel? isolationLevel)
		: this(isTransactional)
	{
		IsolationLevel = isolationLevel;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="UnitOfWorkAttribute"/> class.
	/// </summary>
	/// <param name="isTransactional"></param>
	/// <param name="isolationLevel"></param>
	/// <param name="timeout"></param>
	public UnitOfWorkAttribute(bool? isTransactional, IsolationLevel? isolationLevel, TimeSpan? timeout)
		: this(isTransactional, isolationLevel)
	{
		Timeout = timeout;
	}
}