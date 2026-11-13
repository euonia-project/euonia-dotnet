namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息正文内容的契约，支持以流、字节数组或字符串形式读取。
/// </summary>
public interface IMessageContent
{
	/// <summary>
	/// 获取消息内容长度。
	/// </summary>
	long? Length { get; }

	/// <summary>
	/// 以流的形式读取消息正文
	/// </summary>
	/// <returns>包含消息正文的可读流。</returns>
	Stream ReadAsStream();

	/// <summary>
	/// 以字节数组的形式读取消息正文
	/// </summary>
	/// <returns>消息正文的字节数组。</returns>
	byte[] ReadAsBytes();

	/// <summary>
	/// 以字符串的形式读取消息正文
	/// </summary>
	/// <returns>消息正文的字符串表示。</returns>
	string ReadAsString();
}