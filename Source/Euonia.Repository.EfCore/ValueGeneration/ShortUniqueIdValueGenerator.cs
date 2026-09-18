using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 为 EF Core 实体生成紧凑、短小的唯一字符串标识。
/// </summary>
/// <remarks>
/// 该生成器通过以下步骤生成短小且对 URL 友好的标识：
/// <list type="bullet">
/// <item>1）通过 <c>ObjectId.NewSnowflake()</c> 生成雪花算法风格的 64 位值。</item>
/// <item>2）使用 <c>ShortUniqueId.Default.EncodeInt64</c> 将该 64 位值编码为紧凑字符串。</item>
/// </list>
/// 生成器返回永久值（非临时值），适合作为稳定的主键或应用持久层中的唯一业务标识。
/// </remarks>
public class ShortUniqueIdValueGenerator : ValueGenerator<string>
{
	/// <summary>
	/// 为给定实体条目生成下一个短唯一标识。
	/// </summary>
	/// <param name="entry">
	/// 需要生成值的 <see cref="EntityEntry"/>。若生成过程需考虑实体状态或属性，可从该条目中读取。
	/// </param>
	/// <returns>
	/// 新生成的 64 位雪花标识经过 <c>ShortUniqueId.Default.EncodeInt64</c> 编码后的紧凑字符串表示。
	/// </returns>
	public override string Next(EntityEntry entry)
	{
		var snowflake = ObjectId.NewSnowflake();
		return ShortUniqueId.Default.EncodeInt64(snowflake);
	}

	/// <summary>
	/// 指示生成的值是否为临时值。
	/// </summary>
	/// <value>
	/// 始终为 <c>false</c>，因为该生成器产生用于持久化的永久标识。
	/// </value>
	public override bool GeneratesTemporaryValues => false;
}