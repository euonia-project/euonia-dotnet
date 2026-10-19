using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 回归测试：经过多轮 Send、Call、Publish 后，调用方的数据库上下文仍可正常读写，
/// 新作用域也能看到所有已提交的数据。
/// </summary>
/// <remarks>
/// 进程内单播/请求复用调用方作用域，因此命令与请求处理程序使用与调用方相同的
/// <see cref="TestDbContext"/>；多播发布使用消息专用作用域，使用独立上下文但写入同一数据库。
/// 任一路径错误地释放调用方作用域时，轮次结束后的查询或保存会抛出 <see cref="ObjectDisposedException"/>。
/// </remarks>
public class DatabaseLifetimeTests
{
	[Fact]
	public async Task MultipleSendCallPublishRounds_KeepDatabaseUsable()
	{
		var state = new RoundState();
		await using var provider = BuildProvider(state);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		// 先解析上下文以订阅渠道注册事件。
		provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(InsertCommandHandler), typeof(InsertRequestHandler), typeof(InsertEventHandler));

		var registrar = provider.GetRequiredService<IRecipientRegistrar>();
		await registrar.RegisterAsync(configurator.Registrations, "InMemory", TestContext.Current.CancellationToken);

		// 模拟一个长生命周期的调用方作用域：多轮消息处理结束后它仍必须可用。
		using var callerScope = provider.CreateScope();
		var database = callerScope.ServiceProvider.GetRequiredService<TestDbContext>();
		state.CallerContext = database;
		var bus = callerScope.ServiceProvider.GetRequiredService<IBus>();

		const int rounds = 3;
		var expected = 0;

		for (var index = 0; index < rounds; index++)
		{
			await bus.SendAsync(new InsertCommand { Value = $"send-{index}" }, TestContext.Current.CancellationToken);
			expected++;

			var count = await bus.CallAsync<InsertRequest, int>(new InsertRequest { Value = $"call-{index}" }, TestContext.Current.CancellationToken);
			expected++;
			Assert.Equal(expected, count);

			state.PublishRemaining = 1;
			state.PublishAllCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			await bus.PublishAsync(new InsertEvent { Value = $"publish-{index}" }, TestContext.Current.CancellationToken);
			await state.PublishAllCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
			Assert.Null(state.Error);
			expected++;

			// 每轮结束后，调用方的 DbContext 仍可查询（没有被消息处理释放）。
			Assert.Equal(expected, await database.CallLogs.CountAsync(TestContext.Current.CancellationToken));
		}

		Assert.True(state.CommandUsedCallerContext, "命令处理应复用调用方作用域中的 DbContext");
		Assert.True(state.RequestUsedCallerContext, "请求处理应复用调用方作用域中的 DbContext");
		Assert.True(state.EventUsedNewContext, "多播发布应为处理器创建独立作用域");

		// 多轮消息处理后，调用方仍能继续写入。
		database.CallLogs.Add(new CallLog { Value = "caller-after-rounds" });
		await database.SaveChangesAsync(TestContext.Current.CancellationToken);
		Assert.Equal(expected + 1, await database.CallLogs.CountAsync(TestContext.Current.CancellationToken));

		// 新作用域可以看到所有已提交的数据，证明数据库状态未被作用域隔离或释放破坏。
		using var verifyScope = provider.CreateScope();
		var verifyDatabase = verifyScope.ServiceProvider.GetRequiredService<TestDbContext>();
		Assert.Equal(expected + 1, await verifyDatabase.CallLogs.CountAsync(TestContext.Current.CancellationToken));
		Assert.Contains(await verifyDatabase.CallLogs.Select(item => item.Value).ToListAsync(TestContext.Current.CancellationToken), value => value == "publish-2");
	}

	[Fact]
	public async Task QueuedPublishRounds_KeepDatabaseUsable()
	{
		var state = new RoundState();
		await using var provider = BuildProvider(state);

		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(builder => builder.Add<DefaultMessageConvention>());

		provider.GetRequiredService<IHandlerContext>();
		configurator.RegisterChannel(typeof(InsertCommandHandler), typeof(InsertRequestHandler), typeof(InsertEventHandler));

		var registrar = provider.GetRequiredService<IRecipientRegistrar>();
		await registrar.RegisterAsync(configurator.Registrations, "InMemory", TestContext.Current.CancellationToken);

		using var callerScope = provider.CreateScope();
		var database = callerScope.ServiceProvider.GetRequiredService<TestDbContext>();
		state.CallerContext = database;
		var bus = callerScope.ServiceProvider.GetRequiredService<IBus>();

		const int count = 10;
		state.PublishRemaining = count;
		state.PublishAllCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// 连续入队多条事件，让它们在后台泵中依次使用各自的执行作用域处理。
		for (var index = 0; index < count; index++)
		{
			await bus.PublishAsync(new InsertEvent { Value = $"burst-{index}" }, TestContext.Current.CancellationToken);
		}

		await state.PublishAllCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		Assert.Null(state.Error);
		Assert.Equal(count, await database.CallLogs.CountAsync(TestContext.Current.CancellationToken));

		// 发布批次结束后，调用方仍能发送命令、发起请求并继续写数据库。
		await bus.SendAsync(new InsertCommand { Value = "after-burst-send" }, TestContext.Current.CancellationToken);
		var requestCount = await bus.CallAsync<InsertRequest, int>(new InsertRequest { Value = "after-burst-call" }, TestContext.Current.CancellationToken);
		Assert.Equal(count + 2, requestCount);

		database.CallLogs.Add(new CallLog { Value = "caller-after-burst" });
		await database.SaveChangesAsync(TestContext.Current.CancellationToken);
		Assert.Equal(count + 3, await database.CallLogs.CountAsync(TestContext.Current.CancellationToken));
	}

	private static ServiceProvider BuildProvider(RoundState state)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		// 显式共享数据库名与 InMemoryDatabaseRoot，使不同作用域的 DbContext 访问同一个数据库。
		var databaseName = $"database-lifetime-{Guid.NewGuid():N}";
		var databaseRoot = new InMemoryDatabaseRoot();
		services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "InMemory");
		services.AddEuoniaBus();
		services.AddInMemoryBus("InMemory");
		services.AddSingleton(state);

		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
	}

	private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
	{
		public DbSet<CallLog> CallLogs => Set<CallLog>();
	}

	private sealed class CallLog
	{
		public int Id { get; set; }

		public string Value { get; set; }
	}

	private sealed class RoundState
	{
		public TestDbContext CallerContext { get; set; }

		public bool CommandUsedCallerContext { get; set; } = true;

		public bool RequestUsedCallerContext { get; set; } = true;

		public bool EventUsedNewContext { get; set; }

		public Exception Error { get; set; }

		public int PublishRemaining;

		public TaskCompletionSource PublishAllCompleted { get; set; }
	}

	private sealed class InsertCommand : IUnicast
	{
		public string Value { get; set; }
	}

	private sealed class InsertRequest : IRequest<int>
	{
		public string Value { get; set; }
	}

	private sealed class InsertEvent : IMulticast
	{
		public string Value { get; set; }
	}

	private sealed class InsertCommandHandler(TestDbContext database, RoundState state) : IHandler<InsertCommand>
	{
		public async Task HandleAsync(InsertCommand message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			state.CommandUsedCallerContext &= ReferenceEquals(database, state.CallerContext);
			database.CallLogs.Add(new CallLog { Value = message.Value });
			await database.SaveChangesAsync(cancellationToken);
		}
	}

	private sealed class InsertRequestHandler(TestDbContext database, RoundState state) : IHandler<InsertRequest, int>
	{
		public async Task<int> HandleAsync(InsertRequest message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			state.RequestUsedCallerContext &= ReferenceEquals(database, state.CallerContext);
			database.CallLogs.Add(new CallLog { Value = message.Value });
			await database.SaveChangesAsync(cancellationToken);
			return await database.CallLogs.CountAsync(cancellationToken);
		}
	}

	private sealed class InsertEventHandler(TestDbContext database, RoundState state) : IHandler<InsertEvent>
	{
		public async Task HandleAsync(InsertEvent message, IMessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				state.EventUsedNewContext = !ReferenceEquals(database, state.CallerContext);
				database.CallLogs.Add(new CallLog { Value = message.Value });
				await database.SaveChangesAsync(cancellationToken);
			}
			catch (Exception exception)
			{
				state.Error = exception;
			}
			finally
			{
				if (Interlocked.Decrement(ref state.PublishRemaining) == 0)
				{
					state.PublishAllCompleted.TrySetResult();
				}
			}
		}
	}
}
