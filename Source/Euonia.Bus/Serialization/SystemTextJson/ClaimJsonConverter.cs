using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nerosoft.Euonia.Bus.SystemTextJson;

/// <summary>
/// <see cref="Claim"/> 的 JSON 序列化/反序列化转换器（基于 System.Text.Json）。
/// 支持将 <see cref="Claim"/> 对象序列化为包含 Type、Value、ValueType、Issuer、OriginalIssuer 字段的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimJsonConverter : JsonConverter<Claim>
{
	/// <inheritdoc />
	public override Claim Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}

		string type = null;
		string value = null;
		string valueType = null;
		string issuer = null;
		string originalIssuer = null;

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
				case nameof(Claim.Type):
					type = reader.GetString();
					break;
				case nameof(Claim.Value):
					value = reader.TokenType switch
					{
						JsonTokenType.String => reader.GetString(),
						_ => GetRawValue(ref reader)
					};
					break;
				case nameof(Claim.ValueType):
					valueType = reader.GetString();
					break;
				case nameof(Claim.Issuer):
					issuer = reader.GetString();
					break;
				case nameof(Claim.OriginalIssuer):
					originalIssuer = reader.GetString();
					break;
				default:
					reader.Skip();
					break;
			}
		}

		return new Claim(type ?? string.Empty, value ?? string.Empty, valueType, issuer, originalIssuer);
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Claim value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString(nameof(Claim.Type), value.Type);
		writer.WriteString(nameof(Claim.Value), value.Value);
		writer.WriteString(nameof(Claim.ValueType), value.ValueType);
		writer.WriteString(nameof(Claim.Issuer), value.Issuer);
		writer.WriteString(nameof(Claim.OriginalIssuer), value.OriginalIssuer);
		writer.WriteEndObject();
	}

	/// <summary>
	/// 获取非字符串类型令牌的原始 JSON 表示。
	/// </summary>
	private static string GetRawValue(ref Utf8JsonReader reader)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		return document.RootElement.GetRawText();
	}
}
