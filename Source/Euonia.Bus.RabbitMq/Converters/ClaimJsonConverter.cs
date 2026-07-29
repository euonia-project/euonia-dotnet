using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.RabbitMq;

internal class ClaimJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return (objectType == typeof(Claim));
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is not Claim claim)
		{
			return;
		}

		var jsonObject = new JObject
		{
			{ nameof(Claim.Type), claim.Type },
			{ nameof(Claim.Value), claim.Value },
			{ nameof(Claim.ValueType), claim.ValueType },
			{ nameof(Claim.Issuer), claim.Issuer },
			{ nameof(Claim.OriginalIssuer), claim.OriginalIssuer }
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

		var type = jsonObject.GetValue(nameof(Claim.Type), token => token.Value<string>());

		var value = jsonObject.GetValue(nameof(Claim.Value), token =>
		{
			return token.Type switch
			{
				JTokenType.String => token.Value<string>(),
				_ => token.ToString(Formatting.None)
			};
		});

		var valueType = jsonObject.GetValue(nameof(Claim.ValueType), token => token.Value<string>());
		var issuer = jsonObject.GetValue(nameof(Claim.Issuer), token => token.Value<string>());
		var originalIssuer = jsonObject.GetValue(nameof(Claim.OriginalIssuer), token => token.Value<string>());
		return new Claim(type ?? string.Empty, value ?? string.Empty, valueType, issuer, originalIssuer);
	}
}