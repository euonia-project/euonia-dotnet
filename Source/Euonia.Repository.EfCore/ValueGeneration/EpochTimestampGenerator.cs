using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 基于 Unix 时间戳的 <see cref="ValueGenerator{TValue}"/> 实现，为 <see cref="long"/> 类型的属性生成以毫秒计的 UTC 时间戳值。
/// </summary>
/// <remarks>
/// 生成的值是持久的（<see cref="GeneratesTemporaryValues"/> 为 <c>false</c>），
/// 一旦生成即作为实体的最终值写入数据库，不会被 EF Core 临时值替换。
/// </remarks>
public class EpochTimestampGenerator : ValueGenerator<long>
{
	/// <inheritdoc/>
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc/>
	public override long Next(EntityEntry entry)
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
