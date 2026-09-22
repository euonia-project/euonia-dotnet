namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 目标对象状态到业务操作的<b>唯一</b>映射。
/// </summary>
/// <remarks>
/// <para>
/// 这条映射是策略键解析的根：键只由操作决定，操作只由本映射决定。
/// 工厂、规则、<c>CanXObject()</c> 必须共用本类——若各自实现一遍，
/// 「规则按 Update 判、工厂按 Create 判」这类漂移会使规则静默失效。
/// </para>
/// <para>
/// 注意 <see cref="ObjectEditState"/> 由调用方通过公开的 <c>MarkAsNew</c>/<c>MarkAsChanged</c>/
/// <c>MarkAsDeleted</c> 设置。这不会构成越权通道：它改变的是<b>实际执行的操作</b>，
/// 而每个操作各自应用自己的策略，不存在「把同一变更路由到更宽松的键」。
/// </para>
/// </remarks>
internal static class ScopeOperationMap
{
	/// <summary>
	/// 依据目标对象的类型与状态解析要执行的业务操作。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <returns>业务操作。</returns>
	/// <exception cref="InvalidOperationException">可编辑对象的状态为 <see cref="ObjectEditState.None"/>，或目标为只读对象时抛出。</exception>
	internal static BusinessOperation Resolve(object target)
	{
		return target switch
		{
			IEditableObject editable => FromEditState(editable.State),
			ICommandObject => BusinessOperation.Execute,
			IReadOnlyObject => throw new InvalidOperationException("The operation can not apply for ReadOnlyObject."),
			_ => BusinessOperation.Update
		};
	}

	/// <summary>
	/// 尝试依据目标对象的类型与状态解析业务操作。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <param name="operation">解析出的业务操作。</param>
	/// <returns>可解析则返回 <see langword="true"/>；目标无可执行操作（如未变更的可编辑对象、只读对象）时返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 供行级断言（<c>CanAccessRow</c>）等「非保存」场景使用：那里对象状态通常为
	/// <see cref="ObjectEditState.None"/>，应当回落到默认键而不是抛异常。
	/// </remarks>
	internal static bool TryResolve(object target, out BusinessOperation operation)
	{
		switch (target)
		{
			case IEditableObject editable when editable.State != ObjectEditState.None:
				operation = FromEditState(editable.State);
				return true;
			case ICommandObject:
				operation = BusinessOperation.Execute;
				return true;
			case IReadOnlyObject:
				operation = BusinessOperation.Read;
				return true;
			case IEditableObject:
				// 未变更：没有待执行的操作
				operation = BusinessOperation.Read;
				return false;
			default:
				operation = BusinessOperation.Update;
				return false;
		}
	}

	/// <summary>
	/// 把可编辑对象的状态映射为业务操作。
	/// </summary>
	/// <param name="state">对象状态。</param>
	/// <returns>业务操作。</returns>
	/// <exception cref="InvalidOperationException">状态为 <see cref="ObjectEditState.None"/> 时抛出。</exception>
	internal static BusinessOperation FromEditState(ObjectEditState state)
	{
		return state switch
		{
			ObjectEditState.New => BusinessOperation.Create,
			ObjectEditState.Changed => BusinessOperation.Update,
			ObjectEditState.Deleted => BusinessOperation.Delete,
			_ => throw new InvalidOperationException("The object has no pending change to save.")
		};
	}
}
