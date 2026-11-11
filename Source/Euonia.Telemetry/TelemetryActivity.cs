using System.Diagnostics;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 活动句柄；无监听者时所有操作空操作。
/// </summary>
public readonly struct TelemetryActivity : IDisposable
{
	private readonly ITelemetryRedactor? _redactor;

	/// <summary>
	/// 初始化 <see cref="TelemetryActivity"/> 的新实例。
	/// </summary>
	/// <param name="activity">底层活动；可为 <c>null</c>。</param>
	/// <param name="redactor">标签脱敏器。</param>
	public TelemetryActivity(Activity? activity, ITelemetryRedactor? redactor = null)
	{
		Activity = activity;
		_redactor = redactor;
	}

	/// <summary>
	/// 获取底层活动。
	/// </summary>
	public Activity? Activity { get; }

	/// <summary>
	/// 获取一个值，指示当前是否记录。
	/// </summary>
	public bool IsRecording => Activity != null;

	/// <summary>
	/// 设置标签；值经脱敏。
	/// </summary>
	/// <param name="key">标签键。</param>
	/// <param name="value">标签值。</param>
	public void SetTag(string key, object? value)
	{
		if (Activity == null || value == null)
		{
			return;
		}

		Activity.SetTag(key, _redactor?.Redact(key, value) ?? value);
	}

	/// <summary>
	/// 设置结果标签与状态。
	/// </summary>
	/// <param name="success">是否成功。</param>
	/// <param name="errorType">异常类型；仅失败时使用。</param>
	public void SetOutcome(bool success, string? errorType = null)
	{
		if (Activity == null)
		{
			return;
		}

		Activity.SetTag(TelemetryNames.Outcome, success ? "success" : "failure");
		if (!success)
		{
			if (!string.IsNullOrWhiteSpace(errorType))
			{
				Activity.SetTag(TelemetryNames.ErrorType, errorType);
			}

			Activity.SetStatus(ActivityStatusCode.Error);
		}
	}

	/// <summary>
	/// 添加事件。
	/// </summary>
	/// <param name="name">事件名称。</param>
	/// <param name="tags">事件标签。</param>
	public void AddEvent(string name, params KeyValuePair<string, object?>[] tags)
	{
		var redactor = _redactor;
		var collection = new ActivityTagsCollection(tags.Select(tag =>
			new KeyValuePair<string, object?>(tag.Key, redactor?.Redact(tag.Key, tag.Value) ?? tag.Value)));
		Activity?.AddEvent(new ActivityEvent(name, tags: collection));
	}

	/// <summary>
	/// 记录异常；不写入异常消息作为标签。
	/// </summary>
	/// <param name="exception">异常。</param>
	public void RecordException(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception);

		if (Activity == null)
		{
			return;
		}

		Activity.SetTag(TelemetryNames.ErrorType, exception.GetType().FullName);
		Activity.SetStatus(ActivityStatusCode.Error);
		Activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
		{
			["exception.type"] = exception.GetType().FullName,
			["exception.stacktrace"] = exception.StackTrace,
		}));
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Activity?.Dispose();
	}
}
