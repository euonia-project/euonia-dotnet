using System.Collections.Concurrent;
using System.Reflection;
using System.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 只读对象与绕过管理器的回归护栏：
/// <list type="bullet">
/// <item>只读对象必须拒绝写入（此前 <c>IsBypassingRuleChecks</c> 恒为真把写入权限检查整体短路，只读对象静默可写）；</item>
/// <item>只读对象的读取权限不得被同一个开关短路；</item>
/// <item><c>BypassRuleChecks</c> 并发进出后必须完全还原（管理器不残留、计数归零）。</item>
/// </list>
/// </summary>
public class ReadOnlyObjectRegressionTests
{
	[Fact]
	public void ReadOnly_Object_Should_Refuse_Writes()
	{
		var target = new ReadOnlyProbe();

		Assert.Throws<SecurityException>(() =>
		{
			target.Name = "changed";
		});

		Assert.NotEqual("changed", target.Name);
	}

	[Fact]
	public void ReadOnly_Object_Should_Still_Honor_Read_Permissions()
	{
		var target = new ReadOnlyProbe { AllowRead = false };

		Assert.Throws<SecurityException>(() =>
		{
			_ = target.Name;
		});
	}

	[Fact]
	public async Task Concurrent_Bypass_Blocks_Should_Restore_The_Target_Completely()
	{
		var target = new ReadOnlyProbe();
		var managerProperty = typeof(BusinessObject)
			.GetProperty("InternalBypassRuleChecks", BindingFlags.Instance | BindingFlags.NonPublic);

		var failures = new ConcurrentBag<Exception>();

		var threads = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
		{
			try
			{
				for (var i = 0; i < 2_000; i++)
				{
					using (target.BypassRuleChecks)
					{
						Thread.Yield();
					}
				}
			}
			catch (Exception ex)
			{
				failures.Add(ex);
			}
		}, TestContext.Current.CancellationToken)).ToArray();

		await Task.WhenAll(threads);

		Assert.Empty(failures);
		Assert.Null(managerProperty.GetValue(target));
	}
}

/// <summary>
/// 用于只读与绕过护栏的探针对象。
/// </summary>
public class ReadOnlyProbe : ReadOnlyObject<ReadOnlyProbe>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	private string _name;

	/// <summary>
	/// 控制 <see cref="CanReadProperty(IPropertyInfo)"/> 的返回值。
	/// </summary>
	public bool AllowRead { get; set; } = true;

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, ref _name, value);
	}

	/// <inheritdoc />
	public override bool CanReadProperty(IPropertyInfo property)
	{
		return AllowRead;
	}
}
