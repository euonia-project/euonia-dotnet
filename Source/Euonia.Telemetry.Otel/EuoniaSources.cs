using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Telemetry.Otel;

/// <summary>
/// Euonia 各组件的默认源目录；由 OTel 适配层维护，内核不感知具体组件名称。
/// </summary>
/// <remarks>
/// OTel 的源订阅在 Provider 构建时固定：自定义源必须在本方法注册前通过 <see cref="Add"/> 声明，
/// 构建之后再创建/登记的源不会被导出。
/// </remarks>
public static class EuoniaSources
{
	private static readonly ConcurrentDictionary<string, byte> _names = new(StringComparer.Ordinal)
	{
		["Nerosoft.Euonia.Bus"] = 0,
		["Nerosoft.Euonia.Bus.Http"] = 0,
		["Nerosoft.Euonia.Bus.RabbitMq"] = 0,
		["Nerosoft.Euonia.Bus.ActiveMq"] = 0,
		["Nerosoft.Euonia.Bus.Grpc"] = 0,
		["Euonia.Osba"] = 0,
		["Nerosoft.Euonia.Auditing"] = 0,
		["Nerosoft.Euonia.Tenancy"] = 0,
		["Nerosoft.Euonia.Repository"] = 0,
		["Nerosoft.Euonia.Uow"] = 0,
		["Nerosoft.Euonia.Caching"] = 0,
		["Nerosoft.Euonia.Security"] = 0,
		["Nerosoft.Euonia.Quartz"] = 0,
		["Nerosoft.Euonia.Concurrency"] = 0,
		["Nerosoft.Euonia.Hosting"] = 0,
		["Nerosoft.Euonia.Application"] = 0,
	};

	/// <summary>默认 ActivitySource / Meter 名称快照。</summary>
	public static IReadOnlyList<string> Default => _names.Keys.ToArray();

	/// <summary>
	/// 声明一个需要在 Provider 构建时订阅的自定义源。
	/// </summary>
	/// <param name="sourceName">源名称。</param>
	public static void Add(string sourceName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
		_names[sourceName] = 0;
	}
}
