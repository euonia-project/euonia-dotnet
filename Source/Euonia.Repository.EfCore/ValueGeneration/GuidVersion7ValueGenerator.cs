using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 
/// </summary>
public class GuidVersion7ValueGenerator : ValueGenerator<Guid>
{
	/// <summary>
	/// 
	/// </summary>
	public override bool GeneratesTemporaryValues { get; } = false;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="entry"></param>
	/// <returns></returns>
	public override Guid Next(EntityEntry entry)
	{
		return Guid.CreateVersion7();
	}
}
