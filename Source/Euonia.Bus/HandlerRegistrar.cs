namespace Nerosoft.Euonia.Bus;

internal static class HandlerRegistrar
{
	/// <summary>
	/// Holds discovered message handler registrations.
	/// </summary>
	private static readonly List<MessageRegistration> _registrations = [];

	/// <summary>
	/// Gets the list of registered message handler registrations.
	/// </summary>
	public static IReadOnlyList<MessageRegistration> Registrations => _registrations;

	public static void RegisterHandlers(IEnumerable<Type> types)
	{
		var registrations = MessageHandlerFinder.Find(types).ToList();
		_registrations.AddRange(registrations);
	}
}