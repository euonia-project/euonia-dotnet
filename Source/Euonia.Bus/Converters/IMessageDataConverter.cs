namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义将消息数据流转换为指定类型对象的转换器契约。
/// </summary>
/// <typeparam name="T">转换后的目标类型。</typeparam>
public interface IMessageDataConverter<T>
{
	/// <summary>
	/// 从指定的消息数据流中读取内容并转换为 <typeparamref name="T"/> 类型的对象。
	/// </summary>
	/// <param name="stream">包含消息数据的输入流。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步转换操作的任务，包含转换后的 <typeparamref name="T"/> 对象。</returns>
	Task<T> ConvertAsync(Stream stream, CancellationToken cancellationToken);
}