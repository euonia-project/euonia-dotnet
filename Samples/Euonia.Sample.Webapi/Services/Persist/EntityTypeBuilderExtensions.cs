using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

internal static class EntityTypeBuilderExtensions
{
	/// <summary>
	/// Configure auditable properties for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="builder"></param>
	public static void ConfigureAuditableProperties<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IAuditable
	{
		builder.Property(t => t.CreatedAt)
			   .HasColumnName("created_at")
			   .HasValueGenerator<UtcTimeValueGenerator>()
			   .ValueGeneratedOnAdd()
			   .IsRequired();

		builder.Property(t => t.UpdatedAt)
			   .HasColumnName("updated_at")
			   .HasValueGenerator<UtcTimeValueGenerator>()
			   .ValueGeneratedOnAddOrUpdate()
			   .IsRequired();

		builder.Property(t => t.CreatedBy)
			   .HasColumnName("created_by")
			   .HasMaxLength(64)
			   .IsRequired()
			   .IsUnicode();

		builder.Property(t => t.UpdatedBy)
			   .HasColumnName("updated_by")
			   .HasMaxLength(64)
			   .IsRequired()
			   .IsUnicode();

		// builder.Property(t => t.IsDeleted)
		// 	   .HasColumnName("is_deleted")
		// 	   .HasDefaultValue(false)
		// 	   .IsRequired();
		//
		// builder.Property(t => t.DeletedAt)
		// 	   .HasColumnName("deleted_at");
		//
		// builder.Property(t => t.DeletedBy)
		// 	   .HasColumnName("deleted_by")
		// 	   .HasMaxLength(64)
		// 	   .IsUnicode();
	}

	/// <summary>
	/// Configure tombstone properties for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="builder"></param>
	public static void ConfigureTombstoneProperty<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, ITombstone
	{
		builder.Property(t => t.IsDeleted)
			   .HasColumnName("is_deleted")
			   .HasDefaultValue(false)
			   .IsRequired();
	}

	/// <summary>
	/// Configure Snowflake ID property for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="builder"></param>
	public static void SnowflakeId<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IEntity<long>
	{
		builder.HasKey(t => t.Id);
		builder.Property(t => t.Id)
			   .HasColumnName("id")
			   .IsRequired()
			   .HasValueGenerator<SnowflakeIdValueGenerator>()
			   .ValueGeneratedOnAdd();
	}

	/// <summary>
	/// Configure Short Unique ID property for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <typeparam name="TEntity"></typeparam>
	public static void ShortUniqueId<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IEntity<string>
	{
		builder.HasKey(t => t.Id);
		builder.Property(t => t.Id)
			   .HasColumnName("id")
			   .IsRequired()
			   .HasValueGenerator<ShortUniqueIdValueGenerator>()
			   .ValueGeneratedOnAdd();
	}

	/// <summary>
	/// Configure CreatedAt property with UTC time for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="builder"></param>
	/// <typeparam name="TEntity"></typeparam>
	public static void CreatedAtUtc<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IHasCreateTime
	{
		builder.Property(t => t.CreatedAt)
			   .HasColumnName("created_at")
			   .HasValueGenerator<UtcTimeValueGenerator>()
			   .ValueGeneratedOnAdd()
			   .IsRequired();
	}

	/// <summary>
	/// Configure UpdatedAt property with UTC time for entity type <typeparamref name="TEntity"/>.
	/// 时间由 <see cref="UtcTimeValueGenerator"/> 在写入时填充；与 created_at 一样保持普通列，
	/// 不做 ValueGeneratedOnAddOrUpdate——SQLite 的 RETURNING 会把该列视为数据库生成、
	/// 期望表上存在默认值，而模型没有默认值会直接命中 NOT NULL 约束。
	/// </summary>
	public static void UpdatedAtUtc<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IHasUpdateTime
	{
		builder.Property(t => t.UpdatedAt)
			   .HasColumnName("updated_at")
			   .HasValueGenerator<UtcTimeValueGenerator>()
			   .IsRequired();
	}

	/// <summary>
	/// Configure tombstone index for entity type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <typeparam name="TEntity"></typeparam>
	/// <param name="builder"></param>
	/// <param name="tableName"></param>
	public static void HasTombstoneIndex<TEntity>(this EntityTypeBuilder<TEntity> builder, string tableName = null)
		where TEntity : class, ITombstone
	{
		builder.HasIndex(t => t.IsDeleted)
			   .HasDatabaseName($"{tableName ?? typeof(TEntity).Name.ToLower()}_idx_tombstone");
	}
}
