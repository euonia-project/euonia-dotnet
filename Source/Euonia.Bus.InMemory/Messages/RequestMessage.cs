using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于请求消息的 <see langword="class"/>，既可直接使用，也可通过派生类使用。
/// </summary>
/// <typeparam name="T">要发起的请求类型。</typeparam>
public class RequestMessage<T>
{
	private T _response = default!;

	/// <summary>
	/// 获取消息响应。
	/// </summary>
	/// <exception cref="InvalidOperationException">当 <see cref="HasReceivedResponse"/> 为 <see langword="false"/> 时引发。</exception>
	public T Response
	{
		get
		{
			if (!HasReceivedResponse)
			{
				ThrowInvalidOperationExceptionForNoResponseReceived();
			}

			return _response;
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
		if (HasReceivedResponse)
		{
			ThrowInvalidOperationExceptionForDuplicateReply();
		}

		HasReceivedResponse = true;

		_response = response;
	}

	/// <summary>
	/// 从给定的 <see cref="RequestMessage{T}"/> 实例隐式获取响应。
	/// </summary>
	/// <param name="message">输入的 <see cref="RequestMessage{T}"/> 实例。</param>
	/// <exception cref="System.ArgumentNullException">当 <paramref name="message"/> 为 <see langword="null"/> 时引发。</exception>
	/// <exception cref="InvalidOperationException">当 <see cref="HasReceivedResponse"/> 为 <see langword="false"/> 时引发。</exception>
	public static implicit operator T(RequestMessage<T> message)
	{
		ArgumentAssert.ThrowIfNull(message);

		return message.Response;
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
	/// 当 <see cref="Reply"/> 被调用两次时引发 <see cref="InvalidOperationException"/>。
	/// </summary>
	[DoesNotReturn]
	private static void ThrowInvalidOperationExceptionForDuplicateReply()
	{
		throw new InvalidOperationException(Resources.IDS_RESPONSE_ALREADY_ISSUED);
	}
}