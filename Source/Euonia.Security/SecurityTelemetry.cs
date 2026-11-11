using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 安全组件的遥测入口：授权判定 Span 与决策指标。
/// </summary>
public static class SecurityTelemetry
{
	/// <summary>ActivitySource / Meter 名称。</summary>
	public const string SourceName = "Nerosoft.Euonia.Security";

	/// <summary>授权判定活动。</summary>
	public const string AuthorizeActivity = "euonia.security.authorize";

	/// <summary>授权决策计数。</summary>
	public const string DecisionsMetric = "euonia.security.authorization.decisions";

	/// <summary>底层 ActivitySource。</summary>
	public static readonly ActivitySource Source = new(SourceName);

	private static readonly Meter _meter = new(SourceName);
	private static readonly Counter<long> _decisions = _meter.CreateCounter<long>(DecisionsMetric, "{decision}");

	/// <summary>
	/// 启动授权判定活动。
	/// </summary>
	/// <param name="permission">权限编码。</param>
	/// <returns>活动；未监听时为 <c>null</c>。</returns>
	public static Activity? Start(string permission)
	{
		return Source.StartActivity(
			AuthorizeActivity,
			ActivityKind.Internal,
			Activity.Current?.Context ?? default,
			[new KeyValuePair<string, object?>("security.permission", permission)]);
	}

	/// <summary>
	/// 记录判定结果。
	/// </summary>
	/// <param name="activity">活动。</param>
	/// <param name="granted">是否放行。</param>
	public static void Complete(Activity? activity, bool granted)
	{
		_decisions.Add(1, new TagList { { "decision", granted ? "granted" : "denied" } });

		if (activity == null)
		{
			return;
		}

		activity.SetTag("euonia.outcome", granted ? "success" : "failure");
		activity.Dispose();
	}
}
