using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// Generates compact, short unique string identifiers for EF Core entities.
/// </summary>
/// <remarks>
/// This value generator produces a short, URL-friendly identifier by:
/// <list type="bullet">
/// <item>1) Generating a Snowflake-style 64-bit value via <c>ObjectId.NewSnowflake()</c>.</item>
/// <item>2) Encoding that 64-bit value to a compact string using <c>ShortUniqueId.Default.EncodeInt64</c>.</item>
/// </list>
/// The generator returns permanent values (not temporary) suitable for use as stable primary keys
/// or unique business identifiers within the application's persistence layer.
/// </remarks>
public class ShortUniqueIdValueGenerator : ValueGenerator<string>
{
	/// <summary>
	/// Generates the next short unique identifier for the given entity entry.
	/// </summary>
	/// <param name="entry">
	/// The <see cref="EntityEntry"/> for which the value is being generated.
	/// The entry can be inspected if generation needs to consider entity state or properties.
	/// </param>
	/// <returns>
	/// A compact string representation of a newly generated 64-bit Snowflake identifier.
	/// The returned string is produced by encoding the Snowflake value using
	/// <c>ShortUniqueId.Default.EncodeInt64</c>.
	/// </returns>
	public override string Next(EntityEntry entry)
	{
		var snowflake = ObjectId.NewSnowflake();
		return ShortUniqueId.Default.EncodeInt64(snowflake);
	}

	/// <summary>
	/// Indicates whether the values generated are temporary.
	/// </summary>
	/// <value>
	/// <c>false</c> because this generator produces permanent identifiers intended for storage.
	/// </value>
	public override bool GeneratesTemporaryValues => false;
}