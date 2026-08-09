namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 直接返回消息数据流本身的转换器，适用于以流方式传递消息的场景。
/// </summary>
public class StreamMessageDataConverter : IMessageDataConverter<Stream>
{
	/// <inheritdoc/>
	public Task<Stream> ConvertAsync(Stream stream, CancellationToken cancellationToken)
	{
		return Task.FromResult(stream);
	}
}