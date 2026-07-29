using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// Generates the current UTC time.
/// </summary>
public class UtcTimeValueGenerator : ValueGenerator<DateTime>
{
	/// <inheritdoc />
	public override DateTime Next(EntityEntry entry)
	{
		return DateTime.UtcNow;
	}

	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;
}