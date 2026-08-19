namespace Nerosoft.Euonia.Application;

/// <summary>
/// Represents a distributed lock attribute that can be applied to methods or classes to ensure safe execution across processes or systems.
/// </summary>
public sealed class DistributedLockAttribute : LockAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DistributedLockAttribute"/> class.
	/// </summary>
	public DistributedLockAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DistributedLockAttribute"/> class with the specified token.
	/// </summary>
	/// <param name="token">Specifies the lock token</param>
	public DistributedLockAttribute(string token)
		: base(token)
	{
	}
}
