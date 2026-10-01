using System.Security;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Constants;
using Nerosoft.Euonia.Sample.Domain.Permissions;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 对某个仓库执行 push 的命令对象（对齐 SAMPLE.md 的 PushCommand 场景）。
/// 命令体经 <see cref="IObjectFactory.ExecuteAsync{T}"/> 裁决后才执行：
/// <see cref="BusinessOperation.Execute"/> 的类型级闸门（<see cref="RepositoryPermissions.Push"/> 与角色），
/// 行级判定（单行授予 <c>repository:push</c>）在命令体内做，避免「先 push 后判定、拒了也 push」。
/// </summary>
public sealed class PushRepositoryCommand : CommandObjectBase<PushRepositoryCommand>
{
	/// <summary>目标仓库标识（由 <see cref="FactoryCreateAttribute"/> 填充）。</summary>
	public string RepoId { get; set; }

	/// <summary>命令是否已成功执行。</summary>
	public bool Pushed { get; private set; }

	[FactoryCreate]
	private async Task CreateAsync(string repoId, CancellationToken cancellationToken = default)
	{
		RepoId = repoId;
		await Task.CompletedTask;
	}

	[Permission(RepositoryPermissions.Push, RoleName.Developer, RoleName.ProjectManager)]
	[FactoryExecute]
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var repository = await BusinessContext.GetRequiredService<IRepositoryStore>().GetAsync(RepoId, cancellationToken);
		if (repository == null)
		{
			throw new NotFoundException($"Repository with ID '{RepoId}' not found.");
		}

		var guard = BusinessContext.GetRequiredService<IScopeGuard>();
		if (!guard.AllowsObject(repository, RepositoryPermissions.Push))
		{
			throw new SecurityException(guard.ExplainObject(repository, RepositoryPermissions.Push));
		}

		Pushed = true;
	}
}