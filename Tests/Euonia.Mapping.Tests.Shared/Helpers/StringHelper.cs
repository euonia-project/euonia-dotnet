namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 提供测试映射配置使用的字符串拼接辅助方法。
/// </summary>
public class StringHelper
{
	/// <summary>
	/// 将给定字符串以空格分隔拼接为单个字符串。
	/// </summary>
	/// <param name="values">待拼接的字符串序列。</param>
	/// <returns>以空格分隔拼接后的字符串。</returns>
	public string Combine(params string[] values)
	{
		return string.Join(" ", values);
	}
}