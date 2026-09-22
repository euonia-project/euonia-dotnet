using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 生成当前 UTC 时间的值生成器。
/// </summary>
/// <remarks>
/// 用于在写入记录时自动填充创建时间、更新时间等时间字段，保证以 UTC 存储。
/// </remarks>
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