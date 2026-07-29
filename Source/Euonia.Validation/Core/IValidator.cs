namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 
/// </summary>
public interface IValidator
{
	/// <summary>
	/// Return the collection of errors if entity state is not valid
	/// </summary>
	/// <typeparam name="TObject">The type of entity</typeparam>
	/// <param name="item">The instance with validation errors</param>
	void Validate<TObject>(TObject item)
		where TObject : class;

	/// <summary>
	/// Return the collection of errors if entity state is not valid
	/// </summary>
	/// <typeparam name="TObject"></typeparam>
	/// <param name="item"></param>
	/// <returns></returns>
	Task ValidateAsync<TObject>(TObject item)
		where TObject : class;
}