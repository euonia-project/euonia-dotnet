using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证 Osba 工厂创建的业务对象：懒加载服务提供程序可用，且对象级校验规则按编辑状态生效。
/// </summary>
public class UserGeneralBusinessTests
{
	private IObjectFactory _factory;

	/// <summary>
	/// 注入对象工厂，供各用例创建业务对象。
	/// </summary>
	/// <param name="factory">Osba 对象工厂。</param>
	public UserGeneralBusinessTests(IObjectFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	/// 工厂创建的对象带有可用的 <c>LazyServiceProvider</c>，依赖可在首次使用时惰性解析。
	/// </summary>
	[Fact]
	public async Task LazyServiceProviderShouldWork()
	{
		var business = await _factory.CreateAsync<UserGeneralBusiness>(CancellationToken.None);
		Assert.NotNull(business.LazyServiceProvider);
	}

	/// <summary>
	/// 新建态使用被禁用的用户名保存时抛出 <see cref="ValidationException"/>，错误中包含 <c>UsernameNotAllowed</c>。
	/// </summary>
	[Fact]
	public async Task CreateWithForbiddenName_ShouldFailValidation()
	{
		var business = await _factory.CreateAsync<UserGeneralBusiness>(CancellationToken.None);
		business.Name = "admin";
		business.MarkAsNew();

		var ex = await Assert.ThrowsAsync<ValidationException>(
			() => business.SaveAsync(false, CancellationToken.None));

		// 验证线只报数据校验问题。越权不走这条线——它一律由工厂边界抛 SecurityException
		Assert.Contains(ex.Errors, error => error.ErrorMessage.Contains("UsernameNotAllowed"));
	}

	/// <summary>
	/// 带 <c>ExecuteOnState(ObjectEditState.New)</c> 的用户名规则在更新态不执行，
	/// 因此被禁用的用户名可以保存成功并回到 <see cref="ObjectEditState.None"/>。
	/// </summary>
	[Fact]
	public async Task UpdateWithForbiddenName_ShouldSucceed_BecauseNewOnlyRuleDoesNotApply()
	{
		// UsernameCheckRule 带 [ExecuteOnState(ObjectEditState.New)]：更新态不跑它。
		// 顺带钉住「删除/更新默认跳过对象级规则」与状态过滤这两个纯验证线机制——
		// 权限不再由规则表达，因此这些机制不再承担越权拦截的职责。
		var business = await _factory.CreateAsync<UserGeneralBusiness>(CancellationToken.None);
		business.Name = "admin";
		business.MarkAsChanged();

		var result = await business.SaveAsync(true, CancellationToken.None);

		Assert.Equal(ObjectEditState.None, result.State);
	}
}
