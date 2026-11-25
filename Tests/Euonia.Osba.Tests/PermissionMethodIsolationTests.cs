using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 方法级 <see cref="PermissionAttribute"/> 的隔离语义：只在该方法被工厂调用时生效，不波及同类型的其它入口。
/// </summary>
/// <remarks>
/// <para>
/// 方法级声明只对工厂实际调用的方法生效：闸门接收工厂解析出的 <see cref="MethodInfo"/>，
/// 只读<b>那一个方法</b>的声明；同类型其它入口方法的声明不会叠加到本方法的调用路径上。
/// </para>
/// <para>
/// 用例 1 与 3 互为对偶：授予 <c>iso:update</c> 必须放行，授予 <c>iso:archive</c> 必须拒绝——
/// 后者正是「不因另一个入口的码而放行」的反向证明。
/// </para>
/// </remarks>
public class PermissionMethodIsolationTests
{
	/// <summary>
	/// 只授予 <c>iso:update</c> 时更新保存成功：更新路径不会附带要求同类型其它入口的码（不过度收紧）。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Should_Succeed_When_Only_The_Update_Code_Is_Granted()
	{
		using var scope = CreateScope(["iso:update"], out var provider);

		var order = NewOrder(provider);
		order.MarkAsChanged();

		var saved = await order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Same(order, saved);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 不授予任何权限码时更新被拒绝：方法级声明自动生效，不需要任何入口规则。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Should_Throw_When_No_Code_Is_Granted()
	{
		using var scope = CreateScope([], out var provider);

		var order = NewOrder(provider);
		order.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 只授予 <c>iso:archive</c> 时更新仍被拒绝：archive 的码不能顺带满足 update（反向证明）。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Should_Throw_When_Only_The_Sibling_Code_Is_Granted()
	{
		using var scope = CreateScope(["iso:archive"], out var provider);

		var order = NewOrder(provider);
		order.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 零配置路径：只 <c>Scan</c>、不注册任何 <see cref="IPermissionCodeSource"/>，更新同样被拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Should_Throw_On_The_Scan_Only_Path()
	{
		using var scope = CreateScope([], out var provider);

		Assert.DoesNotContain(
			provider.GetRequiredService<IPermissionCodeSource>().AllOperations,
			operation => operation == BusinessOperation.Update);

		var order = NewOrder(provider);
		order.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// <c>CanPerformOperation</c> 只反映类型级要求：它没有方法上下文，方法级要求不在此叠加，
	/// 但仍会在工厂边界的闸门上生效。
	/// </summary>
	[Fact]
	public async Task CanPerformOperation_Should_Only_Reflect_Type_Level_Requirements()
	{
		using var scope = CreateScope([], out var provider);

		// 只有方法级声明的类型：查询不叠加方法级要求 ⇒ 返回 true（这是有意的查询口径）
		var methodLevel = NewOrder(provider);
		Assert.True(methodLevel.CanPerformOperation(BusinessOperation.Update));

		// 类型级声明仍生效：无授予时查询返回 false
		var typeLevel = new TypeLevelIsolatedOrder { BusinessContext = provider.GetRequiredService<BusinessContext>() };
		typeLevel.MarkAsChanged();
		Assert.False(typeLevel.CanPerformOperation(BusinessOperation.Update));

		// 闸门不受查询口径影响：同一实例的更新仍被拒绝
		await Assert.ThrowsAsync<SecurityException>(
			() => typeLevel.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	#region 装配

	private static IServiceScope CreateScope(string[] codes, out IServiceProvider provider)
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(IsolatedOrder).Assembly);

		// 零配置路径：只 Scan，不登记任何 IPermissionCodeSource（也不登记自定义要求来源）。
		// 要求全部来自「读声明」的内建路径（不需要登记任何来源）。
		services.AddPermission(p => p.Scan(typeof(IsolatedOrder).Assembly));

		services.AddSingleton<IScopeSubjectResolver>(new IsolationSubjectResolver(codes));
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();

		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider.Warm();

		return scope;
	}

	private static IsolatedOrder NewOrder(IServiceProvider provider)
	{
		return new IsolatedOrder { Name = "isolated", BusinessContext = provider.GetRequiredService<BusinessContext>() };
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[new Claim(UserClaimTypes.Subject, userId)],
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 更新入口与归档入口各带一个权限码的可编辑对象：用于验证方法级隔离。
/// </summary>
public class IsolatedOrder : EditableObject<IsolatedOrder>
{
	/// <summary>
	/// 获取或设置对象名称。
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 更新入口：只要求 <c>iso:update</c>。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	[Permission("iso:update")]
	[FactoryUpdate]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 另一个入口：要求 <c>iso:archive</c>，但它不是更新工厂方法，更新路径不应要求它。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	[Permission("iso:archive")]
	public async Task ArchiveAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 类型级要求 + 方法级要求的可编辑对象：用于验证 <c>CanPerformOperation</c> 的收窄语义。
/// </summary>
[Permission("iso:type")]
public class TypeLevelIsolatedOrder : EditableObject<TypeLevelIsolatedOrder>
{
	/// <summary>
	/// 更新入口：除类型级 <c>iso:type</c> 外还要求 <c>iso:update</c>。
	/// </summary>
	/// <param name="cancellationToken">取消令牌。</param>
	[Permission("iso:update")]
	[FactoryUpdate]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 授予固定权限码集合的解析器。
/// </summary>
internal sealed class IsolationSubjectResolver(string[] codes) : IScopeSubjectResolver
{
	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder().AddCodes(codes).Build());
	}
}