using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Osba;

namespace Euonia.Osba.Benchmarks;

/// <summary>
/// 原始值、回滚与状态迁移的基线。
/// </summary>
[MemoryDiagnoser]
public class DomainControlBenchmarks
{
	private DomainBenchObject _target = null!;
	private bool _toggle;

	/// <summary>
	/// 创建基准对象并以初始值 <c>baseline</c> 接受变更，使原始值已就绪。
	/// </summary>
	[GlobalSetup]
	public void Setup()
	{
		_target = new DomainBenchObject { Name = "baseline" };
		_target.AcceptChanges();
	}

	/// <summary>
	/// 测量对属性赋值的开销，作为其余领域控制操作的对比基线。
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Write()
	{
		_target.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	/// <summary>
	/// 测量按键读取属性原始值的开销。
	/// </summary>
	[Benchmark]
	public object GetOriginalValue()
	{
		return _target.GetOriginalValue(DomainBenchObject.NameProperty);
	}

	/// <summary>
	/// 测量“修改属性后回滚到原始值”的整体开销。
	/// </summary>
	[Benchmark]
	public void RejectChanges()
	{
		_target.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
		_target.RejectChanges();
	}

	/// <summary>
	/// 测量标记为已修改后立即接受变更的状态迁移开销。
	/// </summary>
	[Benchmark]
	public void MarkAsChangedThenAccept()
	{
		_target.MarkAsChanged();
		_target.AcceptChanges();
	}
}

/// <summary>
/// 领域控制基准对象。
/// </summary>
public sealed class DomainBenchObject : EditableObject<DomainBenchObject>
{
	/// <summary>
	/// 注册的 <c>Name</c> 属性元数据，供读取原始值时按键查找。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

	/// <summary>
	/// 获取或设置名称，读写均经由领域对象的属性跟踪机制。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}
