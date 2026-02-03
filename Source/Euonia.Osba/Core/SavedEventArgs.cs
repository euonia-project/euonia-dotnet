namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents the event is triggered when an object is saved.
/// </summary>
public class SavedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SavedEventArgs"/> class.
	/// </summary>
	/// <param name="newObject"></param>
    public SavedEventArgs(object newObject)
    {
        NewObject = newObject;
    }

	/// <summary>
	/// Initializes a new instance of the <see cref="SavedEventArgs"/> class.
	/// </summary>
	/// <param name="newObject"></param>
	/// <param name="error"></param>
	/// <param name="userState"></param>
    public SavedEventArgs(object newObject, Exception error, object userState)
        : this(newObject)
    {
        Error = error;
        UserState = userState;
    }

	/// <summary>
	/// Gets the new object.
	/// </summary>
    public object NewObject { get; }

	/// <summary>
	/// Gets the error.
	/// </summary>
    public Exception Error { get; }

	/// <summary>
	/// Gets the user state.
	/// </summary>
    public object UserState { get; }
}