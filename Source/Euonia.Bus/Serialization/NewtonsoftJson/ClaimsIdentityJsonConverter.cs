using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.NewtonsoftJson;

/// <summary>
/// <see cref="ClaimsIdentity"/> 的 JSON 序列化/反序列化转换器。
/// 支持将 <see cref="ClaimsIdentity"/> 对象序列化为包含 AuthenticationType、IsAuthenticated、Claims 等字段的 JSON 对象，以及反向反序列化。
/// </summary>
internal class ClaimsIdentityJsonConverter : JsonConverter
{
	/// <summary>
	/// 判断是否可以转换指定的类型。
	/// </summary>
	/// <param name="objectType">要检查的类型。</param>
	/// <returns>当类型为 <see cref="ClaimsIdentity"/> 时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public override bool CanConvert(Type objectType)
	{
		return typeof(ClaimsIdentity) == objectType;
	}

	/// <inheritdoc />
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
}