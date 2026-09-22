using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 使用 UUID v7 算法生成 <see cref="Guid"/> 值的生成器。
/// </summary>
/// <remarks>
/// UUID v7 将时间戳置于高位，因此生成的值具有近似单调递增的特性，可减少数据库索引页分裂。
/// </remarks>
public class GuidVersion7ValueGenerator : ValueGenerator<Guid>
{
	/// <summary>
	/// 获取一个值，指示生成器是否产生临时值。
	/// </summary>
	/// <value>始终为 <c>false</c>，即生成的值可直接持久化。</value>
	public override bool GeneratesTemporaryValues { get; } = false;

	/// <summary>
	/// 为指定实体条目生成下一个 <see cref="Guid"/> 值。
	/// </summary>
	/// <param name="entry">需要生成值的实体条目。</param>
	/// <returns>新生成的 UUID v7 值。</returns>
	public override Guid Next(EntityEntry entry)
	{
		return Guid.CreateVersion7();
	}
}
