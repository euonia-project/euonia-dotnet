namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents that the implemented classes are savable.
/// </summary>
public interface ISavable
{
    /// <summary>
    /// Event raised when an object has been saved.
    /// </summary>
    event EventHandler<SavedEventArgs> Saved;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newObject"></param>
    void SaveComplete(object newObject);

    /// <summary>
    /// Saves the object to the database.
    /// </summary>
    /// <param name="forceUpdate">true to force the save to be an update.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A new object containing the saved values.</returns>
    Task<object> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents that the implemented classes are savable.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISavable<T> where T : class
{
    /// <summary>
    /// Saves the object to the database.
    /// </summary>
    /// <param name="forceUpdate">true to force the save to be an update.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A new object containing the saved values.</returns>
    Task<T> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newObject"></param>
    void SaveComplete(T newObject);

    /// <summary>
    /// Event raised when an object has been saved.
    /// </summary>
    event EventHandler<SavedEventArgs> Saved;
}