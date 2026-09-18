using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 顺序 GUID（Sequential GUID）值生成器。
/// </summary>
/// <remarks>
/// 生成按字符串形式排序递增的 GUID，使主键在数据库索引中近似有序，从而降低索引碎片。
/// </remarks>
public class SequentialGuidValueGenerator : ValueGenerator<Guid>
{
    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <inheritdoc />
    public override Guid Next(EntityEntry entry)
    {
        return ObjectId.NewGuid(GuidType.SequentialAsString);
    }
}