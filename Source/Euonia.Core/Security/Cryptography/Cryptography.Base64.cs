namespace Nerosoft.Euonia.Security;

public static partial class Cryptography
{
	/// <summary>
	/// 提供 UTF-8 字符串与 Base64 之间的编码与解码。
	/// </summary>
	public static class Base64
	{
		/// <summary>
		/// 使用 UTF-8 将指定字符串编码为 Base64。
		/// </summary>
		/// <param name="source">要编码的字符串。</param>
		/// <returns>Base64 编码后的表示。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
		public static string Encode(string source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
		}

		/// <summary>
		/// 将指定的 Base64 字符串按 UTF-8 解码。
		/// </summary>
		/// <param name="source">Base64 编码的字符串。</param>
		/// <returns>解码后的 UTF-8 字符串。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
		/// <exception cref="FormatException"><paramref name="source"/> 不是合法的 Base64。</exception>
		public static string Decode(string source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return Encoding.UTF8.GetString(Convert.FromBase64String(source));
		}
	}
}
