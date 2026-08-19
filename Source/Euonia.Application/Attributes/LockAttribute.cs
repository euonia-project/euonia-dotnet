namespace Nerosoft.Euonia.Application;

/// <summary>
/// Represents the base class for lock attributes that can be applied to methods or classes to ensure safe execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public abstract class LockAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LockAttribute"/> class.
	/// </summary>
	protected LockAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LockAttribute"/> class with the specified token.
	/// </summary>
	/// <param name="token">Specifies the lock token</param>
	/// <param name="maximumCount">Specifies the max count of concurrent accesses allowed.</param>
	protected LockAttribute(string token, int maximumCount = 1)
		: this()
	{
		Token = token;
		Check.Ensure(maximumCount > 0, nameof(maximumCount), "Maximum count must be greater than zero.");
		MaximumCount = maximumCount;
	}

	/// <summary>
	/// Gets the lock token. The token may contain placeholders in the form <c>{parameterName}</c>,
	/// which are replaced with the corresponding argument values of the intercepted method at runtime.
	/// </summary>
	public string Token { get; }

	private int _timeout = 30000;

	/// <summary>
	/// Gets or sets the lock timeout in milliseconds. Default is 30000 (30 seconds).
	/// </summary>
	public int Timeout
	{
		get => _timeout;
		set
		{
			Check.Ensure(value > 0, nameof(Timeout), "Timeout must be greater than zero.");
			_timeout = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of concurrent accesses allowed. Default is 1.
	/// </summary>
	public int MaximumCount { get; } = 1;
}
