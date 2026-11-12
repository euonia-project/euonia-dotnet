using System.Diagnostics;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Telemetry;

namespace Nerosoft.Euonia.Core.Tests.Telemetry;

/// <summary>
/// 遥测内核：源开关、脱敏、标签补充、传播与上下文。
/// </summary>
public class TelemetryCoreTests
{
	private static TelemetrySource CreateSource(
		bool enabled = true,
		string[]? redactionKeys = null,
		ITelemetryEnricher[]? enrichers = null,
		ITelemetryContextProvider? contextProvider = null)
	{
		var options = new TelemetryOptions { Enabled = enabled };
		if (redactionKeys != null)
		{
			options.Redaction.Keys = redactionKeys;
		}

		return new TelemetrySource(
			"test-source",
			tracesEnabled: enabled,
			metricsEnabled: enabled,
			redactor: new DefaultTelemetryRedactor(new StaticOptionsMonitor(options)),
			enrichers: enrichers,
			contextProvider: contextProvider);
	}

	private static ActivityListener Listen(TelemetrySource source) =>
		Listen(name => name == source.SourceName);

	private static ActivityListener Listen(Func<string, bool> shouldListen)
	{
		var listener = new ActivityListener
		{
			ShouldListenTo = source => shouldListen(source.Name),
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
		};

		ActivitySource.AddActivityListener(listener);
		return listener;
	}

	/// <summary>
	/// 无监听者时返回空活动，所有操作空操作。
	/// </summary>
	[Fact]
	public void StartActivity_WithoutListener_ShouldBeEmpty()
	{
		var source = CreateSource();
		using var activity = source.StartActivity("op");

		Assert.False(activity.IsRecording);
		Assert.Null(activity.Activity);
		activity.SetTag("k", "v");
		activity.SetOutcome(true);
	}

	/// <summary>
	/// 关闭时即使有监听者也不创建活动。
	/// </summary>
	[Fact]
	public void StartActivity_WhenDisabled_ShouldBeEmpty()
	{
		using var listener = Listen(_ => true);
		var source = CreateSource(enabled: false);
		using var activity = source.StartActivity("op");

		Assert.False(activity.IsRecording);
	}

	/// <summary>
	/// 标签写入前按脱敏键替换为掩码。
	/// </summary>
	[Fact]
	public void SetTag_ShouldRedactConfiguredKeys()
	{
		var source = CreateSource(redactionKeys: ["db.statement"]);
		using var listener = Listen(source);

		using var activity = source.StartActivity("op");
		activity.SetTag("db.statement", "select * from users");
		activity.SetTag("db.system", "postgresql");

		Assert.Equal("***", activity.Activity!.GetTagItem("db.statement"));
		Assert.Equal("postgresql", activity.Activity.GetTagItem("db.system"));
	}

	/// <summary>
	/// 补充器把执行上下文中的租户写入 Trace 标签。
	/// </summary>
	[Fact]
	public void Enricher_ShouldAddTenantTags()
	{
		AmbientExecutionContext.Reset();
		var options = Options.Create(new TelemetryOptions { Enabled = true, RecordTenantId = true });
		var enricher = new TelemetryContextEnricher(new StaticOptionsMonitor(options.Value));
		var source = CreateSource(enrichers: [enricher]);
		using var listener = Listen(source);

		using (AmbientExecutionContext.Push(values =>
		       {
			       values.TenantId = "t1";
			       values.ActorTenantId = "t1";
			       values.CorrelationId = "c1";
		       }))
		{
			using var activity = source.StartActivity("op");
			Assert.Equal("t1", activity.Activity!.GetTagItem(TelemetryNames.TenantId));
			Assert.Equal("t1", activity.Activity.GetTagItem(TelemetryNames.TenantActorId));
			Assert.Equal("c1", activity.Activity.GetTagItem(TelemetryNames.CorrelationId));
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 传播器注入后可提取出相同 TraceId。
	/// </summary>
	[Fact]
	public void Propagator_ShouldRoundTripTraceContext()
	{
		using var listener = Listen(_ => true);
		using var activity = new Activity("test").Start();

		var propagator = new DefaultTelemetryPropagator();
		var carrier = new Dictionary<string, object>();
		propagator.Inject(carrier, activity);

		Assert.True(carrier.ContainsKey("traceparent"));

		var extracted = propagator.Extract(carrier);
		Assert.NotNull(extracted);
		Assert.Equal(activity.TraceId, extracted!.Value.TraceId);
	}

	/// <summary>
	/// 富化器抛异常时结束活动并恢复 Activity.Current。
	/// </summary>
	[Fact]
	public void StartActivity_WhenEnricherThrows_ShouldRestoreCurrent()
	{
		var source = CreateSource(enrichers: [new ThrowingEnricher()]);
		using var listener = Listen(source);
		using var parent = new Activity("parent").Start();

		var result = source.StartActivity("child");

		Assert.Null(result.Activity);
		Assert.Same(parent, Activity.Current);
	}

	private sealed class ThrowingEnricher : ITelemetryEnricher
	{
		public void Enrich(Activity activity, TelemetryContext? context)
		{
			throw new InvalidOperationException("boom");
		}
	}

	/// <summary>
	/// 非法 traceparent 按新根处理，不抛异常。
	/// </summary>
	[Fact]
	public void Propagator_ShouldIgnoreInvalidTraceParent()
	{
		var propagator = new DefaultTelemetryPropagator();
		var carrier = new Dictionary<string, object> { ["traceparent"] = "not-a-traceparent" };

		Assert.Null(propagator.Extract(carrier));
	}

	/// <summary>
	/// 上下文提供程序回退到执行上下文，不生成新标识。
	/// </summary>
	[Fact]
	public void ContextProvider_ShouldReadExecutionContext()
	{
		AmbientExecutionContext.Reset();
		var provider = new DefaultTelemetryContextProvider();

		using (AmbientExecutionContext.Push(values =>
		       {
			       values.TenantId = "t1";
			       values.CorrelationId = "c1";
		       }))
		{
			var context = provider.GetContext();
			Assert.NotNull(context);
			Assert.Equal("t1", context!.TenantId);
			Assert.Equal("c1", context.CorrelationId);
		}

		AmbientExecutionContext.Reset();
		Assert.Null(provider.GetContext());
	}

	/// <summary>
	/// 组件开关覆盖全局设置。
	/// </summary>
	[Fact]
	public void Factory_ShouldHonorComponentOverride()
	{
		var options = new TelemetryOptions { Enabled = true };
		options.Components["Nerosoft.Euonia.Bus"] = new TelemetryComponentOptions { Enabled = false };

		var monitor = new StaticOptionsMonitor(options);
		var factory = new TelemetrySourceFactory(monitor, new DefaultTelemetryRedactor(monitor), []);

		var enabled = factory.Create("Nerosoft.Euonia.Caching");
		var disabled = factory.Create("Nerosoft.Euonia.Bus");

		using var listener = Listen(name => name == "Nerosoft.Euonia.Caching");

		Assert.True(enabled.IsTraceEnabled);
		Assert.True(enabled.IsMetricEnabled);
		Assert.False(disabled.IsTraceEnabled);
		Assert.False(disabled.IsMetricEnabled);
	}

	private sealed class StaticOptionsMonitor : IOptionsMonitor<TelemetryOptions>
	{
		private readonly TelemetryOptions _value;

		public StaticOptionsMonitor(TelemetryOptions value)
		{
			_value = value;
		}

		public TelemetryOptions CurrentValue => _value;

		public TelemetryOptions Get(string? name) => _value;

		public IDisposable? OnChange(Action<TelemetryOptions, string?> listener) => null;
	}
}
