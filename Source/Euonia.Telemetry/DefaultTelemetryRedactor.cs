using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 基于键名的默认脱敏器。
/// </summary>
public sealed class DefaultTelemetryRedactor : ITelemetryRedactor
{
	/// <summary>
	/// 脱敏后的占位值。
	/// </summary>
	public const string Mask = "***";

	private readonly IOptionsMonitor<TelemetryOptions>? _options;
	private readonly object _sync = new();
	private volatile TelemetryRedactionOptions? _snapshot;
	private volatile string[]? _keysSource;
	private volatile HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 初始化 <see cref="DefaultTelemetryRedactor"/> 的新实例。
	/// </summary>
	/// <param name="options">遥测选项监视器；支持键集合热更新。</param>
	public DefaultTelemetryRedactor(IOptionsMonitor<TelemetryOptions> options)
	{
		_options = options;
	}

	/// <inheritdoc />
	public bool ShouldRedact(string key)
	{
		var redaction = _options!.CurrentValue.Redaction;
		if (!ReferenceEquals(redaction, _snapshot) || !ReferenceEquals(redaction.Keys, _keysSource))
		{
			lock (_sync)
			{
				if (!ReferenceEquals(redaction, _snapshot) || !ReferenceEquals(redaction.Keys, _keysSource))
				{
					_keys = new HashSet<string>(redaction.Keys, StringComparer.OrdinalIgnoreCase);
					_snapshot = redaction;
					_keysSource = redaction.Keys;
				}
			}
		}

		return redaction.Enabled && _keys.Contains(key);
	}

	/// <inheritdoc />
	public object? Redact(string key, object? value)
	{
		return ShouldRedact(key) ? Mask : value;
	}
}
