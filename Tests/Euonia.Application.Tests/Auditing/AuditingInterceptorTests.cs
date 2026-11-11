using Castle.DynamicProxy;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests.Auditing;

/// <summary>
/// 审计拦截器：同步、异步、异常与脱敏。
/// </summary>
public class AuditingInterceptorTests
{
	private static (AuditingInterceptor Interceptor, CapturingWriter Writer) Create()
	{
		var writer = new CapturingWriter();
		var options = new AuditingOptions { Enabled = true, RecordAnonymous = true };
		var recorder = new AuditingRecorder(options, writer);
		return (new AuditingInterceptor(recorder, options), writer);
	}

	/// <summary>
	/// 同步方法记录参数、结果与耗时。
	/// </summary>
	[Fact]
	public async Task Intercept_ShouldRecordSyncCall()
	{
		var (interceptor, writer) = Create();
		var proxy = new ProxyGenerator().CreateClassProxy<AuditedService>(interceptor);

		var result = proxy.Combine(new Credentials { Password = "secret" }, 2);

		Assert.Equal("alice:2", result);
		await WaitAsync(() => writer.Records.Count == 1);

		var action = Assert.Single(Assert.Single(writer.Records).Actions);
		Assert.True(action.IsSuccess);
		Assert.Contains("***", action.Parameters);
		Assert.DoesNotContain("secret", action.Parameters);
		Assert.Contains("alice:2", action.Result);
	}

	/// <summary>
	/// 异步方法在任务完成后封口。
	/// </summary>
	[Fact]
	public async Task Intercept_ShouldAwaitAsyncCall()
	{
		var (interceptor, writer) = Create();
		var proxy = new ProxyGenerator().CreateClassProxy<AuditedService>(interceptor);

		var result = await proxy.CombineAsync(new Credentials { Password = "secret" }, 3);

		Assert.Equal("alice:3", result);
		await WaitAsync(() => writer.Records.Count == 1);
		Assert.True(Assert.Single(Assert.Single(writer.Records).Actions).IsSuccess);
	}

	/// <summary>
	/// 异常向上传播且动作记录失败。
	/// </summary>
	[Fact]
	public async Task Intercept_ShouldRecordFailureAndRethrow()
	{
		var (interceptor, writer) = Create();
		var proxy = new ProxyGenerator().CreateClassProxy<AuditedService>(interceptor);

		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.FailAsync());

		await WaitAsync(() => writer.Records.Count == 1);
		var action = Assert.Single(Assert.Single(writer.Records).Actions);
		Assert.False(action.IsSuccess);
		Assert.Equal("boom", action.ExceptionMessage);
	}

	/// <summary>
	/// WritesOnly 模式跳过读前缀方法，写方法仍然记录。
	/// </summary>
	[Fact]
	public async Task WritesOnly_ShouldSkipReadMethods()
	{
		var writer = new CapturingWriter();
		var options = new AuditingOptions { Enabled = true, Mode = AuditingTriggerMode.WritesOnly, RecordAnonymous = true };
		var recorder = new AuditingRecorder(options, writer);
		var interceptor = new AuditingInterceptor(recorder, options);
		var proxy = new ProxyGenerator().CreateClassProxy<AuditedService>(interceptor);

		proxy.ReadSomething();
		Assert.Empty(writer.Records);

		proxy.Combine(new Credentials(), 1);
		await WaitAsync(() => writer.Records.Count == 1);
		Assert.Single(Assert.Single(writer.Records).Actions);
	}

	/// <summary>
	/// 默认不记录匿名成功调用。
	/// </summary>
	[Fact]
	public void Anonymous_ShouldBeSkippedByDefault()
	{
		var writer = new CapturingWriter();
		var options = new AuditingOptions { Enabled = true };
		var recorder = new AuditingRecorder(options, writer);
		var proxy = new ProxyGenerator().CreateClassProxy<AuditedService>(new AuditingInterceptor(recorder, options));

		proxy.Combine(new Credentials(), 1);

		Assert.Empty(writer.Records);
	}

	/// <summary>
	/// 审计拦截器排在最外层。
	/// </summary>
	[Fact]
	public void Order_ShouldBeOutermost()
	{
		var (interceptor, _) = Create();
		Assert.Equal(-1000, interceptor.Order);
	}

	private static async Task WaitAsync(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.True(condition());
	}

	private sealed class CapturingWriter : IAuditingWriter
	{
		public List<AuditingRecord> Records { get; } = [];

		public ValueTask WriteAsync(AuditingRecord record, CancellationToken cancellationToken = default)
		{
			lock (Records)
			{
				Records.Add(record);
			}

			return ValueTask.CompletedTask;
		}

		public Task FlushAsync(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}
}

/// <summary>
/// 被审计的测试服务。
/// </summary>
public class AuditedService
{
	/// <summary>
	/// 同步组合名称与数字。
	/// </summary>
	public virtual string Combine(Credentials credentials, int value)
	{
		return $"{credentials.Name}:{value}";
	}

	/// <summary>
	/// 异步组合名称与数字。
	/// </summary>
	public virtual async Task<string> CombineAsync(Credentials credentials, int value)
	{
		await Task.Yield();
		return $"{credentials.Name}:{value}";
	}

	/// <summary>
	/// 读方法；WritesOnly 下应跳过。
	/// </summary>
	public virtual string ReadSomething()
	{
		return "read";
	}

	/// <summary>
	/// 始终抛出异常。
	/// </summary>
	public virtual async Task FailAsync()
	{
		await Task.Yield();
		throw new InvalidOperationException("boom");
	}
}

/// <summary>
/// 含敏感字段的测试输入。
/// </summary>
public sealed class Credentials
{
	/// <summary>
	/// 获取或设置名称。
	/// </summary>
	public string? Name { get; set; } = "alice";

	/// <summary>
	/// 获取或设置密码。
	/// </summary>
	[SensitiveData]
	public string? Password { get; set; }
}
