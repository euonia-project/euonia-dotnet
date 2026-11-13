using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

/// <summary>
/// <see cref="AuthorizationRecord"/> 的 EF Core 实体映射配置。
/// </summary>
[DbContext(typeof(SampleDataContext))]
public class AuthorizationRecordEntityConfiguration : IEntityTypeConfiguration<AuthorizationRecord>
{
	/// <summary>
	/// 配置授权记录表名、由用户标识、类型与值组成的复合主键，以及各列的约束。
	/// </summary>
	/// <param name="builder">用于构建实体映射的构建器。</param>
	public void Configure(EntityTypeBuilder<AuthorizationRecord> builder)
	{
		builder.ToTable("authorization_record");
		builder.HasKey(x => new { x.UserId, x.Kind, x.Value });

		builder.Property(x => x.UserId)
		       .HasColumnName("user_id")
		       .HasMaxLength(64)
		       .IsRequired();

		builder.Property(x => x.Kind)
		       .HasColumnName("kind")
		       .HasMaxLength(16)
		       .IsRequired();

		builder.Property(x => x.Value)
		       .HasColumnName("value")
		       .HasMaxLength(128)
		       .IsRequired();
	}
}