namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 
/// </summary>
public class StringMessageDataConverter : IMessageDataConverter<string>
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<string> ConvertAsync(Stream stream, CancellationToken cancellationToken)
	{
		using var ms = new MemoryStream();

		await stream.CopyToAsync(ms, 4096, cancellationToken).ConfigureAwait(false);

		return Encoding.UTF8.GetString(ms.ToArray());
	}
}