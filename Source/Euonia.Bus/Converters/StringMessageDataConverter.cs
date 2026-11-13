namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 将消息流内容按 UTF-8 解码为字符串的数据转换器。
/// </summary>
public class StringMessageDataConverter : IMessageDataConverter<string>
{
	/// <summary>
	/// 读取流中的全部内容并解码为字符串。
	/// </summary>
	/// <param name="stream">包含消息数据的源流。</param>
	/// <param name="cancellationToken">用于取消异步读取操作的令牌。</param>
	/// <returns>流内容按 UTF-8 解码得到的字符串。</returns>
	public async Task<string> ConvertAsync(Stream stream, CancellationToken cancellationToken)
	{
		using var ms = new MemoryStream();

		await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

		return Encoding.UTF8.GetString(ms.ToArray());
	}
}