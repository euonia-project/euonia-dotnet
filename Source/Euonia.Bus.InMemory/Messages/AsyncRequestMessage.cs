using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于异步请求消息的 <see langword="class"/>，既可直接使用，也可通过派生类使用。
/// </summary>
/// <typeparam name="T">要发起的请求类型。</typeparam>
public class AsyncRequestMessage<T>
{
	private Task<T>? _response;

	/// <summary>
	/// 获取消息响应。
	/// </summary>
	/// <exception cref="InvalidOperationException">当 <see cref="HasReceivedResponse"/> 为 <see langword="false"/> 时引发。</exception>
	public Task<T> Response
	{
		get
		{
			if (!HasReceivedResponse)
			{
				ThrowInvalidOperationExceptionForNoResponseReceived();
			}

			return _response!;
		}
	}

	/// <summary>
	/// 获取一个值，指示是否已为当前实例分配了响应。
	/// </summary>
	public bool HasReceivedResponse { get; private set; }

	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复请求消息的响应。</param>
	/// <exception cref="InvalidOperationException">当 <see cref="Response"/> 已被设置时引发。</exception>
	public void Reply(T response)
	{
		Reply(Task.FromResult(response));
	}

	/// <summary>
	/// 回复当前请求消息。
	/// </summary>
	/// <param name="response">用于回复请求消息的响应。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="response"/> 为 <see langword="null"/> 时引发。</exception>
	/// <exception cref="InvalidOperationException">当 <see cref="Response"/> 已被设置时引发。</exception>
	public void Reply(Task<T> response)
	{
		ArgumentAssert.ThrowIfNull(response);

		if (HasReceivedResponse)
		{
			ThrowInvalidOperationExceptionForDuplicateReply();
		}

		HasReceivedResponse = true;

		_response = response;
	}

	/// <inheritdoc cref="Task{T}.GetAwaiter"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public TaskAwaiter<T> GetAwaiter()
	{
		return Response.GetAwaiter();
	}

	/// <summary>
	/// 当响应不可用时引发 <see cref="InvalidOperationException"/>。
	/// </summary>
	[DoesNotReturn]
	private static void ThrowInvalidOperationExceptionForNoResponseReceived()
	{
		throw new InvalidOperationException(Resources.IDS_NO_RESPONSE_RECEIVED);
	}

	/// <summary>
	/// 当 <see cref="Reply(T)"/> 或 <see cref="Reply(Task{T})"/> 被调用两次时引发 <see cref="InvalidOperationException"/>。
	/// </summary>
	[DoesNotReturn]
	private static void ThrowInvalidOperationExceptionForDuplicateReply()
	{
		throw new InvalidOperationException(Resources.IDS_RESPONSE_ALREADY_ISSUED);
	}
}