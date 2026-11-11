using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Auditing.EfCore;

/// <summary>
/// 审计表结构约定。
/// </summary>
public static class AuditingModelBuilderExtensions
{
	/// <summary>
	/// 应用审计记录与动作的映射（表名、列名、索引）。
	/// </summary>
	/// <param name="modelBuilder">模型构建器。</param>
	/// <returns>模型构建器。</returns>
	public static ModelBuilder ApplyAuditingModel(this ModelBuilder modelBuilder)
	{
		ArgumentNullException.ThrowIfNull(modelBuilder);

		modelBuilder.Entity<AuditingRecord>(builder =>
		{
			builder.ToTable("euonia_audit_log");
			builder.HasKey(record => record.Id);

			builder.Property(record => record.Id).HasColumnName("id");
			builder.Property(record => record.ScopeId).HasColumnName("scope_id").HasMaxLength(256).IsRequired();
			builder.Property(record => record.TenantId).HasColumnName("tenant_id").HasMaxLength(64);
			builder.Property(record => record.TenantName).HasColumnName("tenant_name").HasMaxLength(256);
			builder.Property(record => record.TargetTenantId).HasColumnName("target_tenant_id").HasMaxLength(64);
			builder.Property(record => record.IsCrossTenant).HasColumnName("is_cross_tenant");
			builder.Property(record => record.CrossTenantReason).HasColumnName("cross_tenant_reason").HasMaxLength(512);
			builder.Property(record => record.UserId).HasColumnName("user_id").HasMaxLength(128);
			builder.Property(record => record.UserName).HasColumnName("user_name").HasMaxLength(256);
			builder.Property(record => record.ClientId).HasColumnName("client_id").HasMaxLength(128);
			builder.Property(record => record.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
			builder.Property(record => record.TraceId).HasColumnName("trace_id").HasMaxLength(64);
			builder.Property(record => record.SpanId).HasColumnName("span_id").HasMaxLength(32);
			builder.Property(record => record.RequestTraceId).HasColumnName("request_trace_id").HasMaxLength(64);
			builder.Property(record => record.UnitOfWorkId).HasColumnName("unit_of_work_id").HasMaxLength(64);
			builder.Property(record => record.ApplicationName).HasColumnName("application_name").HasMaxLength(128);
			builder.Property(record => record.ExecutionTime).HasColumnName("execution_time");
			builder.Property(record => record.ExecutionDuration).HasColumnName("execution_duration");
			builder.Property(record => record.ClientIpAddress).HasColumnName("client_ip_address").HasMaxLength(64);
			builder.Property(record => record.ClientName).HasColumnName("client_name").HasMaxLength(128);
			builder.Property(record => record.BrowserInfo).HasColumnName("browser_info").HasMaxLength(512);
			builder.Property(record => record.HttpMethod).HasColumnName("http_method").HasMaxLength(16);
			builder.Property(record => record.HttpStatusCode).HasColumnName("http_status_code");
			builder.Property(record => record.Url).HasColumnName("url").HasMaxLength(2048);
			builder.Property(record => record.IsSuccess).HasColumnName("is_success");
			builder.Property(record => record.ExtraProperties).HasColumnName("extra_properties");
			builder.Property(record => record.RecordedAt).HasColumnName("recorded_at");

			builder.HasIndex(record => record.ScopeId).IsUnique().HasDatabaseName("ux_audit_log_scope");
			builder.HasIndex(record => record.ExecutionTime).HasDatabaseName("ix_audit_log_time");
			builder.HasIndex(record => new { record.TenantId, record.ExecutionTime }).HasDatabaseName("ix_audit_log_tenant_time");
			builder.HasIndex(record => new { record.TargetTenantId, record.ExecutionTime }).HasDatabaseName("ix_audit_log_target_time");
			builder.HasIndex(record => new { record.UserId, record.ExecutionTime }).HasDatabaseName("ix_audit_log_user_time");
			builder.HasIndex(record => record.CorrelationId).HasDatabaseName("ix_audit_log_correlation");

			builder.HasMany(record => record.Actions)
			       .WithOne()
			       .HasForeignKey("LogId")
			       .OnDelete(DeleteBehavior.Cascade);

		});

		modelBuilder.Entity<AuditingAction>(action =>
		{
			action.ToTable("euonia_audit_action");
			action.HasKey(item => item.ActionId);

			action.Property<Guid>("LogId").HasColumnName("log_id");
			action.Property(item => item.ActionId).HasColumnName("id");
			action.Property(item => item.ParentActionId).HasColumnName("parent_action_id");
			action.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(320).IsRequired();
			action.Property(item => item.ServiceName).HasColumnName("service_name").HasMaxLength(512);
			action.Property(item => item.MethodName).HasColumnName("method_name").HasMaxLength(256);
			action.Property(item => item.ActionName).HasColumnName("action_name").HasMaxLength(256);
			action.Property(item => item.ExecutionTime).HasColumnName("execution_time");
			action.Property(item => item.ExecutionDuration).HasColumnName("execution_duration");
			action.Property(item => item.IsSuccess).HasColumnName("is_success");
			action.Property(item => item.Parameters).HasColumnName("parameters");
			action.Property(item => item.Result).HasColumnName("result");
			action.Property(item => item.ExceptionType).HasColumnName("exception_type").HasMaxLength(512);
			action.Property(item => item.ExceptionMessage).HasColumnName("exception_message").HasMaxLength(2048);
			action.Property(item => item.ExceptionStackTrace).HasColumnName("exception_stack_trace");
			action.Property(item => item.ExtraProperties).HasColumnName("extra_properties");

			action.HasIndex("LogId").HasDatabaseName("ix_audit_action_log");
			action.HasIndex("LogId", nameof(AuditingAction.IdempotencyKey))
			      .IsUnique()
			      .HasDatabaseName("ux_audit_action_key");
		});

		return modelBuilder;
	}
}
