using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 基于 Unix 时间戳的 <see cref="ValueGenerator{TValue}"/> 实现，为 <see cref="long"/> 类型的属性生成以毫秒计的 UTC 时间戳值。
/// </summary>
/// <remarks>
/// <para>
/// 生成的值是持久的（<see cref="GeneratesTemporaryValues"/> 为 <c>false</c>），
/// 一旦生成即作为实体的最终值写入数据库，不会被 EF Core 临时值替换。
/// </para>
/// <para>
/// 通过静态计数器保证进程内严格单调递增：同一毫秒内的并发取值不会重复，
/// 系统时钟回拨时取值也不会倒退（取当前时间与上次值加一中的较大者）。
/// 仅进程内单调，跨进程或进程重启后不保证唯一。
/// </para>
/// </remarks>
public class EpochTimestampGenerator : ValueGenerator<long>
{
	private static long _last;

	/// <inheritdoc/>
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc/>
	public override long Next(EntityEntry entry)
	{
		while (true)
		{
			var last = Interlocked.Read(ref _last);
			var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			var next = Math.Max(now, last + 1);
			if (Interlocked.CompareExchange(ref _last, next, last) == last)
			{
				return next;
			}
		}
	}
}
