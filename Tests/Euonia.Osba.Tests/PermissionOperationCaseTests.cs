using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 兜底权限要求来源的操作名匹配口径：<b>忽略大小写</b>。
/// </summary>
/// <remarks>
/// 规则表按 <see cref="BusinessOperation"/> 的小写常量登记，而调用方可能传入任意大小写的操作名。
/// 按序数比较时 <c>AttributeTypesOf</c> 返回空 → <c>RequirementsFor</c> 返回空 →
/// <c>ObjectAuthorization</c> 视为「没有任何权限要求」而<b>静默放行</b>。
/// </remarks>
public class PermissionOperationCaseTests
{
	[Fact]
	public void Requirements_Lookup_Should_Ignore_Operation_Casing()
	{
		var provider = ObjectPermissionRequirementProvider.Instance;

		var expected = provider.RequirementsFor(typeof(MixedCaseOperationSubject), BusinessOperation.Update);
		var actual = provider.RequirementsFor(typeof(MixedCaseOperationSubject), "UPDATE");

		Assert.NotEmpty(expected);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void Codes_Lookup_Should_Ignore_Operation_Casing()
	{
		var provider = ObjectPermissionRequirementProvider.Instance;

		var expected = provider.CodesFor(typeof(MixedCaseOperationSubject), BusinessOperation.Update);
		var actual = provider.CodesFor(typeof(MixedCaseOperationSubject), "UpDaTe");

		Assert.Equal(["mixed:update"], expected);
		Assert.Equal(expected, actual);
	}

	private sealed class MixedCaseOperationSubject
	{
		[Permission("mixed:update")]
		[FactoryUpdate]
		public void Update() { }
	}
}
