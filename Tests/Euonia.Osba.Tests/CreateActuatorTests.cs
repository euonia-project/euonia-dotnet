using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证 <see cref="CreateActuator{TTarget}"/> 支持任意 <see cref="ObjectBase{T}"/> 类型：
/// 可编辑对象（<see cref="EditableObject{T}"/>）创建后标记新增并保存（插入语义）；
/// 命令对象（<see cref="ServiceObject{T}"/>）、普通业务对象等不可持久化类型仅构造实例、不做落库。
/// </summary>
public class CreateActuatorTests
{
	/// <summary>
	/// 验证创建命令对象时只执行创建工厂方法与 Handle，不会运行命令体。
	/// </summary>
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

	/// <summary>
	/// 验证普通业务对象可被创建执行器构造，并向 Handle 暴露构造结果。
	/// </summary>
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

	/// <summary>
	/// 验证可编辑对象创建后被标记为新增并执行插入，且保留 Handle 中修改的属性值。
	/// </summary>
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
	/// <summary>
	/// 可编辑探针名称的属性元数据。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 指示创建工厂方法（<c>FactoryCreateAsync</c>，标有 <see cref="FactoryCreateAttribute"/>）是否已执行。
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

	/// <summary>
	/// 插入阶段将 <see cref="Inserted"/> 置位并直接完成，不执行真实持久化。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示插入过程的任务。</returns>
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		Inserted = true;
		return Task.CompletedTask;
	}
}

/// <summary>
/// 命令对象（不可持久化类型），用于验证创建执行器只构造、不执行命令体。
/// </summary>
public class CreateProbeCommand : ServiceObject<CreateProbeCommand>
{
	/// <summary>
	/// 指示创建工厂方法（<c>FactoryCreateAsync</c>，标有 <see cref="FactoryCreateAttribute"/>）是否已执行。
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
	public string Criteria { get; private set; } = null!;

	[FactoryCreate]
	private Task FactoryCreateAsync(string criteria, CancellationToken cancellationToken = default)
	{
		Criteria = criteria;
		Created = true;
		return Task.CompletedTask;
	}

	/// <summary>
	/// 命令体将 <see cref="Executed"/> 置位，用于验证创建路径不会触发执行。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示执行过程的任务。</returns>
	[FactoryExecute]
	protected internal override Task ExecuteAsync(CancellationToken cancellationToken = default)
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
	/// 指示创建工厂方法（<c>FactoryCreateAsync</c>，标有 <see cref="FactoryCreateAttribute"/>）是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	/// <summary>
	/// 指示 Handle 处理块是否已执行。
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// 创建工厂方法接收到的参数。
	/// </summary>
	public string Criteria { get; private set; } = null!;

	[FactoryCreate]
	private Task FactoryCreateAsync(string criteria, CancellationToken cancellationToken = default)
	{
		Criteria = criteria;
		Created = true;
		return Task.CompletedTask;
	}
}