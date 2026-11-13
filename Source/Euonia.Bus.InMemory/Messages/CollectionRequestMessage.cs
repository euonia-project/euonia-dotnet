using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于可接收多个响应的请求消息的 <see langword="class"/>，既可直接使用，也可通过派生类使用。
/// </summary>
/// <typeparam name="T">要发起的请求类型。</typeparam>
public class CollectionRequestMessage<T> : IEnumerable<T>
{
	private readonly List<T> _responses = new();

	/// <summary>
	/// 获取消息响应。
	/// </summary>
	public IReadOnlyCollection<T> Responses => _responses;

	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复请求消息的响应。</param>
	public void Reply(T response)
	{
		_responses.Add(response);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public IEnumerator<T> GetEnumerator()
	{
		return _responses.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}