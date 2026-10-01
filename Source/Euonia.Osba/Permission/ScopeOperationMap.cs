using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 目标对象状态到业务操作的<b>唯一</b>映射。
/// </summary>
/// <remarks>
/// <para>
/// 这条映射是策略键解析的根：键只由操作决定，操作只由本映射决定。
/// 工厂边界（<see cref="ObjectAuthorization"/> / <see cref="ScopeAuthorization"/>）、
/// <c>IObjectOperationResolver</c>（把对象状态交给引擎解析策略键）与单行/下推判定必须共用本类——
/// 若各自实现一遍，「工厂按 Update 判、单行判定按 Create 判」这类漂移会让策略键静默错位。
/// </para>
/// <para>
/// 注意 <see cref="ObjectEditState"/> 由调用方通过公开的 <c>MarkAsNew</c>/<c>MarkAsChanged</c>/
/// <c>MarkAsDeleted</c> 设置。这不会构成越权通道：它改变的是<b>实际执行的操作</b>，
/// 而每个操作各自应用自己的策略，不存在「把同一变更路由到更宽松的键」。
/// </para>
/// </remarks>
public static class ScopeOperationMap
{
	/// <summary>
	/// 依据目标对象的类型与状态解析要执行的业务操作。
	/// </summary>
	/// <param name="target">目标对象。</param>
	/// <returns>业务操作。</returns>
	/// <exception cref="InvalidOperationException">可编辑对象的状态为 <see cref="ObjectEditState.None"/>，或目标为只读对象时抛出。</exception>
	public static string Resolve(object target)
	{
		return target switch
		{
			IEditableObject editable => FromEditState(editable.State),
			ICommandObject => BusinessOperation.Execute,
			IReadOnlyObject => throw new InvalidOperationException(Resources.IDS_OPERATION_NOT_APPLY_READONLY),
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
	/// 供「非保存」场景使用——业务方法内部直接问 <c>IScopeGuard</c> 某一行是否可见时，
	/// 对象状态通常为 <see cref="ObjectEditState.None"/>，应当回落到默认键而不是抛异常。
	/// </remarks>
	public static bool TryResolve(object target, out string operation)
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
	public static string FromEditState(ObjectEditState state)
	{
		return state switch
		{
			ObjectEditState.New => BusinessOperation.Create,
			ObjectEditState.Changed => BusinessOperation.Update,
			ObjectEditState.Deleted => BusinessOperation.Delete,
			_ => throw new InvalidOperationException(Resources.IDS_NO_PENDING_CHANGE)
		};
	}
}
