namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The bus related constants.
/// </summary>
internal class Constants
{
	/// <summary>
	/// The minimal sequence value.
	/// </summary>
	public const long MinimalSequence = -1L;

	/// <summary>
	/// The maximum sequence value.
	/// </summary>
	public const long MaximumSequence = long.MaxValue;

	public const string ConfigurationSection = "Euonia:Bus";

	public const string DeadLetterTransport = "DeadLetter";

	public const string DefaultTransportSection = $"{ConfigurationSection}:DefaultTransport";
}