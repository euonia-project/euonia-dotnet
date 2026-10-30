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
	private ServiceProvider _provider;
	private IServiceScope _scope;
	private BusinessContext _context;
	private RuledObject _ruled;
	private PlainObject _plain;
	private IActuator _actuator;
	private IObjectFactory _factory;
	private bool _toggle;

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

	[GlobalCleanup]
	public void Cleanup()
	{
		BusinessContextAccessor.Clear();
		_scope?.Dispose();
		_provider?.Dispose();
	}

	[Benchmark(Baseline = true)]
	public void WritePlain()
	{
		_plain.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	[Benchmark]
	public void WriteWithPropertyRule()
	{
		_ruled.Name = _toggle ? "a" : "b";
		_toggle = !_toggle;
	}

	[Benchmark]
	public async Task<bool> ValidatePlain()
	{
		return await _plain.ValidateAsync();
	}

	[Benchmark]
	public async Task SavePlain()
	{
		_plain.MarkAsChanged();
		await _plain.SaveAsync();
	}

	[Benchmark]
	public async Task<PlainObject> FetchFactory()
	{
		return await _factory.FetchAsync<PlainObject>("id");
	}

	[Benchmark]
	public async Task<PlainObject> FetchActuator()
	{
		return await _actuator.For<PlainObject>().Fetch("id").ExecuteAsync();
	}
}

/// <summary>无规则对象。</summary>
public sealed class PlainObject : EditableObject<PlainObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(x => x.Name);

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

/// <summary>带一条属性级规则的对象。</summary>
public sealed class RuledObject : EditableObject<RuledObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(x => x.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule(new CommonRule.Required(NameProperty, "required"));
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
