using BenchmarkDotNet.Attributes;
using Nerosoft.Euonia.Osba;

namespace Euonia.Osba.Benchmarks;

/// <summary>
/// 对象创建、首次字段访问与稳态读写的基线。
/// </summary>
[MemoryDiagnoser]
public class ObjectLifecycleBenchmarks
{
	private SmallObject _existing = null!;
	private bool _toggle;

	/// <summary>
	/// 创建基准对象并以初始值 <c>existing</c> 接受变更，作为稳态读写的目标。
	/// </summary>
	[GlobalSetup]
	public void Setup()
	{
		_existing = new SmallObject { Name = "existing" };
		_existing.AcceptChanges();
	}

	/// <summary>
	/// 测量仅创建对象实例的开销，作为生命周期基线。
	/// </summary>
	[Benchmark(Baseline = true)]
	public SmallObject Create()
	{
		return new SmallObject();
	}

	/// <summary>
	/// 测量创建对象后立即读取首个属性值的开销，包含惰性初始化的成本。
	/// </summary>
	[Benchmark]
	public string CreateAndFirstRead()
	{
		return new SmallObject().Name;
	}

	/// <summary>
	/// 测量读取已初始化对象属性值的稳态开销。
	/// </summary>
	[Benchmark]
	public string ReadExisting()
	{
		return _existing.Name;
	}

	/// <summary>
	/// 测量对已有对象写入不同值的开销。
	/// </summary>
	[Benchmark]
	public void WriteExisting()
	{
		_existing.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	/// <summary>
	/// 测量写入与当前值相同的值时的开销，用于验证相等性短路是否生效。
	/// </summary>
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
	/// <summary>
	/// 注册的 <c>Name</c> 属性元数据，供属性读写时按键查找。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(x => x.Name);

	/// <summary>
	/// 获取或设置名称，读写均经由领域对象的属性跟踪机制。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 工厂插入钩子的空实现，使对象可在不访问存储的情况下走完创建流程。
	/// </summary>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>已完成的空任务。</returns>
	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	/// <summary>
	/// 工厂更新钩子的空实现，使对象可在不访问存储的情况下走完更新流程。
	/// </summary>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>已完成的空任务。</returns>
	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}
