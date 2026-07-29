namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The business object operation factory.
/// </summary>
public interface IObjectFactory
{
	/// <summary>
	/// Creates an instance of the specified target type using the provided criteria.
	/// </summary>
	/// <typeparam name="TTarget">The type of object to create.</typeparam>
	/// <param name="criteria">An array of criteria used to determine how the target instance is created. The interpretation of these values
	/// depends on the implementation.</param>
	/// <returns>An instance of type TTarget created according to the specified criteria.</returns>
	TTarget Create<TTarget>(params object[] criteria);

	/// <summary>
	/// Retrieves an instance of the specified type that matches the given criteria.
	/// </summary>
	/// <remarks>If no matching instance is found, the method may return null or throw an exception, depending on
	/// the implementation. The criteria parameter allows for flexible querying, but callers should consult the specific
	/// implementation for supported criteria types and behaviors.</remarks>
	/// <typeparam name="TTarget">The type of object to retrieve.</typeparam>
	/// <param name="criteria">An array of criteria used to identify or filter the target instance. The interpretation of each criterion depends
	/// on the implementation.</param>
	/// <returns>An instance of type TTarget that matches the specified criteria.</returns>
	TTarget Fetch<TTarget>(params object[] criteria);

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