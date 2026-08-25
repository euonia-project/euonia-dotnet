using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证命令对象（<see cref="CommandObject{T}"/>）通过执行器（<see cref="IActuator"/>）执行的完整流程：
/// <c>For&lt;TCommand&gt;().Execute(criteria).Handle(...).ExecuteAsync()</c>。
/// </summary>
public class CommandObjectActuatorTests
{
	private readonly IActuator _actuator;

	public CommandObjectActuatorTests(IActuator actuator)
	{
		_actuator = actuator;
	}

	[Fact]
	public async Task ExecuteShouldCreateHandleThenRunCommandBody()
	{
		var result = await _actuator.For<ActuatorTestCommand>()
		                            .Execute(CancellationToken.None)
		                            .Handle(command => command.Step = 2)
		                            .ExecuteAsync(CancellationToken.None);

		Assert.NotNull(result);
		Assert.True(result.Created);
		Assert.True(result.Executed);
	}
}

/// <summary>
/// 用于验证执行器命令流程的测试命令对象。
/// </summary>
public class ActuatorTestCommand : CommandObject<ActuatorTestCommand>
{
	/// <summary>
	/// 指示创建步骤（<see cref="CreateAsync(CancellationToken)"/>）是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	/// <summary>
	/// 执行步骤计数：1 = 创建，2 = Handle，3 = 命令体。
	/// </summary>
	public int Step { get; set; }

	/// <summary>
	/// 指示命令体是否已执行；仅在 Handle（Step = 2）之后执行时置位，用于验证执行顺序。
	/// </summary>
	public bool Executed { get; private set; }

	[FactoryCreate]
	protected override Task CreateAsync(CancellationToken cancellationToken = default)
	{
		Created = true;
		Step = 1;
		return base.CreateAsync(cancellationToken);
	}

	[FactoryExecute]
	protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = Step == 2;
		return base.ExecuteAsync(cancellationToken);
	}
}
