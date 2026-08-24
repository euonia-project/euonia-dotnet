using Mapster;
using Newtonsoft.Json;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 用于将 JSON 格式的 <see cref="string"/> 映射为 <typeparamref name="TDest"/> 类型的映射注册。
/// </summary>
/// <typeparam name="TDest">反序列化的目标类型。</typeparam>
public class JsonValueConverter<TDest> : IRegister
{
	/// <inheritdoc />
	public void Register(TypeAdapterConfig config)
	{
		config.ForType<string, TDest>().MapWith(source => JsonConvert.DeserializeObject<TDest>(source));
	}
}