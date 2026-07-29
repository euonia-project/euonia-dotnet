using System.Runtime.Serialization;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// Represents the exception that is thrown when given data is not valid.
/// Implements the <see cref="Exception" />
/// </summary>
/// <seealso cref="Exception"/>
[Serializable]
public class ValidationException : Exception
{
	private readonly IEnumerable<ValidationResult> _errors;

	/// <summary>
	/// Create a new <see cref="ValidationException"/>.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="results"></param>
	public ValidationException(string message, params ValidationResult[] results)
		: base(message)
	{
		_errors = results?.ToList() ?? Enumerable.Empty<ValidationResult>();
	}

	/// <summary>
	/// Creates a new <see cref="ValidationException"/>
	/// </summary>
	/// <param name="message"></param>
	/// <param name="errors"></param>
	public ValidationException(string message, IEnumerable<ValidationResult> errors)
		: base(message)
	{
		_errors = errors;
	}

	/// <summary>
	/// 
	/// </summary>
	public virtual IEnumerable<ValidationResult> Errors => _errors;

#pragma warning disable SYSLIB0051
	/// <inheritdoc />
	public ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_errors = info.GetValue(nameof(Errors), typeof(IEnumerable<ValidationResult>)) as IEnumerable<ValidationResult>;
	}

#pragma warning disable CS0672
	/// <inheritdoc />
	public override void GetObjectData(SerializationInfo info, StreamingContext context)

	{
		base.GetObjectData(info, context);
		info.AddValue(nameof(Errors), _errors, typeof(IEnumerable<ValidationResult>));
	}
#pragma warning restore CS0672

#pragma warning restore SYSLIB0051
}