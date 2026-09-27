using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Core.Tests;

public class UserGeneralBusinessTests
{
	private IObjectFactory _factory;

	public UserGeneralBusinessTests(IObjectFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task LazyServiceProviderShouldWork()
	{
		var business = await _factory.CreateAsync<UserGeneralBusiness>(CancellationToken.None);
		Assert.NotNull(business.LazyServiceProvider);
	}

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
