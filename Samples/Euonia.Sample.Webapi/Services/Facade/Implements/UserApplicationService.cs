using System.Reactive.Subjects;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Persist.Requests;

namespace Nerosoft.Euonia.Sample.Facade.Implements;

internal class UserApplicationService : BaseApplicationService, IUserApplicationService
{
	public async Task<UserDetailDto> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		var request = new UserDetailQueryRequest(id);
		return await Bus.CallAsync(request, null, cancellationToken);
	}

	public Task<List<UserListDto>> FindAsync(string keyword, int skip, int take, CancellationToken cancellationToken = default)
	{
		var request = new UserListQueryRequest(keyword) { Skip = skip, Take = take };
		return Bus.CallAsync(request, null, cancellationToken);
	}

	public async Task<string> CreateAsync(UserCreateDto data, CancellationToken cancellationToken = default)
	{
		var command = TypeAdapter.ProjectedAs<UserCreateCommand>(data);
		var tcs = new TaskCompletionSource<string>();

		var subject = new Subject<string>();
		subject.Subscribe(tcs.SetResult, tcs.SetException, () => tcs.TrySetResult(null));

		await Bus.SendAsync(command, subject, cancellationToken);
		return await tcs.Task;
	}

	/// <summary>
	/// 更新用户资料：把 DTO 投影为命令后发到总线，命令走 <c>UserCommandHandler</c> 直接落库。
	/// </summary>
	public Task UpdateAsync(string id, UserUpdateDto data, CancellationToken cancellationToken = default)
	{
		var command = TypeAdapter.ProjectedAs<UserUpdateCommand>(data);
		command.Id = id;

		// 处理程序返回 Task（无返回值），故走「不期望响应」的重载；
		// 若挂 Subject<bool>，总线回填的是 Unit，回调强转会失败。
		return Bus.SendAsync(command, cancellationToken);
	}

	/// <summary>
	/// 修改当前用户密码：命令对象自带「取当前用户 + 校验旧口令 + 换盐换哈希 + 发布事件」的完整流程，
	/// 不经消息总线处理器，直接由对象工厂创建并执行。
	/// </summary>
	public async Task ChangePasswordAsync(UserChangePasswordDto data, CancellationToken cancellationToken = default)
	{
		var factory = LazyServiceProvider.GetService<IObjectFactory>();
		var command = await factory.CreateAsync<UserChangePasswordCommand>(data.OldPassword, data.NewPassword);
		await factory.ExecuteAsync(command, cancellationToken);
	}
}