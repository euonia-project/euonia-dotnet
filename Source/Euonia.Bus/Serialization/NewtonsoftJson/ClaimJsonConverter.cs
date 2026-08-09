using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.NewtonsoftJson;

/// <summary>
/// <see cref="Claim"/> 的 JSON 序列化/反序列化转换器。
/// 支持将 <see cref="Claim"/> 对象序列化为包含 Type、Value、ValueType、Issuer、OriginalIssuer 字段的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimJsonConverter : JsonConverter
{
	/// <summary>
	/// 判断是否可以转换指定的类型。
	/// </summary>
	/// <param name="objectType">要检查的类型。</param>
	/// <returns>当类型为 <see cref="Claim"/> 时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public override bool CanConvert(Type objectType)
	{
		return (objectType == typeof(Claim));
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
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