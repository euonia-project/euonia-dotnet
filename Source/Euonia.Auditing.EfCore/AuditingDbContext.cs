using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Auditing.EfCore;

/// <summary>
/// 独立的审计数据上下文；不得与业务上下文共用连接或事务。
/// </summary>
public class AuditingDbContext : DbContext
{
	/// <summary>
	/// 初始化 <see cref="AuditingDbContext"/> 的新实例。
	/// </summary>
	/// <param name="options">上下文选项。</param>
	public AuditingDbContext(DbContextOptions<AuditingDbContext> options)
		: base(options)
	{
	}

	/// <summary>
	/// 获取审计记录集合。
	/// </summary>
	public DbSet<AuditingRecord> Records => Set<AuditingRecord>();

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyAuditingModel();
		base.OnModelCreating(modelBuilder);
	}
}
