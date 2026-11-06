using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证命令对象（<see cref="ServiceObject{T}"/>）通过执行器（<see cref="IActuator"/>）执行的完整流程：
/// <c>For&lt;TCommand&gt;().Execute(criteria).Handle(...).ExecuteAsync()</c>。
/// </summary>
public class ServiceObjectActuatorTests
{
	private readonly IActuator _actuator;

	/// <summary>
	/// 初始化测试类并保存待验证的执行器。
	/// </summary>
	/// <param name="actuator">由测试宿主注入的执行器实例。</param>
	public ServiceObjectActuatorTests(IActuator actuator)
	{
		_actuator = actuator;
	}

	/// <summary>
	/// 验证执行器按「创建 → Handle → 命令体」的顺序执行，且命令体在 Handle 之后才被置为已执行。
	/// </summary>
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
public class ActuatorTestCommand : ServiceObject<ActuatorTestCommand>
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

	/// <summary>
	/// 创建阶段将 <see cref="Created"/> 置位、把 <see cref="Step"/> 置为 1，再调用基类创建逻辑。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示创建过程的任务。</returns>
	[FactoryCreate]
	protected internal override Task CreateAsync(CancellationToken cancellationToken = default)
	{
		Created = true;
		Step = 1;
		return base.CreateAsync(cancellationToken);
	}

	/// <summary>
	/// 命令体仅在 <see cref="Step"/> 已被 Handle 置为 2 时将 <see cref="Executed"/> 置位，用于验证执行顺序。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示执行过程的任务。</returns>
	[FactoryExecute]
	protected internal override Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = Step == 2;
		return base.ExecuteAsync(cancellationToken);
	}
}
