using AutoMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 用于将 <see cref="byte"/> 数组转换为 <see cref="long"/> 的转换器。
/// </summary>
public class ByteArrayToInt64Converter : IValueConverter<byte[], long>
{
	/// <inheritdoc />
	public long Convert(byte[] sourceMember, ResolutionContext context)
    {
        return BitConverter.ToInt64(sourceMember);
    }
}