using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 可编程设定的请求上下文访问器桩，供行为/拦截器测试注入 <see cref="RequestContext"/>。
/// </summary>
internal sealed class StubRequestContextAccessor : IRequestContextAccessor
{
	/// <summary>
	/// 获取或设置当前请求上下文；设为 <see langword="null"/> 可模拟无上下文的调用场景。
	/// </summary>
	public RequestContext? Context { get; set; }
}

/// <summary>
/// 捕获日志条目的内存日志提供程序，便于断言拦截器输出的日志内容。
/// </summary>
internal sealed class InMemoryLoggerProvider : ILoggerProvider
{
	private readonly List<(LogLevel Level, string Message)> _entries = [];

	/// <summary>
	/// 获取已捕获的日志条目，包含级别与格式化后的消息，供断言使用。
	/// </summary>
	public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

	/// <summary>
	/// 创建写入本提供程序条目列表的日志记录器。
	/// </summary>
	/// <param name="categoryName">日志类别名；捕获时忽略该值。</param>
	/// <returns>日志记录器。</returns>
	public ILogger CreateLogger(string categoryName) => new InMemoryLogger(this);

	/// <inheritdoc />
	public void Dispose()
	{
	}

	/// <summary>
	/// 把日志写入所属提供程序的内存记录器，用于断言拦截器实际输出的日志内容。
	/// </summary>
	private sealed class InMemoryLogger : ILogger
	{
		private readonly InMemoryLoggerProvider _provider;

		public InMemoryLogger(InMemoryLoggerProvider provider)
		{
			_provider = provider;
		}

		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull
		{
			return null!;
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel is LogLevel.Debug or LogLevel.Error or LogLevel.Information or LogLevel.Trace or LogLevel.Warning or LogLevel.Critical;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			lock (_provider._entries)
			{
				_provider._entries.Add((logLevel, formatter(state, exception)));
			}
		}
	}
}

/// <summary>
/// 供代理拦截的虚拟成员目标。
/// </summary>
public class InterceptedTarget : BaseApplicationService
{
	/// <summary>
	/// 原样返回传入的字符串，作为普通参数日志记录的测试目标。
	/// </summary>
	public virtual string Echo(string value) => value;

	/// <summary>
	/// 短暂休眠后返回，用于验证无参数方法的拦截行为。
	/// </summary>
	public virtual void DoNothing() => System.Threading.Thread.Sleep(1);

	/// <summary>
	/// 原样返回密码字符串，参数名含敏感关键字，用于验证日志脱敏。
	/// </summary>
	public virtual string EchoWithPassword(string password) => password;

	/// <summary>
	/// 原样返回凭据字符串，参数标注 <see cref="SensitiveDataAttribute"/>，用于验证特性驱动的日志脱敏。
	/// </summary>
	public virtual string EchoWithSensitiveData([SensitiveData] string credential) => credential;

	/// <summary>
	/// 返回登录命令中的用户名，用于验证 DTO 内部敏感属性的脱敏。
	/// </summary>
	public virtual string EchoWithDto(LoginCommand command) => command.Username;

	/// <summary>
	/// 异步原样返回传入的字符串，用于验证异步方法的日志拦截。
	/// </summary>
	public virtual async Task<string> EchoAsync(string value)
	{
		await Task.Yield();
		return value;
	}

	/// <summary>
	/// 执行整数除法，除数为零时抛出 <see cref="DivideByZeroException"/>，用于验证异常日志。
	/// </summary>
	public virtual int Divide(int a, int b) => a / b;

	/// <summary>
	/// 标注零耗时阈值的同步方法，用于验证耗时日志输出。
	/// </summary>
	[Timing(ThresholdMs = 0)]
	public virtual string EchoTimed(string value) => value;

	/// <summary>
	/// 标注极大耗时阈值的同步方法，用于验证耗时未超过阈值时不输出日志。
	/// </summary>
	[Timing(ThresholdMs = 1000000000)]
	public virtual string EchoOverThreshold(string value) => value;

	/// <summary>
	/// 耗时阈值与警告阈值均为零的同步方法，用于验证耗时日志降级为警告。
	/// </summary>
	[Timing(ThresholdMs = 0, WarningThresholdMs = 0)]
	public virtual string EchoTimedWarning(string value) => value;

	/// <summary>
	/// 标注零耗时阈值的异步方法，用于验证异步完成后输出耗时日志。
	/// </summary>
	[Timing(ThresholdMs = 0)]
	public virtual async Task<string> EchoTimedAsync(string value)
	{
		await Task.Yield();
		return value;
	}
}

/// <summary>
/// 日志、跟踪与耗时拦截器的行为测试，验证日志级别开关、异常传播、跟踪元数据，
/// 以及敏感信息和耗时阈值的处理结果。
/// </summary>
public class InterceptorTests
{
	private static ServiceProvider CreateCapturingProvider(out InMemoryLoggerProvider provider, bool debugEnabled = true)
	{
		provider = new InMemoryLoggerProvider();
		var services = new ServiceCollection();
		var loggingProvider = provider;
		services.AddLogging(builder =>
		{
			builder.SetMinimumLevel(debugEnabled ? LogLevel.Debug : LogLevel.Warning);
			builder.AddProvider(loggingProvider);
		});
		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 调试级别开启时，日志应包含被调用方法名与实际参数值。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_DebugEnabled_ShouldLogMethodAndArguments()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("Echo") && entry.Message.Contains("hello"));
	}

	/// <summary>
	/// 最低日志级别高于调试时，不应产生任何调试日志。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_DebugDisabled_ShouldNotLog()
	{
		using var container = CreateCapturingProvider(out var logger, debugEnabled: false);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Debug);
	}

	/// <summary>
	/// 目标方法抛出异常时应记录错误日志，并把原异常重新抛给调用方。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_Exception_ShouldLogErrorAndRethrow()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		Assert.Throws<DivideByZeroException>(() => proxy.Divide(1, 0));
		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Divide"));
	}

	/// <summary>
	/// 提供请求上下文访问器且调试开启时，应输出包含 <c>TraceInfo</c> 的跟踪日志。
	/// </summary>
	[Fact]
	public void TracingInterceptor_WithAccessorAndDebug_ShouldLogTrace()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var accessor = new StubRequestContextAccessor();
		var interceptor = new TracingInterceptor(loggerFactory, accessor);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("TraceInfo"));
	}

	/// <summary>
	/// 未提供请求上下文访问器时，不应输出跟踪日志。
	/// </summary>
	[Fact]
	public void TracingInterceptor_WithoutAccessor_ShouldNotLog()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TracingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("TraceInfo"));
	}

	/// <summary>
	/// 请求头同时包含跟踪 ID 与关联 ID 时，跟踪日志应同时记录两者。
	/// </summary>
	[Fact]
	public void TracingInterceptor_WithCorrelationId_ShouldLogTraceMetadata()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				Headers = new Dictionary<string, string>
				{
					["X-Request-Trace-Id"] = "trace-9",
					["X-Correlation-ID"] = "corr-9"
				}
			}
		};
		var interceptor = new TracingInterceptor(loggerFactory, accessor);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug
			&& entry.Message.Contains("TraceInfo")
			&& entry.Message.Contains("trace-9")
			&& entry.Message.Contains("corr-9"));
	}

	/// <summary>
	/// 仅存在跟踪标识而没有关联 ID 时，跟踪日志只包含跟踪标识。
	/// </summary>
	[Fact]
	public void TracingInterceptor_WithTraceIdentifierButNoCorrelation_ShouldLogTraceIdOnly()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext { TraceIdentifier = "trace-42" }
		};
		var interceptor = new TracingInterceptor(loggerFactory, accessor);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug
			&& entry.Message.Contains("TraceInfo")
			&& entry.Message.Contains("trace-42"));
	}

	/// <summary>
	/// 参数名包含敏感关键字（如 <c>password</c>）时，日志中的参数值应以 <c>***</c> 掩码替代。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_SensitiveKeyword_ShouldMaskArgument()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoWithPassword("plain-password");

		var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("EchoWithPassword"));
		Assert.DoesNotContain("plain-password", entry.Message);
		Assert.Contains("\"***\"", entry.Message);
	}

	/// <summary>
	/// 参数标注 <see cref="SensitiveDataAttribute"/> 时，日志中的参数值应以 <c>***</c> 掩码替代。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_SensitiveDataAttribute_ShouldMaskArgument()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoWithSensitiveData("secret-credential");

		var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("EchoWithSensitiveData"));
		Assert.DoesNotContain("secret-credential", entry.Message);
		Assert.Contains("\"***\"", entry.Message);
	}

	/// <summary>
	/// 普通参数不含敏感标记时，日志应保留其原始值。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_NormalArgument_ShouldKeepOriginalValue()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hello");

		var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("Echo"));
		Assert.Contains("hello", entry.Message);
	}

	/// <summary>
	/// DTO 中标注敏感特性的属性应被掩码为 <c>###</c>，非敏感属性保持原值。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_DtoWithSensitiveProperty_ShouldMaskInnerProperty()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoWithDto(new LoginCommand { Username = "alice", Password = "plain-password" });

		var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("EchoWithDto"));
		Assert.DoesNotContain("plain-password", entry.Message);
		Assert.Contains("alice", entry.Message);
		Assert.Contains("\"###\"", entry.Message);
	}

	/// <summary>
	/// 敏感属性配置了自定义掩码文本时，日志应使用该文本替换原值。
	/// </summary>
	[Fact]
	public void LoggingInterceptor_DtoWithCustomMask_ShouldUseCustomMaskText()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new LoggingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoWithDto(new LoginCommand { Username = "alice", Password = "plain-password" });

		var entry = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("EchoWithDto"));
		Assert.Contains("\"###\"", entry.Message);
	}

	/// <summary>
	/// 同步方法耗时未超过阈值时，应输出包含方法名与毫秒数的信息级耗时日志。
	/// </summary>
	[Fact]
	public void TimingInterceptor_SyncBelowThreshold_ShouldLogElapsed()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TimingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoTimed("hi");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information
			&& entry.Message.Contains("Timing:")
			&& entry.Message.Contains("EchoTimed")
			&& entry.Message.Contains("ms"));
	}

	/// <summary>
	/// 方法耗时远低于配置的 <c>ThresholdMs</c> 时不应输出耗时日志。
	/// </summary>
	[Fact]
	public void TimingInterceptor_OverThreshold_ShouldNotLog()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TimingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoOverThreshold("hi");

		Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("Timing:"));
	}

	/// <summary>
	/// 方法耗时超过 <c>WarningThresholdMs</c> 时，耗时日志应降级为警告级别。
	/// </summary>
	[Fact]
	public void TimingInterceptor_ExceedsWarningThreshold_ShouldLogWarning()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TimingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.EchoTimedWarning("hi");

		Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning
			&& entry.Message.Contains("Timing:")
			&& entry.Message.Contains("EchoTimedWarning"));
	}

	/// <summary>
	/// 异步方法完成后应输出耗时日志，验证异步续延中仍能正确计时。
	/// </summary>
	[Fact]
	public async Task TimingInterceptor_Async_ShouldLogElapsedOnCompletion()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TimingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		_ = await proxy.EchoTimedAsync("hi");
		await WaitUntilLoggedAsync(logger, "Timing:");
	}

	/// <summary>
	/// 未标注 <see cref="TimingAttribute"/> 的方法不应输出耗时日志。
	/// </summary>
	[Fact]
	public void TimingInterceptor_WithoutAttribute_ShouldNotLog()
	{
		using var container = CreateCapturingProvider(out var logger);
		var loggerFactory = container.GetRequiredService<ILoggerFactory>();
		var interceptor = new TimingInterceptor(loggerFactory);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateClassProxy<InterceptedTarget>(interceptor);

		proxy.Echo("hi");

		Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("Timing:"));
	}

	private static async Task WaitUntilLoggedAsync(InMemoryLoggerProvider logger, string fragment)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (logger.Entries.Any(entry => entry.Message.Contains(fragment, StringComparison.Ordinal)))
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Fail("Timing log entry was not written within the timeout.");
	}
}

/// <summary>
/// 含敏感属性的登录命令 DTO。
/// </summary>
public class LoginCommand
{
	/// <summary>
	/// 登录用户名，属于普通属性，日志中原样输出。
	/// </summary>
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// 登录密码，日志中按 <see cref="SensitiveDataAttribute.Mask"/> 配置的 <c>###</c> 文本脱敏。
	/// </summary>
	[SensitiveData(Mask = "###")]
	public string Password { get; set; } = string.Empty;
}