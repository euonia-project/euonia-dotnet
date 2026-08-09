using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nerosoft.Euonia.Bus.SystemTextJson;

/// <summary>
/// <see cref="ClaimsPrincipal"/> 的 JSON 序列化/反序列化转换器（基于 System.Text.Json）。
/// 支持将 <see cref="ClaimsPrincipal"/> 对象序列化为包含 Identities 数组的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimsPrincipalJsonConverter : JsonConverter<ClaimsPrincipal>
{
	/// <inheritdoc />
	public override ClaimsPrincipal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}

		IEnumerable<ClaimsIdentity> identities = null;

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
				case nameof(ClaimsPrincipal.Identities):
					identities = JsonSerializer.Deserialize<IEnumerable<ClaimsIdentity>>(ref reader, options);
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return identities == null ? null : new ClaimsPrincipal(identities);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, ClaimsPrincipal value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName(nameof(ClaimsPrincipal.Identities));
		JsonSerializer.Serialize(writer, value.Identities, options);
		writer.WriteEndObject();
	}
}
