using System.Collections;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 「共享快照被就地改写」的回写护栏：
/// <list type="bullet">
/// <item>权限码来源算出的结果不得通过强转成可写集合来改动（改的是缓存，污染的是后续所有调用方）；</item>
/// <item>共享的空 <see cref="ScopeSubject"/> 不得被就地 <c>Add</c>（那等于给所有无授予的主体注入维度值）。</item>
/// </list>
/// </summary>
public class SnapshotWritebackRegressionTests
{
	[Fact]
	public void OperationCodeSource_RequirementsFor_Should_Not_Be_Writable_Through_The_Returned_Reference()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.Build();

		var requirements = source.RequirementsFor(typeof(ApprovableAsset), BusinessOperation.Execute);

		Assert.NotEmpty(requirements);

		// 数组与 List 的 IList 索引器可以就地写；只读包装会抛 NotSupportedException。
		// 返回数组的话，调用方改的就是本来源按（类型，操作）缓存下来的那一份。
		var asList = requirements as IList;
		Assert.NotNull(asList);

		Assert.Throws<NotSupportedException>(() => asList[0] = requirements[0]);
	}

	[Fact]
	public void ScopeSubject_Empty_Should_Reject_Mutations()
	{
		Assert.Throws<InvalidOperationException>(() => ScopeSubject.Empty.Add("owner", "u1"));

		Assert.True(ScopeSubject.Empty.IsEmpty);
	}
}
