using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

[DbContext(typeof(SampleDataContext))]
public class AuthorizationRecordEntityConfiguration : IEntityTypeConfiguration<AuthorizationRecord>
{
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