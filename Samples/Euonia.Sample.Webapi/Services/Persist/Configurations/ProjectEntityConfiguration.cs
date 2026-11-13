using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

/// <summary>
/// <see cref="Project"/> 的 EF Core 实体映射配置。
/// </summary>
[DbContext(typeof(SampleDataContext))]
public class ProjectEntityConfiguration : IEntityTypeConfiguration<Project>
{
	/// <summary>
	/// 配置项目表名、主键与列映射，并忽略业务对象基类中不属于持久化模型的属性。
	/// </summary>
	/// <param name="builder">用于构建实体映射的构建器。</param>
	public void Configure(EntityTypeBuilder<Project> builder)
	{
		builder.ToTable("project");
		builder.HasKey(x => x.Id);
		// 业务对象基类向 EF 暴露的属性不属于持久化模型，按约定会被尝试映射，需显式忽略
		builder.Ignore(x => x.BusinessContext);
		builder.Ignore(x => x.State);
		builder.Ignore(x => x.CheckObjectRulesOnDelete);

		builder.Property(x => x.Id)
		       .HasColumnName("id")
		       .HasMaxLength(64);

		builder.Property(x => x.Name)
		       .HasColumnName("name")
		       .HasMaxLength(100)
		       .IsRequired();

		builder.Property(x => x.Description)
		       .HasColumnName("description")
		       .HasMaxLength(500);

		builder.Property(x => x.OwnerId)
		       .HasColumnName("owner_id")
		       .HasMaxLength(64)
		       .IsRequired();

		builder.Property(x => x.IsArchived)
		       .HasColumnName("is_archived")
		       .IsRequired()
		       .HasDefaultValue(false);
	}
}