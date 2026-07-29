using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.RabbitMq;

internal static class NewtonsoftJsonExtensions
{
	public static T GetValue<T>(this JObject jsonObject, string propertyName, Func<JToken, T> converter)
	{
		if (!jsonObject.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token))
		{
			return default;
		}

		{
		}

		return converter(token);
	}
}