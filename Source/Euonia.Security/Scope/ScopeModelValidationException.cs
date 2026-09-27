namespace Nerosoft.Euonia.Security;

/// <summary>
/// 一条启动期权限模型诊断。
/// </summary>
/// <param name="ModelName">出问题的模型名；无法确定时为 <c>&lt;未知&gt;</c>。</param>
/// <param name="Message">问题描述。</param>
public sealed record ScopeModelDiagnostic(string ModelName, string Message);

/// <summary>
/// 权限模型注册期校验失败。
/// </summary>
/// <remarks>
/// <para>
/// <b>一次报出全部问题</b>，而不是修一个跑一次。启动期校验往往同时暴露多处配置错误
/// （同一个模型既漏映射维度又有死策略），逐个报出等于让人反复重启——
/// 这类「一次只能看到一个问题」的配置期校验是纯粹的时间浪费。
/// </para>
/// <para>
/// 全部诊断见 <see cref="Diagnostics"/>；消息按行汇总，便于直接贴到日志或工单里。
/// </para>
/// </remarks>
public sealed class ScopeModelValidationException : Exception
{
	/// <summary>
	/// 初始化 <see cref="ScopeModelValidationException"/> 的新实例。
	/// </summary>
	/// <param name="diagnostics">全部诊断。</param>
	public ScopeModelValidationException(IReadOnlyList<ScopeModelDiagnostic> diagnostics)
		: base(Format(diagnostics))
	{
		Diagnostics = diagnostics;
	}

	/// <summary>
	/// 获取全部诊断。
	/// </summary>
	public IReadOnlyList<ScopeModelDiagnostic> Diagnostics { get; }

	private static string Format(IReadOnlyList<ScopeModelDiagnostic> diagnostics)
	{
		if (diagnostics == null || diagnostics.Count == 0)
		{
			return "权限模型注册期校验失败。";
		}

		var lines = new List<string>(diagnostics.Count + 1)
		{
			$"权限模型注册期校验失败，共 {diagnostics.Count} 处问题："
		};

		for (var index = 0; index < diagnostics.Count; index++)
		{
			var diagnostic = diagnostics[index];
			lines.Add($"  {index + 1}. [{diagnostic.ModelName}] {diagnostic.Message}");
		}

		return string.Join(Environment.NewLine, lines);
	}
}
