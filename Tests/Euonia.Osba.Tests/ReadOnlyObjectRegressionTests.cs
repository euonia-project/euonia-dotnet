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

/// <summary>
/// <c>IsBypassingRuleChecks</c> 改为<b>异步流作用域</b>（AsyncLocal）后的语义护栏：
/// <list type="bullet">
/// <item>绕过标志只影响发起它的逻辑流及其派生流——并发流上的同实例 setter 不受外溢；</item>
/// <item><c>using</c> 退出还原「进入时捕获的旧值」——嵌套绕过正确叠加/还原；</item>
/// <item>lambda 规则（<c>RulesExtensions.AddRule</c>）在绕过区间内修改属性后，
/// 变更<b>必须</b>仍然进入 <c>ChangedProperties</c>（旧对象级 bool 会把它整体跳过 → 静默不持久化）。</item>
/// </list>
/// </summary>
public class BypassRuleChecksFlowScopeTests
{
	[Fact]
	public async Task Bypass_Should_Not_Leak_To_Concurrent_Flow()
	{
		var target = new BypassProbe();
		var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var observedDuringBypass = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// 流 A：进入绕过区间后挂起，等流 B 在同一实例上写属性
		var flowA = Task.Run(async () =>
		{
			using (target.BypassRuleChecks)
			{
				barrier.SetResult();
				await observedDuringBypass.Task;
			}
		});

		await barrier.Task;

		// 流 B：与流 A 并发，同实例、同一时刻。对象级 bool 时代这里会被外溢的标志跳过变更追踪。
		var flowB = Task.Run(async () =>
		{
			target.Name = "concurrent";
			observedDuringBypass.SetResult(target.ChangedProperties.Count > 0);
		});

		await Task.WhenAll(flowA, flowB);

		Assert.True(observedDuringBypass.Task.Result, "并发流的属性变更被流 A 的绕过标志外溢跳过（AsyncLocal 语义被破坏）。");
		Assert.Contains(target.ChangedProperties, p => p.Name == "Name");
	}

	[Fact]
	public void Nested_Bypass_Should_Restore_Captured_Value_Not_False()
	{
		var target = new BypassProbe();

		using (target.BypassRuleChecks)
		{
			// 嵌套层退出时若还原成恒 false，会关闭外层的绕过状态
			using (target.BypassRuleChecks)
			{
				Assert.True(target.IsBypassingForProbe);
			}

			Assert.True(target.IsBypassingForProbe, "嵌套 using 退出后外层绕过状态被误关闭。");
		}

		Assert.False(target.IsBypassingForProbe);
	}

	[Fact]
	public void Bypass_Should_Not_Cross_Await_Boundary_Into_Sibling_Flow()
	{
		// AsyncLocal 值只流向派生流：进入绕过后启动的新 Task 继承它（保持原语义），
		// 但与本流平行的、绕过之前已启动的流不受影响。
		var target = new BypassProbe();
		var inherited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		using (target.BypassRuleChecks)
		{
			var derived = Task.Run(() => inherited.SetResult(target.IsBypassingForProbe));
			derived.Wait();
		}

		Assert.True(inherited.Task.Result, "派生流应继承绕过状态（与原对象级语义兼容）。");
		Assert.False(target.IsBypassingForProbe, "using 退出后本流必须还原。");
	}

	/// <summary>
	/// 绕过语义探针：暴露当前流的绕过状态与变更追踪。
	/// 继承 <see cref="ObservableObject{T}"/>（可写）——并发写用例需要能真实落进
	/// <see cref="BusinessObject.ChangedProperties"/>；只读探针（<see cref="ReadOnlyProbe"/>）
	/// 的写入会被 <c>CanWriteProperty</c> 拒绝，握手完成源永不置位会让用例挂起。
	/// </summary>
	private sealed class BypassProbe : ObservableObject<BypassProbe>
	{
		public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

		private string _name;

		public bool IsBypassingForProbe => GetType()
			.GetProperty("IsBypassingRuleChecks", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(this) is true;

		public string Name
		{
			get => GetProperty(NameProperty);
			set => SetProperty(NameProperty, ref _name, value);
		}
	}
}
