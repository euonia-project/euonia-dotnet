using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// <see cref="ClaimsPrincipal"/> 的 JSON 序列化/反序列化转换器。
/// 支持将 <see cref="ClaimsPrincipal"/> 对象序列化为包含 Identities 数组的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimsPrincipalJsonConverter : JsonConverter
{
	/// <summary>
	/// 判断是否可以转换指定的类型。
	/// </summary>
	/// <param name="objectType">要检查的类型。</param>
	/// <returns>当类型为 <see cref="ClaimsPrincipal"/> 时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public override bool CanConvert(Type objectType)
	{
		return typeof(ClaimsPrincipal) == objectType;
	}

	/// <inheritdoc />
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
}