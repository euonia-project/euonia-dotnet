namespace Nerosoft.Euonia.Osba;

/// <summary>
/// A contract represents the business object should use business context.
/// </summary>
public interface IUseBusinessContext
{
	/// <summary>
	/// Gets or sets the business context.
	/// </summary>
	BusinessContext BusinessContext { get; set; }
}