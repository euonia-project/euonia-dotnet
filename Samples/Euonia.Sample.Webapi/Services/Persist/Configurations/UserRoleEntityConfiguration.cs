using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

/// <summary>
/// <see cref="UserRoleEntity"/> 的 EF Core 实体映射配置。
/// </summary>
[DbContext(typeof(SampleDataContext))]
public class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
	/// <summary>
	/// 配置用户角色表名、主键、索引与列映射，以及到用户实体的一对多关系。
	/// </summary>
	/// <param name="builder">用于构建实体映射的构建器。</param>
	public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
	{
		builder.ToTable("user_role");
		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.UserId)
			   .HasDatabaseName("user_role_idx_fk");

		builder.HasIndex(x => new { x.UserId, x.Name })
			   .HasDatabaseName("user_role_idx_unique")
			   .IsUnique();

		builder.ShortUniqueId();

		builder.Property(x => x.UserId)
			   .HasColumnName("user_id")
			   .IsRequired();

		builder.Property(x => x.Name)
			   .HasColumnName("name")
			   .HasMaxLength(100)
			   .IsRequired();

		builder.CreatedAtUtc();

		builder.HasOne(x => x.UserEntity)
			   .WithMany(x => x.Roles)
			   .HasForeignKey(x => x.UserId)
			   .OnDelete(DeleteBehavior.Cascade);
	}
}
