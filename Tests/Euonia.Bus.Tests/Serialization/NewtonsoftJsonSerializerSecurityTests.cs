using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 Newtonsoft 序列化器的安全回归测试。
/// </summary>
/// <remarks>
/// 消息体可能来自 broker 或远程端，不能信任其中的 <c>$type</c>；
/// 序列化器必须保持 <c>TypeNameHandling.None</c>，避免任意类型实例化。
/// </remarks>
public class NewtonsoftJsonSerializerSecurityTests
{
	[Fact]
	public void Deserialize_DoesNotHonorTypeNameHandling()
	{
		DangerousProbe.Activations = 0;

		var serializer = new NewtonsoftJsonSerializer(Options.Create(new MessageSerializerOptions()));
		var json = $$"""{"$type":"{{typeof(DangerousProbe).AssemblyQualifiedName}}","Value":"unsafe"}""";

		var result = serializer.Deserialize<object>(json);

		Assert.Equal(0, DangerousProbe.Activations);
		Assert.IsNotType<DangerousProbe>(result);
	}

	private sealed class DangerousProbe
	{
		public static int Activations;

		public DangerousProbe()
		{
			Interlocked.Increment(ref Activations);
		}

		public string Value { get; set; }
	}
}
