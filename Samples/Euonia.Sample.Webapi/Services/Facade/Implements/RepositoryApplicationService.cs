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
/// 仓库应用服务实现：清单/分页经查询请求（读侧行级下推），
/// 创建/更新/删除经命令总线走工厂边界，push 走命令对象，详情按身份走读模型或工厂判定。
/// 越权（<see cref="System.Security.SecurityException"/>）与不存在（<see cref="NotFoundException"/>）
/// 原样上抛，由全局异常中间件映射为 403 / 404。
/// </summary>
internal class RepositoryApplicationService(IRepositoryStore store, IScopeGuard guard, IObjectFactory factory)
	: BaseApplicationService, IRepositoryApplicationService
{
	public Task<PagedResult<RepositoryDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default)
	{
		return Bus.CallAsync(new RepositoryListQueryRequest(keyword, sortBy, page, pageSize), cancellationToken);
	}

	public async Task<RepositoryDto> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		if (guard.User?.Identity?.IsAuthenticated != true)
		{
			// 匿名：不走工厂（无码无主体），用读模型 + 默认策略判定；公开行放行，私有/机密 403。
			var entity = await store.GetAsync(id, cancellationToken);
			if (entity == null)
			{
				throw new NotFoundException($"Repository with ID '{id}' not found.");
			}

			if (!guard.Allows(entity))
			{
				throw new System.Security.SecurityException("当前用户没有权限访问该仓库。");
			}

			return ToDto(entity);
		}

		var repository = await factory.FetchAsync<CodeRepository>(id, cancellationToken);
		return ToDto(repository);
	}

	public async Task<string> CreateAsync(RepositoryCreateInput input, CancellationToken cancellationToken = default)
	{
		var command = new CreateRepositoryCommand
		{
			Name = input.Name,
			TeamId = input.TeamId,
			Level = input.Level,
			IsPublic = input.IsPublic
		};
		return await CallAsync(command, cancellationToken);
	}

	// 命令类不归类为请求类型，无法用 Bus.CallAsync 直接取回结果；与 User 切片一致，
	// 用 SendAsync(command, Subject<T>) 感知命令完成并回收处理器返回值。
	private async Task<string> CallAsync(CreateRepositoryCommand command, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<string>();
		var subject = new Subject<string>();
		subject.Subscribe(tcs.SetResult, tcs.SetException, () => tcs.TrySetResult(null));

		await Bus.SendAsync(command, subject, cancellationToken);
		return await tcs.Task;
	}

	public Task UpdateAsync(string id, RepositoryUpdateInput input, CancellationToken cancellationToken = default)
	{
		var command = new UpdateRepositoryCommand
		{
			Id = id,
			Name = input.Name,
			Level = input.Level,
			IsPublic = input.IsPublic
		};
		return Bus.SendAsync(command, cancellationToken);
	}

	public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		return Bus.SendAsync(new DeleteRepositoryCommand { Id = id }, cancellationToken);
	}

	public async Task PushAsync(string id, CancellationToken cancellationToken = default)
	{
		var command = await factory.CreateAsync<PushRepositoryCommand>(id, cancellationToken);
		await factory.ExecuteAsync(command, cancellationToken);
	}

	public async Task<object> ExplainAsync(string id, CancellationToken cancellationToken = default)
	{
		var repository = await factory.FetchAsync<CodeRepository>(id, cancellationToken);
		return guard.Explain(repository);
	}

	private static RepositoryDto ToDto(CodeRepository repository)
	{
		return new RepositoryDto(repository.Id, repository.Name, repository.TeamId, repository.OwnerId, repository.Level, repository.IsPublic);
	}
}