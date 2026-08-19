namespace Nerosoft.Euonia.Application;

/// <summary>
/// Represents a process lock attribute that can be applied to methods or classes to ensure safe execution across processes on the same machine.
/// </summary>
public sealed class ProcessLockAttribute : LockAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ProcessLockAttribute"/> class.
	/// </summary>
	public ProcessLockAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ProcessLockAttribute"/> class with the specified token.
	/// </summary>
	/// <param name="token">Specifies the lock token</param>
	public ProcessLockAttribute(string token)
		: base(token)
	{
	}
}
