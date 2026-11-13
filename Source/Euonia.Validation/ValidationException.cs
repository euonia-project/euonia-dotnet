using System.Runtime.Serialization;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 表示给定的数据无效时引发的异常。
/// 实现 <see cref="Exception" />
/// </summary>
/// <seealso cref="Exception"/>
[Serializable]
public class ValidationException : Exception
{
	private readonly IEnumerable<ValidationResult> _errors;

	/// <summary>
	/// 创建新的 <see cref="ValidationException"/>。
	/// </summary>
	/// <param name="message">描述验证失败的错误消息。</param>
	/// <param name="results">验证失败的结果集合。</param>
	public ValidationException(string message, params ValidationResult[] results)
		: base(message)
	{
		_errors = results?.ToList() ?? Enumerable.Empty<ValidationResult>();
	}

	/// <summary>
	/// 创建新的 <see cref="ValidationException"/>
	/// </summary>
	/// <param name="message">描述验证失败的错误消息。</param>
	/// <param name="errors">验证失败的结果集合。</param>
	public ValidationException(string message, IEnumerable<ValidationResult> errors)
		: base(message)
	{
		_errors = errors;
	}

	/// <summary>
	/// 获取验证失败的结果集合。
	/// </summary>
	public virtual IEnumerable<ValidationResult> Errors => _errors;

#pragma warning disable SYSLIB0051
	/// <inheritdoc />
	public ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_errors = info.GetValue(nameof(Errors), typeof(IEnumerable<ValidationResult>)) as IEnumerable<ValidationResult> ?? Enumerable.Empty<ValidationResult>();
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