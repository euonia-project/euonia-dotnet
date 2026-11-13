namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 提供 MD5 摘要计算能力的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.MD5</c>。
/// </summary>
public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 用于计算 MD5 摘要的 MD5 辅助类。
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public class MD5
	{
		/// <summary>
		/// 计算指定字符串的 MD5 哈希值，并返回小写十六进制表示。
		/// </summary>
		/// <param name="value">要计算哈希的输入字符串。</param>
		/// <returns>小写十六进制的 MD5 摘要。</returns>
		public static string Encrypt(string value)
		{
			var bytes = System.Security.Cryptography.MD5.HashData(Encoding.Default.GetBytes(value));
			var builder = new StringBuilder();
			foreach (var @byte in bytes)
			{
				builder.Append(@byte.ToString("x").PadLeft(2, '0'));
			}

			return builder.ToString();
		}
	}
}