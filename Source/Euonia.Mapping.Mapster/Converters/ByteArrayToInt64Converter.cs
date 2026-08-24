using Mapster;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 用于将 <see cref="byte"/> 数组转换为 <see cref="long"/> 的映射注册。
/// </summary>
public class ByteArrayToInt64Converter : IRegister
{
	/// <inheritdoc />
	public void Register(TypeAdapterConfig config)
    {
        config.ForType<byte[], long>().MapWith(source => BitConverter.ToInt64(source));
    }
}