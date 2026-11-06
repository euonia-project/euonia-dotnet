namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 提供 Base64 编解码能力的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.Base64</c>。
/// </summary>
public partial class Cryptography
{
	/// <summary>
	/// 为 UTF8 字符串提供 Base64 编码与解码的辅助类。
	/// </summary>
	public class Base64
	{
		/// <summary>
		/// 使用 UTF8 编码把指定字符串编码为 Base64。
		/// </summary>
		/// <param name="source">要编码的字符串。</param>
		/// <returns>Base64 编码后的表示。</returns>
		public static string Encode(string source)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
		}

		/// <summary>
		/// 使用 UTF8 编码解码指定的 Base64 编码字符串。
		/// </summary>
		/// <param name="source">Base64 编码的字符串。</param>
		/// <returns>解码后的 UTF8 字符串。</returns>
		public static string Decode(string source)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(source));
		}
	}
}