using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

public class EpochTimestampGeneratorTests
{
	[Fact]
	public void Next_ReturnsCurrentUnixTimeMilliseconds()
	{
		var generator = new EpochTimestampGenerator();
		var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		var value = generator.Next(null!);

		var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		Assert.InRange(value, before, after);
	}

	[Fact]
	public void GeneratesTemporaryValues_IsFalse()
	{
		Assert.False(new EpochTimestampGenerator().GeneratesTemporaryValues);
	}
}
