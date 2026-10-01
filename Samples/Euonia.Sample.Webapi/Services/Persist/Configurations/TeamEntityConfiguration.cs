using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

[DbContext(typeof(SampleDataContext))]
public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
	public void Configure(EntityTypeBuilder<Team> builder)
	{
		builder.ToTable("team");
		builder.HasKey(x => x.Id);
		// 业务对象基类向 EF 暴露的属性不属于持久化模型，按约定会被尝试映射，需显式忽略
		builder.Ignore(x => x.BusinessContext);
		builder.Ignore(x => x.State);
		builder.Ignore(x => x.CheckObjectRulesOnDelete);

		// 子表行由 team_member 表承载（见 TeamMemberEntityConfiguration），聚合上只做承载：
		// 它不是本表的一列，也不是 EF 导航属性（否则 EF 会为它推出一条关系）。
		builder.Ignore(x => x.Members);

		builder.Property(x => x.Id)
		       .HasColumnName("id")
		       .HasMaxLength(64);

		builder.Property(x => x.Name)
		       .HasColumnName("name")
		       .HasMaxLength(100)
		       .IsRequired();

		builder.Property(x => x.LeaderId)
		       .HasColumnName("leader_id")
		       .HasMaxLength(64);
	}
}