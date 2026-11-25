using System.Text;

namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// 用于计算 MD5 摘要的辅助类。
	/// </summary>
	/// <remarks>
	/// MD5 不具备抗碰撞性，仅用于兼容既有摘要；新数据请使用 SHA-256。
	/// </remarks>
	public static class MD5
	{
		/// <summary>
		/// 计算指定字符串的 MD5 摘要并返回小写十六进制表示。
		/// </summary>
		/// <param name="value">要计算摘要的输入字符串。</param>
		/// <returns>小写十六进制形式的 MD5 摘要。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <see langword="null"/>。</exception>
		public static string Encrypt(string value)
		{
			ArgumentNullException.ThrowIfNull(value);

			var bytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(value));
			var builder = new StringBuilder(bytes.Length * 2);

			foreach (var @byte in bytes)
			{
				builder.Append(@byte.ToString("x2"));
			}

			return builder.ToString();
		}
	}
}
