using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.RabbitMq;

internal class ClaimsIdentityJsonConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is not ClaimsIdentity identity)
		{
			return;
		}

		var jsonObject = new JObject
		{
			{ nameof(ClaimsIdentity.AuthenticationType), identity.AuthenticationType },
			{ nameof(ClaimsIdentity.IsAuthenticated), identity.IsAuthenticated },
			{ nameof(ClaimsIdentity.Actor), identity.Actor == null ? null : JObject.FromObject(identity.Actor, serializer) },
			{ nameof(ClaimsIdentity.BootstrapContext), identity.BootstrapContext == null ? null : JObject.FromObject(identity.BootstrapContext, serializer) },
			{ nameof(ClaimsIdentity.Claims), new JArray(identity.Claims.Select(x => JObject.FromObject(x, serializer))) },
			{ nameof(ClaimsIdentity.Label), identity.Label },
			{ nameof(ClaimsIdentity.Name), identity.Name },
			{ nameof(ClaimsIdentity.NameClaimType), identity.NameClaimType },
			{ nameof(ClaimsIdentity.RoleClaimType), identity.RoleClaimType }
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


		var claims = jsonObject.GetValue(nameof(ClaimsIdentity.Claims), token =>
		{
			if (!token.HasValues)
			{
				return [];
			}

			{
			}
			return token.ToObject<IEnumerable<Claim>>(serializer);
		});
		var authenticationType = jsonObject.GetValue(nameof(ClaimsIdentity.AuthenticationType), token => token.Value<string>());
		var nameClaimType = jsonObject.GetValue(nameof(ClaimsIdentity.NameClaimType), token => token.Value<string>());
		var roleClaimType = jsonObject.GetValue(nameof(ClaimsIdentity.RoleClaimType), token => token.Value<string>());
		return new ClaimsIdentity(claims, authenticationType, nameClaimType, roleClaimType);
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(ClaimsIdentity) == objectType;
	}
}