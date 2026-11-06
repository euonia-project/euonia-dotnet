using System.Buffers;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 使用池化数组的简单缓冲区写入器实现。
/// </summary>
/// <typeparam name="T">要存储在列表中的项类型。</typeparam>
/// <remarks>
/// 此类型是 <see langword="ref"/> <see langword="struct"/>，以避免对象分配，并支持基于模式的
/// <see cref="IDisposable"/>。由于此类型是私有的且只能在父类型内部访问，因此我们无需担心
/// 使用者未正确使用它。
/// </remarks>
internal ref struct ArrayPoolBufferWriter<T>
{
	/// <summary>
	/// 用于扩展空数组的默认缓冲区大小。
	/// </summary>
	private const int DEFAULT_INITIAL_BUFFER_SIZE = 128;

	/// <summary>
	/// 底层的 <typeparamref name="T"/> 数组。
	/// </summary>
	private T[] _array;

	/// <summary>
	/// 映射到 <see cref="_array"/> 的跨度。
	/// </summary>
	/// <remarks>所有写入都通过它完成，以避免协变检查。</remarks>
	private Span<T> _span;

	/// <summary>
	/// 在 <see cref="_array"/> 中的起始偏移量。
	/// </summary>
	private int _index;

	/// <summary>
	/// 创建 <see cref="ArrayPoolBufferWriter{T}"/> 结构的新实例。
	/// </summary>
	/// <returns>新的 <see cref="ArrayPoolBufferWriter{T}"/> 实例。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArrayPoolBufferWriter<T> Create()
	{
		ArrayPoolBufferWriter<T> instance;

		instance._span = instance._array = ArrayPool<T>.Shared.Rent(DEFAULT_INITIAL_BUFFER_SIZE);
		instance._index = 0;

		return instance;
	}

	/// <summary>
	/// 获取包含当前项的 <see cref="ReadOnlySpan{T}"/>。
	/// </summary>
	public ReadOnlySpan<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _span[.._index];
	}

	/// <summary>
	/// 向当前集合添加新项。
	/// </summary>
	/// <param name="item">要添加的项。</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item)
	{
		var span = _span;
		var index = _index;

		if ((uint)index < (uint)span.Length)
		{
			span[index] = item;

			_index = index + 1;
		}
		else
		{
			ResizeBufferAndAdd(item);
		}
	}

	/// <summary>
	/// 重置底层数组及已存储的项。
	/// </summary>
	public void Reset()
	{
		Array.Clear(_array, 0, _index);

		_index = 0;
	}

	/// <summary>
	/// 当没有剩余空间容纳新项时调整 <see cref="_array"/> 的大小，然后添加一项
	/// </summary>
	/// <param name="item">要添加的项。</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ResizeBufferAndAdd(T item)
	{
		var rent = ArrayPool<T>.Shared.Rent(_index << 2);

		Array.Copy(_array, 0, rent, 0, _index);
		Array.Clear(_array, 0, _index);

		ArrayPool<T>.Shared.Return(_array);

		_span = _array = rent;

		_span[_index++] = item;
	}

	/// <inheritdoc cref="IDisposable.Dispose"/>
	public void Dispose()
	{
		Array.Clear(_array, 0, _index);

		ArrayPool<T>.Shared.Return(_array);
	}
}