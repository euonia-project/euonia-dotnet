using AutoMapper;
using Newtonsoft.Json;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 用于将 JSON 格式的 <see cref="string"/> 转换为 <typeparamref name="T"/> 类型的转换器。
/// </summary>
/// <typeparam name="T">反序列化的目标类型。</typeparam>
public class JsonValueConverter<T> : IValueConverter<string, T>
{
	/// <inheritdoc />
	public T Convert(string sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(sourceMember);
        }
        catch (Exception)
        {
            return default;
        }
    }
}