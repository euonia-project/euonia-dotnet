namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The business object operation factory.
/// </summary>
public interface IObjectFactory
{
    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke the create method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The create method criteria.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>
    /// The method should named as Create, CreateAsync, FactoryCreate, FactoryCreateAsync, or attributed use <see cref="FactoryCreateAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task<TTarget> CreateAsync<TTarget>(params object[] criteria);

    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke tht fetch method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The create method criteria.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>
    /// The method should named as Fetch, FetchAsync, FactoryFetch, FactoryFetchAsync, or attributed use <see cref="FactoryFetchAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task<TTarget> FetchAsync<TTarget>(params object[] criteria);

    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke the insert method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The insert method criteria.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>
    /// The method should named as Insert, InsertAsync, FactoryInsert, FactoryInsertAsync, or attributed use <see cref="FactoryInsertAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task<TTarget> InsertAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// Invoke the update method of an exists instance of <typeparamref name="TTarget"/>.
	/// </summary>
	/// <typeparam name="TTarget">Type of the target object.</typeparam>
	/// <param name="target"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <remarks>
	/// <para>
	///     For insert operation, the method should named as Insert, InsertAsync, FactoryInsert, FactoryInsertAsync, or attributed use <see cref="FactoryInsertAttribute"/>.
	/// </para>
	/// <para>
	///     For update operation, the method should named as Update, UpdateAsync, FactoryUpdate, FactoryUpdateAsync, or attributed use <see cref="FactoryUpdateAttribute"/>.
	/// </para>
	/// <para>
	///     For delete operation, the method should named as Delete, DeleteAsync, FactoryDelete, FactoryDeleteAsync, or attributed use <see cref="FactoryDeleteAttribute"/>.
	/// </para>
	/// <para>
	///     For execute operation, the method should named as Execute, ExecuteAsync, FactoryExecute, FactoryExecuteAsync, or attributed use <see cref="FactoryExecuteAttribute"/>.
	/// </para>
	/// </remarks>
	Task<TTarget> SaveAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke the update method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The update method criteria.</param>
    /// <returns>The new instance.</returns>
    /// <remarks>
    /// The method should named as Update, UpdateAsync, FactoryUpdate, FactoryUpdateAsync, or attributed use <see cref="FactoryUpdateAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task<TTarget> UpdateAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// Invoke the execute method of an exists command object of <typeparamref name="TTarget"/>.
	/// </summary>
	/// <typeparam name="TTarget">Type of the target object.</typeparam>
	/// <param name="target"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <remarks>
	/// The method should named as Execute, ExecuteAsync, FactoryExecute, FactoryExecuteAsync, or attributed use <see cref="FactoryExecuteAttribute"/>.
	/// </remarks>
	Task<TTarget> ExecuteAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
        where TTarget : ICommandObject;

    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke the execute method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The create method criteria.</param>
    /// <returns></returns>
    /// <remarks>
    /// The execute method should named as Execute, ExecuteAsync, FactoryExecute, FactoryExecuteAsync, or attributed use <see cref="FactoryExecuteAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task<TTarget> ExecuteAsync<TTarget>(params object[] criteria)
        where TTarget : ICommandObject;

    /// <summary>
    /// Create a new instance of <typeparamref name="TTarget"/> and invoke the delete method.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target object.</typeparam>
    /// <param name="criteria">The create method criteria.</param>
    /// <returns></returns>
    /// <remarks>
    /// The method should named as Delete, DeleteAsync, FactoryDelete, FactoryDeleteAsync, or attributed use <see cref="FactoryDeleteAttribute"/>.
    /// Each criteria item must matched the method argument type.
    /// </remarks>
    Task DeleteAsync<TTarget>(params object[] criteria);
}