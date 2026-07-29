namespace Nerosoft.Euonia.Sample.Toolkit;

public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// MD5 helper for computing MD5 digests.
	/// </summary>
	// ReSharper disable once InconsistentNaming

	public class MD5
	{
		/// <summary>
		/// Computes the MD5 hash of the specified string and returns the lowercase hexadecimal representation.
		/// </summary>
		/// <param name="value">Input string to hash.</param>
		/// <returns>Lowercase hexadecimal MD5 digest.</returns>
		public static string Encrypt(string value)
		{
#if NETSTANDARD2_1
			using var md5 = System.Security.Cryptography.MD5.Create();
			var bytes = md5.ComputeHash(Encoding.Default.GetBytes(value));
#elif NET6_0_OR_GREATER
			var bytes = System.Security.Cryptography.MD5.HashData(Encoding.Default.GetBytes(value));
#endif
			var builder = new StringBuilder();
			foreach (var @byte in bytes)
			{
				builder.Append(@byte.ToString("x").PadLeft(2, '0'));
			}

			return builder.ToString();
		}
	}
}