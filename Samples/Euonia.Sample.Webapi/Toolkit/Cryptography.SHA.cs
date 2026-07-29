using System.Security.Cryptography;

namespace Nerosoft.Euonia.Sample.Toolkit;

public partial class Cryptography
{
	// ReSharper disable once InconsistentNaming

	/// <summary>
	/// SHA helper for computing SHA-256 digests and returning Base64 output.
	/// </summary>
	public class SHA
	{
		/// <summary>
		/// Computes the SHA-256 digest of the specified string and returns it as Base64.
		/// </summary>
		/// <param name="source">Input string to hash.</param>
		/// <returns>Base64-encoded SHA-256 digest.</returns>
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