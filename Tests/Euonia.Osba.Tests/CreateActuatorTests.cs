using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 <see cref="CreateActuator{TTarget}"/> 支持任意 <see cref="BusinessObject{T}"/> 类型：
/// 可编辑对象（<see cref="EditableObject{T}"/>）创建后标记新增并保存（插入语义）；
/// 命令对象（<see cref="CommandObject{T}"/>）、普通业务对象等不可持久化类型仅构造实例、不做落库。
/// </summary>
public class CreateActuatorTests
{
	[Fact]
	public async Task Create_ShouldConstructCommandWithoutRunningCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<CreateProbeCommand>()
		                           .Create("create")
		                           .Handle(command => command.Handled = true)
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Created);
		Assert.True(result.Handled);
		Assert.Equal("create", result.Criteria);
		Assert.False(result.Executed);
	}

	[Fact]
	public async Task Create_ShouldConstructPlainBusinessObject()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<CreateProbeBusiness>()
		                           .Create("seed")
		                           .Handle(item => item.Handled = true)
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Created);
		Assert.True(result.Handled);
		Assert.Equal("seed", result.Criteria);
	}

	[Fact]
	public async Task Create_ShouldMarkNewAndInsertForEditableObject()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<CreateProbeEditable>()
		                           .Create("repo")
		                           .Handle(item => item.Name = "new-repo")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Created);
		Assert.True(result.Inserted);
		Assert.Equal("new-repo", result.Name);
	}
}

/// <summary>
/// 可持久化的可编辑对象，用于验证创建执行器的「标记新增 + 插入」语义。
/// </summary>
public class CreateProbeEditable : EditableObject<CreateProbeEditable>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 指示创建工厂方法（<see cref="CreateAsync(CancellationToken)"/>）是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	/// <summary>
	/// 指示插入工厂方法（<see cref="InsertAsync(CancellationToken)"/>）是否已执行。
	/// </summary>
	public bool Inserted { get; private set; }

	/// <summary>
	/// 供测试观察的字段值。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	[FactoryCreate]
	private Task FactoryCreateAsync(string criteria, CancellationToken cancellationToken = default)
	{
		Created = true;
		return Task.CompletedTask;
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		Inserted = true;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 命令对象（不可持久化类型），用于验证创建执行器只构造、不执行命令体。
/// </summary>
public class CreateProbeCommand : CommandObject<CreateProbeCommand>
{
	/// <summary>
	/// 指示创建工厂方法（<see cref="CreateAsync(CancellationToken)"/>）是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	/// <summary>
	/// 指示命令体（<see cref="ExecuteAsync(CancellationToken)"/>）是否已执行。
	/// </summary>
	public bool Executed { get; private set; }

	/// <summary>
	/// 指示 Handle 处理块是否已执行。
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// 创建工厂方法接收到的参数。
	/// </summary>
	public string Criteria { get; private set; }

	[FactoryCreate]
	private Task FactoryCreateAsync(string criteria, CancellationToken cancellationToken = default)
	{
		Criteria = criteria;
		Created = true;
		return Task.CompletedTask;
	}

	[FactoryExecute]
	protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = true;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 普通业务对象（不可持久化类型），用于验证创建执行器只构造实例。
/// </summary>
public class CreateProbeBusiness : BusinessObject<CreateProbeBusiness>
{
	/// <summary>
	/// 指示创建工厂方法是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	/// <summary>
	/// 指示 Handle 处理块是否已执行。
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// 创建工厂方法接收到的参数。
	/// </summary>
	public string Criteria { get; private set; }

	[FactoryCreate]
	private Task FactoryCreateAsync(string criteria, CancellationToken cancellationToken = default)
	{
		Criteria = criteria;
		Created = true;
		return Task.CompletedTask;
	}
}