using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

[DbContext(typeof(SampleDataContext))]
public class TeamMemberEntityConfiguration : IEntityTypeConfiguration<TeamMember>
{
	public void Configure(EntityTypeBuilder<TeamMember> builder)
	{
		builder.ToTable("team_member");
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
		       .HasColumnName("id")
		       .HasMaxLength(64);

		builder.Property(x => x.TeamId)
		       .HasColumnName("team_id")
		       .HasMaxLength(64)
		       .IsRequired();

		builder.Property(x => x.UserId)
		       .HasColumnName("user_id")
		       .HasMaxLength(64)
		       .IsRequired();

		builder.Property(x => x.Status)
		       .HasColumnName("status")
		       .HasMaxLength(16)
		       .IsRequired()
		       .HasDefaultValue(TeamMemberStatus.Active);

		// 一个用户在一个团队里只有一条关系（重新加入即恢复该行，而不是再插一行）
		builder.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();

		// 子表维度的下推条件是 EXISTS (… WHERE team_id = ? AND status = ? AND user_id IN (?))：
		// 这条索引正是为它建的（见 Euonia.Security/README.md §8 性能注意事项）。
		builder.HasIndex(x => new { x.TeamId, x.Status, x.UserId });
	}
}
