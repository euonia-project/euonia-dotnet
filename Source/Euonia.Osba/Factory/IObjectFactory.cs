namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象操作工厂。
/// </summary>
public interface IObjectFactory
{
	/// <summary>
	/// 使用提供的条件创建指定目标类型的实例。
	/// </summary>
	/// <typeparam name="TTarget">要创建的对象类型。</typeparam>
	/// <param name="criteria">用于确定如何创建目标实例的条件数组。这些值的解释取决于实现。</param>
	/// <returns>根据指定条件创建的 TTarget 类型实例。</returns>
	TTarget Create<TTarget>(params object[] criteria);

	/// <summary>
	/// 检索与给定条件匹配的指定类型的实例。
	/// </summary>
	/// <remarks>如果未找到匹配的实例，此方法可能返回 <c>null</c> 或抛出异常，具体取决于实现。
	/// criteria 参数允许灵活查询，但调用方应查阅特定实现以了解支持的条件类型和行为。</remarks>
	/// <typeparam name="TTarget">要检索的对象类型。</typeparam>
	/// <param name="criteria">用于标识或筛选目标实例的条件数组。每个条件的解释取决于实现。</param>
	/// <returns>与指定条件匹配的 TTarget 类型实例。</returns>
	TTarget Fetch<TTarget>(params object[] criteria);

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用创建方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">创建方法的条件。</param>
	/// <returns>新实例。</returns>
	/// <remarks>
	/// 方法应命名为 Create、CreateAsync、FactoryCreate、FactoryCreateAsync，或使用 <see cref="FactoryCreateAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task<TTarget> CreateAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用获取方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">创建方法的条件。</param>
	/// <returns>新实例。</returns>
	/// <remarks>
	/// 方法应命名为 Fetch、FetchAsync、FactoryFetch、FactoryFetchAsync，或使用 <see cref="FactoryFetchAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task<TTarget> FetchAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用插入方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">插入方法的条件。</param>
	/// <returns>新实例。</returns>
	/// <remarks>
	/// 方法应命名为 Insert、InsertAsync、FactoryInsert、FactoryInsertAsync，或使用 <see cref="FactoryInsertAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task<TTarget> InsertAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// 调用 <typeparamref name="TTarget"/> 现有实例的更新方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="target">要保存的目标对象。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步保存操作的任务，包含保存后的对象实例。</returns>
	/// <remarks>
	/// <para>
	///     对于插入操作，方法应命名为 Insert、InsertAsync、FactoryInsert、FactoryInsertAsync，或使用 <see cref="FactoryInsertAttribute"/> 特性标记。
	/// </para>
	/// <para>
	///     对于更新操作，方法应命名为 Update、UpdateAsync、FactoryUpdate、FactoryUpdateAsync，或使用 <see cref="FactoryUpdateAttribute"/> 特性标记。
	/// </para>
	/// <para>
	///     对于删除操作，方法应命名为 Delete、DeleteAsync、FactoryDelete、FactoryDeleteAsync，或使用 <see cref="FactoryDeleteAttribute"/> 特性标记。
	/// </para>
	/// <para>
	///     对于执行操作，方法应命名为 Execute、ExecuteAsync、FactoryExecute、FactoryExecuteAsync，或使用 <see cref="FactoryExecuteAttribute"/> 特性标记。
	/// </para>
	/// </remarks>
	Task<TTarget> SaveAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default);

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用更新方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">更新方法的条件。</param>
	/// <returns>新实例。</returns>
	/// <remarks>
	/// 方法应命名为 Update、UpdateAsync、FactoryUpdate、FactoryUpdateAsync，或使用 <see cref="FactoryUpdateAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task<TTarget> UpdateAsync<TTarget>(params object[] criteria);

	/// <summary>
	/// 调用 <typeparamref name="TTarget"/> 现有命令对象的执行方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="target">要执行的目标命令对象。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步执行操作的任务，包含执行后的对象实例。</returns>
	/// <remarks>
	/// 方法应命名为 Execute、ExecuteAsync、FactoryExecute、FactoryExecuteAsync，或使用 <see cref="FactoryExecuteAttribute"/> 特性标记。
	/// </remarks>
	Task<TTarget> ExecuteAsync<TTarget>(TTarget target, CancellationToken cancellationToken = default)
		where TTarget : ICommandObject;

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用执行方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">创建方法的条件。</param>
	/// <returns>表示异步执行操作的任务，包含执行后的对象实例。</returns>
	/// <remarks>
	/// 执行方法应命名为 Execute、ExecuteAsync、FactoryExecute、FactoryExecuteAsync，或使用 <see cref="FactoryExecuteAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task<TTarget> ExecuteAsync<TTarget>(params object[] criteria)
		where TTarget : ICommandObject;

	/// <summary>
	/// 创建 <typeparamref name="TTarget"/> 的新实例并调用删除方法。
	/// </summary>
	/// <typeparam name="TTarget">目标对象的类型。</typeparam>
	/// <param name="criteria">创建方法的条件。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	/// <remarks>
	/// 方法应命名为 Delete、DeleteAsync、FactoryDelete、FactoryDeleteAsync，或使用 <see cref="FactoryDeleteAttribute"/> 特性标记。
	/// 每个条件项必须与方法参数类型匹配。
	/// </remarks>
	Task DeleteAsync<TTarget>(params object[] criteria);
}