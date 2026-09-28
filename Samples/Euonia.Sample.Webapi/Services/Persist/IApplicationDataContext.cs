using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 数据上下文的公开访问面：暴露授权数据、账号与保存入口。
/// 实现为内部的 <see cref="SampleDataContext"/>，便于把内部上下文限制在持久化层。
/// </summary>
public interface IApplicationDataContext
{
	/// <summary>用户账号。</summary>
	DbSet<UserEntity> Users { get; }

	/// <summary>代码仓库。</summary>
	DbSet<CodeRepository> CodeRepositories { get; }

	/// <summary>团队。</summary>
	DbSet<Team> Teams { get; }

	/// <summary>项目。</summary>
	DbSet<Project> Projects { get; }

	/// <summary>授权数据（角色 / 权限码 / 团队范围 / 显示名）。</summary>
	DbSet<AuthorizationRecord> Authorizations { get; }

	/// <summary>保存上下文中的所有变更。</summary>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>把实体标记为已修改（写入更新前的对象数据）。</summary>
	EntityEntry Update(object entity);
}