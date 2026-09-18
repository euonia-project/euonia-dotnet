using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 生成行版本（RowVersion）并发令牌值的生成器。
/// </summary>
/// <remarks>
/// 以当前 UTC 时间的 <see cref="DateTime.Ticks"/> 字节序作为版本值，
/// 每次保存时都会变化，可用于乐观并发冲突检测。
/// </remarks>
public class RowVersionValueGenerator : ValueGenerator<byte[]>
{
    /// <inheritdoc />
    public override byte[] Next(EntityEntry entry)
    {
        var ticks = DateTime.UtcNow.Ticks;
        return BitConverter.GetBytes(ticks);
    }

    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;
}