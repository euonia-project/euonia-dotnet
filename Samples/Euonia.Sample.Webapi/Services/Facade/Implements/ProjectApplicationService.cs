using System.Reactive.Subjects;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Sample.Persist.Requests;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Sample.Facade.Implements;

/// <summary>
/// 项目应用服务实现：清单/分页经查询请求（读侧行级下推），
/// 创建/更新/删除经命令总线走工厂边界，归档走命令对象，详情按身份走读模型或工厂判定。
/// 越权（<see cref="System.Security.SecurityException"/>）与不存在（<see cref="NotFoundException"/>）
/// 原样上抛，由全局异常中间件映射为 403 / 404。
/// </summary>
internal class ProjectApplicationService(IProjectStore store, IScopeGuard guard, IObjectFactory factory)
	: BaseApplicationService, IProjectApplicationService
{
	public Task<PagedResult<ProjectDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default)
	{
		return Bus.CallAsync(new ProjectListQueryRequest(keyword, sortBy, page, pageSize), cancellationToken);
	}

	public async Task<ProjectDto> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		if (guard.User?.Identity?.IsAuthenticated != true)
		{
			// 匿名：不走工厂（无码无主体），用读模型 + 默认策略判定。项目没有公开概念，恒判否。
			var entity = await store.GetAsync(id, cancellationToken);
			if (entity == null)
			{
				throw new NotFoundException($"Project with ID '{id}' not found.");
			}

			if (!guard.Allows(entity))
			{
				throw new System.Security.SecurityException("当前用户没有权限访问该项目。");
			}

			return ToDto(entity);
		}

		var project = await factory.FetchAsync<Project>(id, cancellationToken);
		return ToDto(project);
	}

	public async Task<string> CreateAsync(ProjectCreateInput input, CancellationToken cancellationToken = default)
	{
		var command = new CreateProjectCommand
		{
			Name = input.Name,
			Description = input.Description
		};
		return await CallAsync(command, cancellationToken);
	}

	// 命令类不归类为请求类型，无法用 Bus.CallAsync 直接取回结果；与仓库切片一致，
	// 用 SendAsync(command, Subject<T>) 感知命令完成并回收处理器返回值。
	private async Task<string> CallAsync(CreateProjectCommand command, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<string>();
		var subject = new Subject<string>();
		subject.Subscribe(tcs.SetResult, tcs.SetException, () => tcs.TrySetResult(null));

		await Bus.SendAsync(command, subject, cancellationToken);
		return await tcs.Task;
	}

	public Task UpdateAsync(string id, ProjectUpdateInput input, CancellationToken cancellationToken = default)
	{
		var command = new UpdateProjectCommand
		{
			Id = id,
			Name = input.Name,
			Description = input.Description
		};
		return Bus.SendAsync(command, cancellationToken);
	}

	public async Task ArchiveAsync(string id, CancellationToken cancellationToken = default)
	{
		var command = await factory.CreateAsync<ArchiveProjectCommand>(id, cancellationToken);
		await factory.ExecuteAsync(command, cancellationToken);
	}

	public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		return Bus.SendAsync(new DeleteProjectCommand { Id = id }, cancellationToken);
	}

	public async Task<object> ExplainAsync(string id, CancellationToken cancellationToken = default)
	{
		var project = await factory.FetchAsync<Project>(id, cancellationToken);
		return guard.Explain(project);
	}

	private static ProjectDto ToDto(Project project)
	{
		return new ProjectDto(project.Id, project.Name, project.Description, project.OwnerId, project.IsArchived);
	}
}