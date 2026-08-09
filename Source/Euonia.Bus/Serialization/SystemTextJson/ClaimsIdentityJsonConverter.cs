using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nerosoft.Euonia.Bus.SystemTextJson;

/// <summary>
/// <see cref="ClaimsIdentity"/> 的 JSON 序列化/反序列化转换器（基于 System.Text.Json）。
/// 支持将 <see cref="ClaimsIdentity"/> 对象序列化为包含 AuthenticationType、IsAuthenticated、Claims 等字段的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimsIdentityJsonConverter : JsonConverter<ClaimsIdentity>
{
	/// <inheritdoc />
	public override ClaimsIdentity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}

		IEnumerable<Claim> claims = null;
		string authenticationType = null;
		string nameClaimType = null;
		string roleClaimType = null;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				break;
			}

			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException();
			}

			var propertyName = reader.GetString();

			reader.Read();

			switch (propertyName)
			{
				case nameof(ClaimsIdentity.Claims):
					claims = JsonSerializer.Deserialize<IEnumerable<Claim>>(ref reader, options);
					break;
				case nameof(ClaimsIdentity.AuthenticationType):
					authenticationType = reader.GetString();
					break;
				case nameof(ClaimsIdentity.NameClaimType):
					nameClaimType = reader.GetString();
					break;
				case nameof(ClaimsIdentity.RoleClaimType):
					roleClaimType = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new ClaimsIdentity(claims, authenticationType, nameClaimType, roleClaimType);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, ClaimsIdentity value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(ClaimsIdentity.AuthenticationType), value.AuthenticationType);
		writer.WriteBoolean(nameof(ClaimsIdentity.IsAuthenticated), value.IsAuthenticated);

		if (value.Actor != null)
		{
			writer.WritePropertyName(nameof(ClaimsIdentity.Actor));
			JsonSerializer.Serialize(writer, value.Actor, options);
		}
		else
		{
			writer.WriteNull(nameof(ClaimsIdentity.Actor));
		}

		if (value.BootstrapContext != null)
		{
			writer.WritePropertyName(nameof(ClaimsIdentity.BootstrapContext));
			JsonSerializer.Serialize(writer, value.BootstrapContext, options);
		}
		else
		{
			writer.WriteNull(nameof(ClaimsIdentity.BootstrapContext));
		}

		writer.WritePropertyName(nameof(ClaimsIdentity.Claims));
		JsonSerializer.Serialize(writer, value.Claims, options);

		writer.WriteString(nameof(ClaimsIdentity.Label), value.Label);
		writer.WriteString(nameof(ClaimsIdentity.Name), value.Name);
		writer.WriteString(nameof(ClaimsIdentity.NameClaimType), value.NameClaimType);
		writer.WriteString(nameof(ClaimsIdentity.RoleClaimType), value.RoleClaimType);
		writer.WriteEndObject();
	}
}
