using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示支持异步保存、更新和删除操作，同时管理其状态和验证的可编辑对象。
/// </summary>
/// <remarks>
/// 此类实现 ISavable 和 ISavable{T} 接口，在对象被保存时提供基于事件的通知。
/// 它包含用于创建、更新和删除对象的可扩展方法，可以重写这些方法以自定义持久化行为。
/// 保存前会执行验证，如果对象无效则抛出 ValidationException。
/// 订阅者可以处理 Saved 事件以响应保存完成。</remarks>
/// <typeparam name="T">可编辑对象的类型，必须继承自 EditableObject{T}。</typeparam>
public abstract class EditableObject<T> : ObservableObject<T>, ISavable, ISavable<T>
	where T : EditableObject<T>
{
	/// <summary>
	/// 对象被保存后引发的事件。
	/// </summary>
	public event EventHandler<SavedEventArgs> Saved
	{
		add => Events.AddEventHandler(value);
		remove => Events.RemoveEventHandler(value);
	}

	/// <summary>
	/// 当对象已被保存时调用。
	/// </summary>
	/// <param name="newObject">新保存的对象实例；如果保存失败则为 <see langword="null"/>。</param>
	/// <param name="error">保存操作期间发生的异常；如果成功则为 <see langword="null"/>。</param>
	/// <param name="userState">与保存操作关联的可选用户定义状态信息。</param>
	protected virtual void OnSaved(T newObject, Exception error, object userState)
	{
		var args = new SavedEventArgs(newObject, error, userState);
		Events.HandleEvent(this, args, nameof(Saved));
	}

	/// <summary>
	/// 完成保存操作，并使用新保存的对象引发 <see cref="Saved"/> 事件。
	/// </summary>
	/// <param name="newObject">新保存的对象实例。</param>
	void ISavable<T>.SaveComplete(T newObject)
	{
		OnSaved(newObject, null, null);
	}

	/// <summary>
	/// 完成保存操作，并使用新保存的对象引发 <see cref="Saved"/> 事件。
	/// </summary>
	/// <param name="newObject">新保存的对象实例。</param>
	void ISavable.SaveComplete(object newObject)
	{
		OnSaved((T)newObject, null, null);
	}

	/// <summary>
	/// 异步保存对象。
	/// </summary>
	/// <param name="forceUpdate">
	/// 如果为 <see langword="true"/>，即使对象状态为 <see cref="ObjectEditState.None"/>，也会将对象标记为已更改，
	/// 强制执行更新操作；否则，如果对象未更改，则不执行任何操作。
	/// </param>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>
	/// 表示异步保存操作的任务。任务结果包含已保存的对象实例。
	/// </returns>
	public async Task<T> SaveAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
	{
		return await SaveAsync(forceUpdate, null, cancellationToken);
	}

	/// <summary>
	/// 异步保存对象（显式接口实现）。
	/// </summary>
	/// <param name="forceUpdate">
	/// 如果为 <see langword="true"/>，即使对象状态为 <see cref="ObjectEditState.None"/>，也会将对象标记为已更改，
	/// 强制执行更新操作；否则，如果对象未更改，则不执行任何操作。
	/// </param>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>
	/// 表示异步保存操作的任务。任务结果包含已保存的对象实例。
	/// </returns>
	async Task<object> ISavable.SaveAsync(bool forceUpdate, CancellationToken cancellationToken)
	{
		return await SaveAsync(forceUpdate, cancellationToken);
	}

	/// <summary>
	/// 保存对象。
	/// </summary>
	/// <param name="forceUpdate">是否强制将保存作为更新操作执行。</param>
	/// <param name="userState">与保存操作关联的用户定义状态信息。</param>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步保存操作的任务，包含保存后的对象实例。</returns>
	/// <exception cref="ValidationException">当对象无效且无法保存时抛出。</exception>
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
	/// 创建新的可编辑对象。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步创建操作的任务。</returns>
	protected internal virtual Task CreateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 指示对象已被保存（插入）。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步插入操作的任务。</returns>
	protected internal virtual Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 更新对象。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步更新操作的任务。</returns>
	protected internal virtual Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 删除对象。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的令牌。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	protected internal virtual Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}