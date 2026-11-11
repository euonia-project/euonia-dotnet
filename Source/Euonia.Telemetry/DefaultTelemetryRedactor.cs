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

	private readonly TelemetryRedactionOptions _options;
	private readonly HashSet<string> _keys;

	/// <summary>
	/// 初始化 <see cref="DefaultTelemetryRedactor"/> 的新实例。
	/// </summary>
	/// <param name="options">遥测选项。</param>
	public DefaultTelemetryRedactor(IOptions<TelemetryOptions> options)
	{
		_options = options.Value.Redaction;
		_keys = new HashSet<string>(_options.Keys, StringComparer.OrdinalIgnoreCase);
	}

	/// <inheritdoc />
	public bool ShouldRedact(string key)
	{
		return _options.Enabled && _keys.Contains(key);
	}

	/// <inheritdoc />
	public object? Redact(string key, object? value)
	{
		return ShouldRedact(key) ? Mask : value;
	}
}
