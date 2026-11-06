using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 示例持久化层的 EF Core 模型构建扩展：统一配置审计字段、墓碑标记、主键生成与时间戳列。
/// </summary>
internal static class EntityTypeBuilderExtensions
{
	/// <summary>
	/// 为实体类型 <typeparamref name="TEntity"/> 配置审计属性。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，必须实现 <see cref="IAuditable"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
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
	/// 为实体类型 <typeparamref name="TEntity"/> 配置墓碑属性。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，必须实现 <see cref="ITombstone"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
	public static void ConfigureTombstoneProperty<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, ITombstone
	{
		builder.Property(t => t.IsDeleted)
			   .HasColumnName("is_deleted")
			   .HasDefaultValue(false)
			   .IsRequired();
	}

	/// <summary>
	/// 为实体类型 <typeparamref name="TEntity"/> 配置雪花 ID 属性。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，其主键类型必须为 <see cref="long"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
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
	/// 为实体类型 <typeparamref name="TEntity"/> 配置短唯一 ID 属性。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，其主键类型必须为 <see cref="string"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
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
	/// 为实体类型 <typeparamref name="TEntity"/> 配置使用 UTC 时间的 CreatedAt 属性。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，必须实现 <see cref="IHasCreateTime"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
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
	/// 为实体类型 <typeparamref name="TEntity"/> 配置使用 UTC 时间的 UpdatedAt 属性。
	/// 时间由 <see cref="UtcTimeValueGenerator"/> 在写入时填充；与 created_at 一样保持普通列，
	/// 不做 ValueGeneratedOnAddOrUpdate——SQLite 的 RETURNING 会把该列视为数据库生成、
	/// 期望表上存在默认值，而模型没有默认值会直接命中 NOT NULL 约束。
	/// </summary>
	/// <typeparam name="TEntity">要配置的实体类型，必须实现 <see cref="IHasUpdateTime"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
	public static void UpdatedAtUtc<TEntity>(this EntityTypeBuilder<TEntity> builder)
		where TEntity : class, IHasUpdateTime
	{
		builder.Property(t => t.UpdatedAt)
			   .HasColumnName("updated_at")
			   .HasValueGenerator<UtcTimeValueGenerator>()
			   .IsRequired();
	}

	/// <summary>
	/// 为实体类型 <typeparamref name="TEntity"/> 配置墓碑索引。
	/// </summary>
	/// <typeparam name="TEntity">要建立索引的实体类型，必须实现 <see cref="ITombstone"/>。</typeparam>
	/// <param name="builder">目标实体的类型构建器。</param>
	/// <param name="tableName">用于生成索引名称的表名；为 <c>null</c> 时使用实体类型名的小写形式。</param>
	public static void HasTombstoneIndex<TEntity>(this EntityTypeBuilder<TEntity> builder, string? tableName = null)
		where TEntity : class, ITombstone
	{
		builder.HasIndex(t => t.IsDeleted)
			   .HasDatabaseName($"{tableName ?? typeof(TEntity).Name.ToLower()}_idx_tombstone");
	}
}
