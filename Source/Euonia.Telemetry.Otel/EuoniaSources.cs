namespace Nerosoft.Euonia.Telemetry.Otel;

/// <summary>
/// Euonia 各组件的默认源目录；由 OTel 适配层维护，内核不感知具体组件名称。
/// </summary>
public static class EuoniaSources
{
	/// <summary>默认 ActivitySource / Meter 名称。</summary>
	public static IReadOnlyList<string> Default { get; } =
	[
		"Nerosoft.Euonia.Bus",
		"Nerosoft.Euonia.Bus.Http",
		"Nerosoft.Euonia.Bus.RabbitMq",
		"Nerosoft.Euonia.Bus.ActiveMq",
		"Nerosoft.Euonia.Bus.Grpc",
		"Euonia.Osba",
		"Nerosoft.Euonia.Auditing",
		"Nerosoft.Euonia.Tenancy",
		"Nerosoft.Euonia.Repository",
		"Nerosoft.Euonia.Uow",
		"Nerosoft.Euonia.Caching",
		"Nerosoft.Euonia.Security",
		"Nerosoft.Euonia.Quartz",
		"Nerosoft.Euonia.Concurrency",
		"Nerosoft.Euonia.Hosting",
		"Nerosoft.Euonia.Application",
	];
}
