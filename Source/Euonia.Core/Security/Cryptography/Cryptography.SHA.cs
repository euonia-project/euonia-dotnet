using System.Security.Cryptography;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// SHA-256 摘要辅助类。
	/// </summary>
	/// <remarks>
	/// 摘要结果为 SHA-256，并以 Base64 字符串返回。
	/// </remarks>
	public static class SHA
	{
		/// <summary>
		/// 计算指定字符串的 SHA-256 摘要，并以 Base64 形式返回。
		/// </summary>
		/// <param name="source">要计算摘要的输入字符串。</param>
		/// <returns>Base64 编码的 SHA-256 摘要。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
		public static string Encrypt(string source)
		{
			ArgumentNullException.ThrowIfNull(source);

			var bytes = Encoding.UTF8.GetBytes(source);
			var output = SHA256.HashData(bytes);

			return Convert.ToBase64String(output);
		}
	}
}
