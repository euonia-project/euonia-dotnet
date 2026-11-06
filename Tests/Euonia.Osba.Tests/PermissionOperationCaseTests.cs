using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 兜底权限码来源的操作名匹配口径：<b>忽略大小写</b>。
/// </summary>
/// <remarks>
/// 规则表按 <see cref="BusinessOperation"/> 的小写常量登记，而调用方可能传入任意大小写的操作名。
/// 按序数比较时 <c>AttributeTypesOf</c> 返回空 → <c>RequirementsFor</c> 返回空 →
/// <c>ObjectAuthorization</c> 视为「没有任何权限要求」而<b>静默放行</b>。
/// </remarks>
public class PermissionOperationCaseTests
{
	/// <summary>
	/// 验证权限要求查询忽略操作名大小写：任意大小写形式返回与规范操作名一致的要求集合。
	/// </summary>
	[Fact]
	public void Requirements_Lookup_Should_Ignore_Operation_Casing()
	{
		var source = ObjectPermissionCodeSource.Instance;

		var expected = source.RequirementsFor(typeof(MixedCaseOperationSubject), BusinessOperation.Update);
		var actual = source.RequirementsFor(typeof(MixedCaseOperationSubject), "UPDATE");

		Assert.NotEmpty(expected);
		Assert.Equal(expected, actual);
	}

	/// <summary>
	/// 验证权限码查询忽略操作名大小写：混合大小写形式返回与规范操作名一致的权限码集合。
	/// </summary>
	[Fact]
	public void Codes_Lookup_Should_Ignore_Operation_Casing()
	{
		var source = ObjectPermissionCodeSource.Instance;

		var expected = source.CodesFor(typeof(MixedCaseOperationSubject), BusinessOperation.Update);
		var actual = source.CodesFor(typeof(MixedCaseOperationSubject), "UpDaTe");

		Assert.Equal(["mixed:update"], expected);
		Assert.Equal(expected, actual);
	}

	/// <summary>
	/// 带 <see cref="PermissionAttribute"/> 与 <see cref="FactoryUpdateAttribute"/> 的测试对象，
	/// 用于验证操作名比较忽略大小写。
	/// </summary>
	private sealed class MixedCaseOperationSubject
	{
		[Permission("mixed:update")]
		[FactoryUpdate]
		public void Update() { }
	}
}
