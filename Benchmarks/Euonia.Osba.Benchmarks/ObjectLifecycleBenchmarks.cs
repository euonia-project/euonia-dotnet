using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Osba;

namespace Euonia.Osba.Benchmarks;

/// <summary>
/// 对象创建、首次字段访问与稳态读写的基线。
/// </summary>
[MemoryDiagnoser]
public class ObjectLifecycleBenchmarks
{
	private SmallObject _existing;
	private bool _toggle;

	[GlobalSetup]
	public void Setup()
	{
		_existing = new SmallObject { Name = "existing" };
		_existing.AcceptChanges();
	}

	[Benchmark(Baseline = true)]
	public SmallObject Create()
	{
		return new SmallObject();
	}

	[Benchmark]
	public string CreateAndFirstRead()
	{
		return new SmallObject().Name;
	}

	[Benchmark]
	public string ReadExisting()
	{
		return _existing.Name;
	}

	[Benchmark]
	public void WriteExisting()
	{
		_existing.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	[Benchmark]
	public void WriteSameValue()
	{
		_existing.Name = "existing";
	}
}

/// <summary>
/// 一个属性、无规则的最小业务对象。
/// </summary>
public sealed class SmallObject : EditableObject<SmallObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(x => x.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}
