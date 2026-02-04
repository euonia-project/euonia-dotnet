using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents an editable object that supports asynchronous save, update, and delete operations, while managing its
/// state and validation.
/// </summary>
/// <remarks>
/// This class implements the ISavable and ISavable{T} interfaces, providing event-based notifications
/// when the object is saved. It includes extensible methods for creating, updating, and deleting the object, which can
/// be overridden to customize persistence behavior. Validation is performed before saving, and a ValidationException is
/// thrown if the object is not valid. Subscribers can handle the Saved event to respond to save completion.</remarks>
/// <typeparam name="T">The type of the editable object, which must inherit from EditableObject{T}.
/// </typeparam>
public abstract class EditableObject<T> : ObservableObject<T>, ISavable, ISavable<T>
	where T : EditableObject<T>
{
	/// <summary>
	/// Event raised when the object has been saved.
	/// </summary>
	public event EventHandler<SavedEventArgs> Saved
	{
		add => Events.AddEventHandler(value);
		remove => Events.RemoveEventHandler(value);
	}

	/// <summary>
	/// Called when the object has been saved.
	/// </summary>
	/// <param name="newObject">The newly saved object instance, or <see langword="null"/> if the save failed.</param>
	/// <param name="error">The exception that occurred during the save operation, or <see langword="null"/> if successful.</param>
	/// <param name="userState">Optional user-defined state information associated with the save operation.</param>
	protected virtual void OnSaved(T newObject, Exception error, object userState)
	{
		var args = new SavedEventArgs(newObject, error, userState);
		Events.HandleEvent(this, args, nameof(Saved));
	}

	/// <summary>
	/// Completes the save operation and raises the <see cref="Saved"/> event with the newly saved object.
	/// </summary>
	/// <param name="newObject">The newly saved object instance.</param>
	void ISavable<T>.SaveComplete(T newObject)
	{
		OnSaved(newObject, null, null);
	}

	/// <summary>
	/// Completes the save operation and raises the <see cref="Saved"/> event with the newly saved object.
	/// </summary>
	/// <param name="newObject">The newly saved object instance.</param>
	void ISavable.SaveComplete(object newObject)
	{
		OnSaved((T)newObject, null, null);
	}

	/// <summary>
	/// Saves the object asynchronously.
	/// </summary>
	/// <param name="forceUpdate">
	/// If <see langword="true"/>, marks the object as changed even if its state is <see cref="ObjectEditState.None"/>,
	/// forcing an update operation; otherwise, no operation is performed if the object is unchanged.
	/// </param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task that represents the asynchronous save operation. The task result contains the saved object instance.
	/// </returns>
	public async Task<T> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
	{
		return await SaveAsync(forceUpdate, null, cancellationToken);
	}

	/// <summary>
	/// Saves the object asynchronously (explicit interface implementation).
	/// </summary>
	/// <param name="forceUpdate">
	/// If <see langword="true"/>, marks the object as changed even if its state is <see cref="ObjectEditState.None"/>,
	/// forcing an update operation; otherwise, no operation is performed if the object is unchanged.
	/// </param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task that represents the asynchronous save operation. The task result contains the saved object instance.
	/// </returns>
	async Task<object> ISavable.SaveAsync(bool forceUpdate, CancellationToken cancellationToken)
	{
		return await SaveAsync(forceUpdate, cancellationToken);
	}

	/// <summary>
	/// Save the object.
	/// </summary>
	/// <param name="forceUpdate"></param>
	/// <param name="userState"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="ValidationException"></exception>
	protected virtual async Task<T> SaveAsync(bool forceUpdate, object userState, CancellationToken cancellationToken = default)
	{
		if (State == ObjectEditState.None)
		{
			if (forceUpdate)
			{
				MarkAsChanged();
			}
			else
			{
				return (T)this;
			}
		}

		if (!IsDeleted || CheckObjectRulesOnDelete)
		{
			await Rules.CheckObjectRulesAsync(true, cancellationToken);
			if (Rules.HasRunningRules)
			{
				var task = new TaskCompletionSource<bool>();
				ValidationComplete += OnValidationCompleted;
				await task.Task;

				ValidationComplete -= OnValidationCompleted;

				void OnValidationCompleted(object sender, EventArgs args)
				{
					task.SetResult(true);
				}
			}
		}

		if (!IsValid && (!IsDeleted || CheckObjectRulesOnDelete))
		{
			var errors = Rules.BrokenRules.Select(t => new ValidationResult(t.Property, t.Description));
			throw new ValidationException("Object not valid for save.", errors);
		}

		MarkAsBusy();
		var result = await BusinessContext.GetRequiredService<IObjectFactory>().SaveAsync((T)this, cancellationToken);
		result?.MarkAsIdle();
		MarkAsIdle();
		OnSaved(result, null, userState);
		return result;
	}

	/// <summary>
	/// Create new editable object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Indicates that the object has been saved.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Update the object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// Delete the object.
	/// </summary>
	/// <param name="cancellationToken"></param>
	protected internal virtual Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}