using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Tenancy;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests.Auditing;

/// <summary>
/// 审计内核：作用域、动作、脱敏、空记录与幂等存储。
/// </summary>
public class AuditingCoreTests
{
	private static AuditingRecorder CreateRecorder(CapturingWriter writer, bool enabled = true)
	{
		return new AuditingRecorder(new AuditingOptions { Enabled = enabled, RecordParameters = true, RecordResult = true }, writer);
	}

	/// <summary>
	/// 动作完成后自动提交记录，参数与结果被脱敏。
	/// </summary>
	[Fact]
	public async Task Scope_ShouldSubmitMaskedRecord()
	{
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:1" });
		var action = scope.BeginAction("OrderService", "CreateAsync", "CreateOrder");
		action.SetParameter("request", new Credentials { Password = "p@ss", Name = "alice" });
		action.Complete(new Credentials { Password = "token", Name = "result" });

		await WaitAsync(() => writer.Records.Count == 1);

		var record = Assert.Single(writer.Records);
		Assert.EndsWith(":req:1", record.ScopeId!, StringComparison.Ordinal);

		var entry = Assert.Single(record.Actions);
		Assert.True(entry.IsSuccess);
		Assert.Contains("***", entry.Parameters);
		Assert.DoesNotContain("p@ss", entry.Parameters);
		Assert.Contains("***", entry.Result);
		Assert.DoesNotContain("token", entry.Result);
	}

	/// <summary>
	/// 没有动作的作用域仍然提交一条记录。
	/// </summary>
	[Fact]
	public async Task Scope_ShouldSubmitEmptyRecord()
	{
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:empty", AutoComplete = false });
		await scope.CompleteAsync(TestContext.Current.CancellationToken);

		var record = Assert.Single(writer.Records);
		Assert.Empty(record.Actions);
		Assert.Null(record.IsSuccess);
	}

	/// <summary>
	/// 审计关闭时不产生任何记录。
	/// </summary>
	[Fact]
	public async Task DisabledRecorder_ShouldNotSubmit()
	{
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer, enabled: false);

		var scope = recorder.EnsureScope();
		scope.BeginAction("Svc", "Do").Complete(null);
		await scope.CompleteAsync(TestContext.Current.CancellationToken);

		Assert.Empty(writer.Records);
	}

	/// <summary>
	/// 执行上下文中的租户与用户被回填到记录。
	/// </summary>
	[Fact]
	public async Task Scope_ShouldFillTenantAndUserFromExecutionContext()
	{
		AmbientExecutionContext.Reset();
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		using (AmbientExecutionContext.Push(values =>
		       {
			       values.ActorTenantId = "t1";
			       values.TenantName = "Tenant One";
			       values.UserId = "u1";
			       values.UserName = "Alice";
			       values.CorrelationId = "c1";
		       }))
		{
			var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:ctx", AutoComplete = false });
			await scope.CompleteAsync(TestContext.Current.CancellationToken);
		}

		var record = Assert.Single(writer.Records);
		Assert.Equal("t1", record.TenantId);
		Assert.Equal("Tenant One", record.TenantName);
		Assert.Equal("u1", record.UserId);
		Assert.Equal("Alice", record.UserName);
		Assert.Equal("c1", record.CorrelationId);

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 失败动作记录异常信息。
	/// </summary>
	[Fact]
	public async Task Action_ShouldRecordFailure()
	{
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:fail", AutoComplete = false });
		var action = scope.BeginAction("Svc", "Do");
		action.Fail(new InvalidOperationException("boom"));
		await scope.CompleteAsync(TestContext.Current.CancellationToken);

		var entry = Assert.Single(Assert.Single(writer.Records).Actions);
		Assert.False(entry.IsSuccess);
		Assert.Equal(typeof(InvalidOperationException).FullName, entry.ExceptionType);
		Assert.Equal("boom", entry.ExceptionMessage);
	}

	/// <summary>
	/// 嵌套动作记录父动作标识。
	/// </summary>
	[Fact]
	public async Task NestedActions_ShouldLinkParent()
	{
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:nested", AutoComplete = false });
		var outer = scope.BeginAction("Outer", "Run");
		var inner = scope.BeginAction("Inner", "Run");
		inner.Complete(null);
		outer.Complete(null);
		await scope.CompleteAsync(TestContext.Current.CancellationToken);

		var actions = Assert.Single(writer.Records).Actions;
		Assert.Equal(2, actions.Count);
		Assert.Equal(outer.ActionId, actions.Single(x => x.ServiceName == "Inner").ParentActionId);
	}

	/// <summary>
	/// 内存存储按作用域幂等，重复保存不产生重复行。
	/// </summary>
	[Fact]
	public async Task Store_ShouldBeIdempotentByScopeId()
	{
		var store = new InMemoryAuditingStore();
		var record = new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:req:1",
			Actions =
			[
				new AuditingAction { ActionId = Guid.CreateVersion7(), IdempotencyKey = "k1", ServiceName = "A", MethodName = "M" },
			],
		};

		await store.SaveAsync(record, TestContext.Current.CancellationToken);
		await store.SaveAsync(record, TestContext.Current.CancellationToken);

		var saved = Assert.Single(store.Records);
		Assert.Single(saved.Actions);
	}

	/// <summary>
	/// 审计查询默认限定在操作人租户；跨租户必须声明原因。
	/// </summary>
	[Fact]
	public async Task Query_ShouldEnforceTenantScope()
	{
		AmbientExecutionContext.Reset();
		var store = new InMemoryAuditingStore();
		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:1",
			TenantId = "t1",
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
		}, TestContext.Current.CancellationToken);

		using (new TenantScopeFactory().Begin(new TenantInfo { Id = "t1" }))
		{
			Assert.Single(await store.QueryAsync(new AuditingQuery(), TestContext.Current.CancellationToken));

			await Assert.ThrowsAsync<UnauthorizedAccessException>(
				() => store.QueryAsync(new AuditingQuery { TenantId = "t2" }, TestContext.Current.CancellationToken));

			await Assert.ThrowsAsync<ArgumentException>(
				() => store.QueryAsync(new AuditingQuery { TenantId = "t2", AllowCrossTenant = true }, TestContext.Current.CancellationToken));

			var cross = await store.QueryAsync(
				new AuditingQuery { TenantId = "t2", AllowCrossTenant = true, CrossTenantReason = "support" },
				TestContext.Current.CancellationToken);
			Assert.Empty(cross);
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 跨租户作用域的审计记录带操作人租户、目标租户与原因。
	/// </summary>
	[Fact]
	public async Task Scope_ShouldRecordCrossTenantFields()
	{
		AmbientExecutionContext.Reset();
		var writer = new CapturingWriter();
		var recorder = CreateRecorder(writer);

		using (new TenantScopeFactory().Begin(new TenantInfo { Id = "t1" }))
		{
			using (new TenantScopeFactory().BeginCrossTenant("support-ticket-1", "t2"))
			{
				var scope = recorder.EnsureScope(new AuditingScopeOptions { OperationKey = "req:cross", AutoComplete = false });
				await scope.CompleteAsync(TestContext.Current.CancellationToken);
			}
		}

		var record = Assert.Single(writer.Records);
		Assert.Equal("t1", record.TenantId);
		Assert.Equal("t2", record.TargetTenantId);
		Assert.True(record.IsCrossTenant);
		Assert.Equal("support-ticket-1", record.CrossTenantReason);

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 内存存储按租户 / 结果查询，并按时间清理。
	/// </summary>
	[Fact]
	public async Task Store_ShouldQueryAndDeleteBefore()
	{
		var store = new InMemoryAuditingStore();
		var now = DateTime.UtcNow;

		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:old",
			TenantId = "t1",
			IsSuccess = true,
			ExecutionTime = now.AddHours(-2),
			RecordedAt = now,
		}, TestContext.Current.CancellationToken);

		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:new",
			TenantId = "t1",
			IsSuccess = false,
			ExecutionTime = now,
			RecordedAt = now,
		}, TestContext.Current.CancellationToken);

		await store.SaveAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t2:new",
			TenantId = "t2",
			IsSuccess = true,
			ExecutionTime = now,
			RecordedAt = now,
		}, TestContext.Current.CancellationToken);

		var failures = await store.QueryAsync(new AuditingQuery { TenantId = "t1", IsSuccess = false }, TestContext.Current.CancellationToken);
		Assert.Single(failures);
		Assert.Equal("t1:new", failures[0].ScopeId);

		var found = await store.FindByScopeAsync("t2:new", TestContext.Current.CancellationToken);
		Assert.NotNull(found);

		var deleted = await store.DeleteBeforeAsync(now.AddHours(-1), TestContext.Current.CancellationToken);
		Assert.Equal(1, deleted);
		Assert.Equal(2, (await store.QueryAsync(new AuditingQuery(), TestContext.Current.CancellationToken)).Count);
	}

	/// <summary>
	/// 成功记录批量写入，失败记录立即写入。
	/// </summary>
	[Fact]
	public async Task Writer_ShouldBatchSuccessAndDeliverFailureImmediately()
	{
		var store = new RecordingStore();
		var services = new ServiceCollection();
		services.AddSingleton<IAuditingStore>(store);
		await using var provider = services.BuildServiceProvider();

		await using var writer = new AuditingWriter(
			provider,
			Microsoft.Extensions.Options.Options.Create(new AuditingOptions
			{
				Enabled = true,
				BatchSize = 10,
				FailureDelivery = AuditingDelivery.Immediate,
			}));

		for (var index = 0; index < 3; index++)
		{
			await writer.WriteAsync(NewRecord($"t1:req:{index}", true), TestContext.Current.CancellationToken);
		}

		await writer.StartAsync(TestContext.Current.CancellationToken);
		await writer.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, store.SaveManyCalls);
		Assert.Equal(0, store.SaveCalls);

		await writer.WriteAsync(NewRecord("t1:req:fail", false), TestContext.Current.CancellationToken);
		await writer.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, store.SaveCalls);
		Assert.Equal(4, store.RecordCount);
	}

	/// <summary>
	/// 存储路由把不同租户记录分发到各自审计库。
	/// </summary>
	[Fact]
	public async Task Writer_ShouldRouteRecordsToPerTenantStores()
	{
		var storeA = new RecordingStore();
		var storeB = new RecordingStore();
		var router = new TenantRouter(storeA, storeB);

		var services = new ServiceCollection();
		services.AddSingleton<IAuditingStoreRouter>(router);
		await using var provider = services.BuildServiceProvider();

		await using var writer = new AuditingWriter(
			provider,
			Microsoft.Extensions.Options.Options.Create(new AuditingOptions { Enabled = true, BatchSize = 10 }));

		await writer.WriteAsync(RoutedRecord("a:1", "a"), TestContext.Current.CancellationToken);
		await writer.WriteAsync(RoutedRecord("b:1", "b"), TestContext.Current.CancellationToken);
		await writer.WriteAsync(RoutedRecord("a:2", "a"), TestContext.Current.CancellationToken);

		await writer.StartAsync(TestContext.Current.CancellationToken);
		await writer.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(2, storeA.RecordCount);
		Assert.Equal(1, storeB.RecordCount);
	}

	private static AuditingRecord RoutedRecord(string scopeId, string tenantId)
	{
		return new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = scopeId,
			TenantId = tenantId,
			IsSuccess = true,
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
		};
	}

	private sealed class TenantRouter : IAuditingStoreRouter
	{
		private readonly IAuditingStore _a;
		private readonly IAuditingStore _b;

		public TenantRouter(IAuditingStore a, IAuditingStore b)
		{
			_a = a;
			_b = b;
		}

		public IAuditingStore? GetStore(AuditingRecord record)
		{
			return record.TenantId == "a" ? _a : _b;
		}
	}

	private static AuditingRecord NewRecord(string scopeId, bool success)
	{
		return new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = scopeId,
			IsSuccess = success,
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
		};
	}

	/// <summary>
	/// 停止写入器时排空队列，业务关闭不丢记录。
	/// </summary>
	[Fact]
	public async Task Writer_ShouldDrainOnStop()
	{
		var store = new InMemoryAuditingStore();
		var services = new ServiceCollection();
		services.AddSingleton<IAuditingStore>(store);
		await using var provider = services.BuildServiceProvider();

		await using var writer = new AuditingWriter(
			provider,
			Microsoft.Extensions.Options.Options.Create(new AuditingOptions { Enabled = true }));

		await writer.StartAsync(TestContext.Current.CancellationToken);
		await writer.WriteAsync(new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = "t1:req:drain",
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
		}, TestContext.Current.CancellationToken);

		await writer.StopAsync(TestContext.Current.CancellationToken);

		Assert.Single(store.Records);
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

	private sealed class RecordingStore : IAuditingStore
	{
		public int SaveCalls { get; private set; }

		public int SaveManyCalls { get; private set; }

		public int RecordCount { get; private set; }

		public Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken = default)
		{
			SaveCalls++;
			RecordCount++;
			return Task.CompletedTask;
		}

		public Task SaveManyAsync(IReadOnlyCollection<AuditingRecord> records, CancellationToken cancellationToken = default)
		{
			SaveManyCalls++;
			RecordCount += records.Count;
			return Task.CompletedTask;
		}
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

	private sealed class Credentials
	{
		[SensitiveData]
		public string? Password { get; set; }

		public string? Name { get; set; }
	}
}
