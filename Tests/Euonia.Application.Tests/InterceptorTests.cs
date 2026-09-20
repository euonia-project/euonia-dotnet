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
	public RequestContext Context { get; set; }
}

/// <summary>
/// 捕获日志条目的内存日志提供程序，便于断言拦截器输出的日志内容。
/// </summary>
internal sealed class InMemoryLoggerProvider : ILoggerProvider
{
	private readonly List<(LogLevel Level, string Message)> _entries = [];

	public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

	public ILogger CreateLogger(string categoryName) => new InMemoryLogger(this);

	public void Dispose()
	{
	}

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
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel is LogLevel.Debug or LogLevel.Error or LogLevel.Information or LogLevel.Trace or LogLevel.Warning or LogLevel.Critical;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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
	public virtual string Echo(string value) => value;

	public virtual void DoNothing() => System.Threading.Thread.Sleep(1);

	public virtual string EchoWithPassword(string password) => password;

	public virtual string EchoWithSensitiveData([SensitiveData] string credential) => credential;

	public virtual string EchoWithDto(LoginCommand command) => command.Username;

	public virtual async Task<string> EchoAsync(string value)
	{
		await Task.Yield();
		return value;
	}

	public virtual int Divide(int a, int b) => a / b;

	[Timing(ThresholdMs = 0)]
	public virtual string EchoTimed(string value) => value;

	[Timing(ThresholdMs = 1000000000)]
	public virtual string EchoOverThreshold(string value) => value;

	[Timing(ThresholdMs = 0)]
	public virtual async Task<string> EchoTimedAsync(string value)
	{
		await Task.Yield();
		return value;
	}
}

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
	public string Username { get; set; }

	[SensitiveData(Mask = "###")]
	public string Password { get; set; }
}