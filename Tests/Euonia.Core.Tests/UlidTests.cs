namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 针对 <see cref="ObjectId.NewUlid"/> 编码正确性的回归测试。
/// </summary>
public class UlidTests
{
	private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

	/// <summary>
	/// 验证生成的 26 位 ULID 的前 10 个字符可解码出毫秒时间戳，且该时间戳落在调用前后的当前时间范围内。
	/// </summary>
	[Fact]
	public void NewUlid_TimestampPrefix_ShouldMatchCurrentTime()
	{
		// 回归测试：ULID 曾按 5 位一组从最高位错误分片，时间戳被完全写错。
		var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		var ulid = ObjectId.NewUlid();
		var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		Assert.Equal(26, ulid.Length);

		// 前 10 个字符编码 50 位（高 2 位恒为 0），即 48 位毫秒时间戳。
		long timestamp = 0;
		for (var index = 0; index < 10; index++)
		{
			var digit = ALPHABET.IndexOf(ulid[index]);
			Assert.True(digit >= 0, $"invalid ULID character '{ulid[index]}'");
			timestamp = (timestamp << 5) | (uint)digit;
		}

		Assert.InRange(timestamp, before, after);
	}

	/// <summary>
	/// 验证跨毫秒生成的两个 ULID 按序字符串比较时保持升序，从而支持按时间排序。
	/// </summary>
	[Fact]
	public void NewUlid_AcrossMilliseconds_ShouldSortLexicographically()
	{
		var first = ObjectId.NewUlid();
		Thread.Sleep(5);
		var second = ObjectId.NewUlid();

		Assert.True(string.CompareOrdinal(first, second) < 0, $"{first} should sort before {second}");
	}
}
