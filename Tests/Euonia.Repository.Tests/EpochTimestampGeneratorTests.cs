using System.Collections.Concurrent;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

public class EpochTimestampGeneratorTests
{
	[Fact]
	public void Next_ReturnsValueNotEarlierThanCurrentTime()
	{
		var generator = new EpochTimestampGenerator();
		var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		var value = generator.Next(null!);

		// 进程内单调递增可能把取值推到当前时钟之前（同毫秒并发取值时），故只断言不早于当前时间。
		Assert.True(value >= before, $"Expected value >= {before}, but got {value}.");
	}

	[Fact]
	public void GeneratesTemporaryValues_IsFalse()
	{
		Assert.False(new EpochTimestampGenerator().GeneratesTemporaryValues);
	}

	/// <summary>
	/// 中7：同毫秒并发取值不得重复，排序后必须严格递增（时钟回拨也不倒退）。
	/// 修复前直接返回 UtcNow 毫秒，1000 次并发取值几乎必然出现重复。
	/// </summary>
	[Fact]
	public void Next_ConcurrentCalls_ProduceUniqueStrictlyIncreasingValues()
	{
		var generator = new EpochTimestampGenerator();
		var values = new ConcurrentBag<long>();

		Parallel.For(0, 1000, _ => values.Add(generator.Next(null!)));

		var sorted = values.OrderBy(x => x).ToArray();
		Assert.Equal(1000, sorted.Length);
		Assert.Equal(1000, sorted.Distinct().Count());
		for (var i = 1; i < sorted.Length; i++)
		{
			Assert.True(sorted[i] > sorted[i - 1], $"Values at index {i - 1} ({sorted[i - 1]}) and {i} ({sorted[i]}) are not strictly increasing.");
		}
	}
}
