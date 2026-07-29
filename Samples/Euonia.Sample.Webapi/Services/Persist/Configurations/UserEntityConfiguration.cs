using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Configurations;

[DbContext(typeof(SampleDataContext))]
public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
	public void Configure(EntityTypeBuilder<UserEntity> builder)
	{
		builder.ToTable("user");
		builder.HasKey(x => x.Id);

		builder.HasIndex(x => x.Username).HasDatabaseName("user_idx_username");
		builder.HasIndex(x => x.Email).HasDatabaseName("user_idx_email");
		builder.HasIndex(x => x.Phone).HasDatabaseName("user_idx_phone");
		builder.HasTombstoneIndex();

		builder.ShortUniqueId();

		builder.Property(x => x.Username)
		       .HasColumnName("username")
		       .HasMaxLength(64)
		       .IsRequired()
		       .IsUnicode();

		builder.Property(x => x.PasswordHash)
		       .HasColumnName("password_hash")
		       .HasMaxLength(512);

		builder.Property(x => x.PasswordSalt)
		       .HasColumnName("password_salt")
		       .HasMaxLength(64);

		builder.Property(x => x.Nickname)
		       .HasColumnName("nickname")
		       .HasMaxLength(32)
		       .IsUnicode();

		builder.Property(x => x.Email)
		       .HasColumnName("email")
		       .HasMaxLength(255);

		builder.Property(x => x.Phone)
		       .HasColumnName("phone")
		       .HasMaxLength(32);

		builder.Property(x => x.AccessFailedCount)
		       .HasColumnName("access_failed_count")
		       .HasDefaultValue(0);

		builder.Property(x => x.LockoutEnd)
		       .HasColumnName("lockout_end");

		builder.Property(x => x.PasswordChangedTime)
		       .HasColumnName("password_changed_at");

		builder.CreatedAtUtc();

		builder.UpdatedAtUtc();

		builder.ConfigureTombstoneProperty();

		builder.HasMany(x => x.Roles)
		       .WithOne(x => x.UserEntity)
		       .HasForeignKey(x => x.UserId)
		       .OnDelete(DeleteBehavior.Cascade);
	}
}