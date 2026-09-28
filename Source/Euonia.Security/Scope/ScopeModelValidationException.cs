namespace Nerosoft.Euonia.Security;

/// <summary>
/// 单条权限模型配置诊断。
/// </summary>
/// <param name="ModelName">模型类型名。</param>
/// <param name="Message">诊断信息。</param>
public sealed record ScopeModelDiagnostic(string ModelName, string Message);

/// <summary>
/// 权限模型注册期校验失败，携带全部诊断以便一次定位所有问题。
/// </summary>
public sealed class ScopeModelValidationException : Exception
{
	public ScopeModelValidationException(IReadOnlyList<ScopeModelDiagnostic> diagnostics)
		: base(Format(diagnostics))
	{
		Diagnostics = diagnostics;
	}

	/// <summary>
	/// 全部诊断。
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
