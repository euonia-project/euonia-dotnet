namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 针对 <see cref="ShortUniqueId"/> 边界输入的回归测试。
/// </summary>
public class ShortUniqueIdTests
{
	[Fact]
	public void EncodeInt64_WithSmallAlphabet_ShouldRoundTrip()
	{
		// 回归测试：缓冲区固定按 20 计算，小字符集下 long.MaxValue 需要 20 位数字，
		// 加上 lottery 字符后写入溢出。
		var id = new ShortUniqueId("salt", 0, "0123456789abcdef", "012345");

		var encoded = id.EncodeInt64(long.MaxValue);

		Assert.False(string.IsNullOrEmpty(encoded));
		Assert.Equal(long.MaxValue, id.DecodeSingleInt64(encoded));
	}

	[Fact]
	public void DecodeSingleInt64_WithGuardOnlyHash_ShouldNotThrowIndexOutOfRange()
	{
		// 回归测试：仅由守卫字符组成的输入会让 Split 返回 0 个区间，
		// 旧实现随后直接索引 ranges[0] 与空 span。
		var id = new ShortUniqueId();

		var exception = Record.Exception(() => id.DecodeSingleInt64("a"));

		Assert.IsNotType<IndexOutOfRangeException>(exception);
		Assert.False(id.TryDecodeSingleInt64("a", out _));
	}
}
