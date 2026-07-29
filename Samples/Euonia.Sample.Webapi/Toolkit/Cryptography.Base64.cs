namespace Nerosoft.Euonia.Sample.Toolkit;

public partial class Cryptography
{
	/// <summary>
	/// Provides Base64 encode and decode helpers for UTF8 strings.
	/// </summary>
	public class Base64
	{
		/// <summary>
		/// Encodes the specified string to Base64 using UTF8 encoding.
		/// </summary>
		/// <param name="source">String to encode.</param>
		/// <returns>Base64-encoded representation.</returns>
		public static string Encode(string source)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
		}

		/// <summary>
		/// Decodes the specified Base64-encoded string using UTF8 encoding.
		/// </summary>
		/// <param name="source">Base64-encoded string.</param>
		/// <returns>Decoded UTF8 string.</returns>
		public static string Decode(string source)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(source));
		}
	}
}