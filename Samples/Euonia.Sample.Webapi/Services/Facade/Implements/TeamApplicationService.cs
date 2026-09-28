using System.Reactive.Subjects;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Permissions;
using Nerosoft.Euonia.Sample.Persist.Requests;

namespace Nerosoft.Euonia.Sample.Facade.Implements;

/// <summary>
/// 团队应用服务实现：清单/分页经查询请求（读侧行级下推），
/// 创建/更新/删除经命令总线走工厂边界，详情走工厂判定。
/// 越权（<see cref="System.Security.SecurityException"/>）与不存在（<see cref="NotFoundException"/>）
/// 原样上抛，由全局异常中间件映射为 403 / 404。
/// </summary>
internal class TeamApplicationService(IObjectFactory factory)
	: BaseApplicationService, ITeamApplicationService
{
	public Task<PagedResult<TeamDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default)
	{
		return Bus.CallAsync(new TeamListQueryRequest(keyword, sortBy, page, pageSize), cancellationToken);
	}

	public async Task<TeamDto> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		var team = await factory.FetchAsync<Team>(id, cancellationToken);
		return new TeamDto(team.Id, team.Name, team.LeaderId);
	}

	public async Task<string> CreateAsync(TeamCreateInput input, CancellationToken cancellationToken = default)
	{
		var command = new CreateTeamCommand { Name = input.Name };
		return await CallAsync(command, cancellationToken);
	}

	// 命令类不归类为请求类型，无法用 Bus.CallAsync 直接取回结果；与 User 切片一致，
	// 用 SendAsync(command, Subject<T>) 感知命令完成并回收处理器返回值。
	private async Task<string> CallAsync(CreateTeamCommand command, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<string>();
		var subject = new Subject<string>();
		subject.Subscribe(tcs.SetResult, tcs.SetException, () => tcs.TrySetResult(null));

		await Bus.SendAsync(command, subject, cancellationToken);
		return await tcs.Task;
	}

	public Task UpdateAsync(string id, TeamUpdateInput input, CancellationToken cancellationToken = default)
	{
		return Bus.SendAsync(new UpdateTeamCommand { Id = id, Name = input.Name }, cancellationToken);
	}

	public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		return Bus.SendAsync(new DeleteTeamCommand { Id = id }, cancellationToken);
	}
}