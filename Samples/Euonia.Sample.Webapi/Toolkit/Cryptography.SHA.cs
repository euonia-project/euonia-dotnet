using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

/// <summary>
/// 提供 SHA-256 摘要计算能力的 <see cref="Cryptography"/> 分部声明，实际实现位于嵌套类型 <c>Cryptography.SHA</c>。
/// </summary>
public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// 用于计算 SHA-256 摘要并以 Base64 返回的 SHA 辅助类。
	/// </summary>
	public class SHA
	{
		/// <summary>
		/// 计算指定字符串的 SHA-256 摘要并以 Base64 返回。
		/// </summary>
		/// <param name="source">要哈希的输入字符串。</param>
		/// <returns>Base64 编码的 SHA-256 摘要。</returns>
		public static string Encrypt(string source)
		{
			var bytes = Encoding.UTF8.GetBytes(source);

#if NETSTANDARD2_1
			byte[] output;
			using (var sha = SHA256.Create())
			{
				output = sha.ComputeHash(bytes);
			}
#else
			var output = SHA256.HashData(bytes);
#endif
			return Convert.ToBase64String(output);
		}
	}
}