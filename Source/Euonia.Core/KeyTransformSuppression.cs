namespace Nerosoft.Euonia;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的键转换抑制状态。
/// </summary>
public static class KeyTransformSuppression
{
	private static readonly AsyncLocal<int> _depth = new();

	/// <summary>
	/// 获取一个值，指示当前异步流是否处于键转换逃逸作用域。
	/// </summary>
	public static bool IsSuppressed => _depth.Value > 0;

	/// <summary>
	/// 进入逃逸作用域；释放后恢复。
	/// </summary>
	/// <returns>可恢复的逃逸作用域。</returns>
	public static IKeyTransformSuppressScope Suppress()
	{
		_depth.Value++;
		return new Scope();
	}

	private sealed class Scope : IKeyTransformSuppressScope
	{
		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_depth.Value--;
		}
	}
}
