namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 针对 <see cref="ObjectId.NewUlid"/> 编码正确性的回归测试。
/// </summary>
public class UlidTests
{
	private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

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

	[Fact]
	public void NewUlid_AcrossMilliseconds_ShouldSortLexicographically()
	{
		var first = ObjectId.NewUlid();
		Thread.Sleep(5);
		var second = ObjectId.NewUlid();

		Assert.True(string.CompareOrdinal(first, second) < 0, $"{first} should sort before {second}");
	}
}
