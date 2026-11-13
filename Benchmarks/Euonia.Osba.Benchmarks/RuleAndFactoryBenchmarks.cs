using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Euonia.Osba.Benchmarks;

/// <summary>
/// 规则执行、校验与工厂保存路径的基线。
/// </summary>
[MemoryDiagnoser]
public class RuleAndFactoryBenchmarks
{
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private BusinessContext _context = null!;
	private RuledObject _ruled = null!;
	private PlainObject _plain = null!;
	private IActuator _actuator = null!;
	private IObjectFactory _factory = null!;
	private bool _toggle;

	/// <summary>
	/// 注册业务对象服务并创建作用域，同时准备一个带规则对象与一个无规则对象供基准复用。
	/// </summary>
	[GlobalSetup]
	public void Setup()
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(RuleAndFactoryBenchmarks).Assembly);
		_provider = services.BuildServiceProvider();
		_scope = _provider.CreateScope();
		BusinessContextAccessor.SetCurrent(_scope.ServiceProvider);
		_context = _scope.ServiceProvider.GetRequiredService<BusinessContext>();
		_actuator = _scope.ServiceProvider.GetRequiredService<IActuator>();
		_factory = _scope.ServiceProvider.GetRequiredService<IObjectFactory>();

		_ruled = new RuledObject { BusinessContext = _context };
		_ruled.Name = "valid";
		_ruled.AcceptChanges();

		_plain = new PlainObject { BusinessContext = _context };
		_plain.Name = "valid";
		_plain.AcceptChanges();
	}

	/// <summary>
	/// 清理当前业务上下文并释放作用域与服务容器。
	/// </summary>
	[GlobalCleanup]
	public void Cleanup()
	{
		BusinessContextAccessor.Clear();
		_scope?.Dispose();
		_provider?.Dispose();
	}

	/// <summary>
	/// 测量无规则对象赋值属性的开销，作为规则相关基线的对照。
	/// </summary>
	[Benchmark(Baseline = true)]
	public void WritePlain()
	{
		_plain.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	/// <summary>
	/// 测量对带必填规则的对象赋值属性时规则执行所增加的开销。
	/// </summary>
	[Benchmark]
	public void WriteWithPropertyRule()
	{
		_ruled.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	/// <summary>
	/// 测量对无规则对象执行完整校验的开销。
	/// </summary>
	/// <returns>校验是否通过。</returns>
	[Benchmark]
	public async Task<bool> ValidatePlain()
	{
		return await _plain.ValidateAsync();
	}

	/// <summary>
	/// 测量标记变更后经工厂保存对象的开销，包含更新钩子的调度。
	/// </summary>
	/// <returns>表示异步保存操作的任务。</returns>
	[Benchmark]
	public async Task SavePlain()
	{
		_plain.MarkAsChanged();
		await _plain.SaveAsync();
	}

	/// <summary>
	/// 测量经由 <see cref="IObjectFactory"/> 拉取对象的开销。
	/// </summary>
	/// <returns>拉取到的对象实例。</returns>
	[Benchmark]
	public async Task<PlainObject> FetchFactory()
	{
		return await _factory.FetchAsync<PlainObject>("id");
	}

	/// <summary>
	/// 测量经由 <see cref="IActuator"/> 构建并执行拉取表达式的开销。
	/// </summary>
	/// <returns>拉取到的对象实例。</returns>
	[Benchmark]
	public async Task<PlainObject> FetchActuator()
	{
		return await _actuator.For<PlainObject>().Fetch("id").ExecuteAsync();
	}
}

/// <summary>
/// 无规则对象。
/// </summary>
public sealed class PlainObject : EditableObject<PlainObject>
{
	/// <summary>
	/// 注册的 <c>Name</c> 属性元数据，供属性读写与规则定义时按键查找。
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

	[FactoryFetch]
	private void Fetch(string id)
	{
		LoadProperty(NameProperty, "valid");
		AcceptChanges();
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

/// <summary>
/// 带一条属性级规则的对象。
/// </summary>
public sealed class RuledObject : EditableObject<RuledObject>
{
	/// <summary>
	/// 注册的 <c>Name</c> 属性元数据，供属性读写与规则定义时按键查找。
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
	/// 为 <c>Name</c> 属性注册必填规则，使写入空值时校验失败。
	/// </summary>
	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "required"));
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
