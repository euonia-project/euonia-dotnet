using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// ULID（Universally Unique Lexicographically Sortable Identifier，可字典序排序的通用唯一标识）值生成器。
/// </summary>
/// <remarks>
/// 生成的字符串标识按字典序排序时与生成时间顺序一致，便于作为可排序的字符串主键使用。
/// </remarks>
public class UlidValueGenerator : ValueGenerator<string>
{
	/// <inheritdoc />
	public override string Next(EntityEntry entry)
	{
		return ObjectId.NewUlid();
	}

	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;
}