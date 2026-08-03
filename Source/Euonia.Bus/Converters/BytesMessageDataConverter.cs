namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 将消息数据流转换为字节数组的转换器。
/// </summary>
public class BytesMessageDataConverter : IMessageDataConverter<byte[]>
{
	/// <summary>
	/// 从指定的流中读取全部数据并转换为字节数组。
	/// 将流内容以 4096 字节的缓冲块复制到内存流中，返回其字节表示。
	/// </summary>
	/// <param name="stream">包含消息数据的输入流。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步转换操作的任务，包含从流中读取的字节数组。</returns>
	public async Task<byte[]> ConvertAsync(Stream stream, CancellationToken cancellationToken)
	{
		using var ms = new MemoryStream();

		await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

		return ms.ToArray();
	}
}