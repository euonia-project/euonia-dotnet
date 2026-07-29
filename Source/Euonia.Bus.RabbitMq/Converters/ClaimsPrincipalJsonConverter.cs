using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.RabbitMq;

internal class ClaimsPrincipalJsonConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is not ClaimsPrincipal principal)
		{
			return;
		}

		var jsonObject = new JObject
		{
			{ nameof(ClaimsPrincipal.Identities), JArray.FromObject(principal.Identities, serializer) }
		};

		jsonObject.WriteTo(writer);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}

		var jsonObject = JObject.Load(reader);

		if (!jsonObject.HasValues)
		{
			return null;
		}

		var identities = jsonObject.GetValue(nameof(ClaimsPrincipal.Identities), token =>
		{
			if (!token.HasValues)
			{
				return null;
			}

			{
			}

			return token.ToObject<IEnumerable<ClaimsIdentity>>(serializer);
		});
		return identities == null ? null : new ClaimsPrincipal(identities);
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(ClaimsPrincipal) == objectType;
	}
}