using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Osba;

namespace Euonia.Osba.Benchmarks;

/// <summary>
/// 原始值、回滚与状态迁移的基线。
/// </summary>
[MemoryDiagnoser]
public class DomainControlBenchmarks
{
	private DomainBenchObject _target;
	private bool _toggle;

	[GlobalSetup]
	public void Setup()
	{
		_target = new DomainBenchObject { Name = "baseline" };
		_target.AcceptChanges();
	}

	[Benchmark(Baseline = true)]
	public void Write()
	{
		_target.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	[Benchmark]
	public object GetOriginalValue()
	{
		return _target.GetOriginalValue(DomainBenchObject.NameProperty);
	}

	[Benchmark]
	public void RejectChanges()
	{
		_target.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
		_target.RejectChanges();
	}

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
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}
