using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 使用雪花算法生成 64 位整数标识的生成器。
/// </summary>
/// <remarks>
/// 生成的标识在分布式环境下全局唯一且趋势递增，适合作 <see cref="long"/> 类型主键。
/// </remarks>
public class SnowflakeIdValueGenerator : ValueGenerator<long>
{
    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <inheritdoc />
    public override long Next(EntityEntry entry)
    {
        var id = ObjectId.NewSnowflake();
        return id;
    }
}