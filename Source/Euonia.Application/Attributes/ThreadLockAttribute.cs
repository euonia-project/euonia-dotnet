namespace Nerosoft.Euonia.Application;

/// <summary>
/// Represents a thread lock attribute that can be applied to methods or classes to ensure thread-safe execution within a single process.
/// </summary>
public sealed class ThreadLockAttribute : LockAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ThreadLockAttribute"/> class.
	/// </summary>
	public ThreadLockAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreadLockAttribute"/> class with the specified token.
	/// </summary>
	/// <param name="token">Specifies the lock token</param>
	/// <param name="maximumCount">Specifies the max count of concurrent accesses allowed.</param>
	public ThreadLockAttribute(string token, int maximumCount = 1)
		: base(token, maximumCount)
	{
	}
}
