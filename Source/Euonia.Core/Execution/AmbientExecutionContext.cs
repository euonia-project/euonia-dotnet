using System.Diagnostics;

namespace Nerosoft.Euonia.Execution;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的当前执行上下文存储。
/// </summary>
/// <remarks>
/// <para>
/// 采用静态存储而非 DI 作用域字段，确保 <c>UnitOfWorkManager</c> 等组件创建的子作用域
/// 能够沿异步流继承同一份身份快照；这也是既有 <c>DefaultRequestContextAccessor</c> 的存储模式。
/// </para>
/// <para>
/// 作用域必须按后进先出顺序释放。<see cref="Push"/> 返回的句柄在释放时会校验当前快照仍是
/// 本次推入的实例，乱序释放只触发调试断言、不覆盖其他作用域的值。
/// </para>
/// </remarks>
public static class AmbientExecutionContext
{
	private static readonly AsyncLocal<ExecutionContextValues?> _current = new();

	/// <summary>
	/// 获取当前异步流的执行上下文快照；尚未安装时为 <c>null</c>。
	/// </summary>
	public static ExecutionContextValues? Current => _current.Value;

	/// <summary>
	/// 基于当前快照克隆并应用变更，返回可恢复原值的释放句柄。
	/// </summary>
	/// <param name="configure">修改克隆快照的委托。</param>
	/// <returns>释放时恢复进入前快照的句柄。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="configure"/> 为 <c>null</c> 时抛出。</exception>
	public static IDisposable Push(Action<ExecutionContextValues> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		var previous = _current.Value;
		var next = previous?.Clone() ?? new ExecutionContextValues();
		configure(next);
		_current.Value = next;
		return new Restore(previous, next);
	}

	/// <summary>
	/// 以全新快照推入执行上下文，不继承当前流中的任何身份信息。
	/// </summary>
	/// <param name="configure">修改新快照的委托。</param>
	/// <returns>释放时恢复进入前快照的句柄。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="configure"/> 为 <c>null</c> 时抛出。</exception>
	/// <remarks>
	/// 用于后台任务等需要“切断当前环境、建立新根”的场景；普通嵌套应使用 <see cref="Push"/>。
	/// </remarks>
	public static IDisposable PushNew(Action<ExecutionContextValues> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);

		var previous = _current.Value;
		var next = new ExecutionContextValues();
		configure(next);
		_current.Value = next;
		return new Restore(previous, next);
	}

	/// <summary>
	/// 清除当前异步流的执行上下文；主要用于测试隔离。
	/// </summary>
	public static void Reset()
	{
		_current.Value = null;
	}

	private sealed class Restore : IDisposable
	{
		private readonly ExecutionContextValues? _previous;
		private readonly ExecutionContextValues _pushed;

		public Restore(ExecutionContextValues? previous, ExecutionContextValues pushed)
		{
			_previous = previous;
			_pushed = pushed;
		}

		public void Dispose()
		{
			if (!ReferenceEquals(_current.Value, _pushed))
			{
				// 乱序释放时不覆盖其他作用域的值；仅记录诊断，不抛异常（Debug.Fail 在测试宿主会中断进程）。
				Trace.TraceWarning("Execution context scope was disposed out of order.");
				return;
			}

			_current.Value = _previous;
		}
	}
}
