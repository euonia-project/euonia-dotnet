using System.Security.Cryptography;

namespace Nerosoft.Euonia.Concurrency.FileSystem;

/// <summary>
/// 用于验证文件名并将锁名称转换为有效文件名的帮助类。
/// </summary>
internal static class FileNameValidationHelper
{
    // 注意：我们的目标是尽可能确保跨平台行为一致，以防有人锁定的是网络文件系统
    // 上的文件。
    //
    // 这意味着我们只使用字母、数字和下划线作为名称字符，并且始终包含一个哈希部分
    // （这可以避开 Windows 上各种“特殊”文件名）。
    //
    // 长度是另一个需要考虑的因素；在 Windows 上，文件名最大长度为 255 个字符，路径最大长度
    // 为 259 个字符或 32000 个字符，具体取决于是否启用长路径以及运行在 .NET Core 还是 .NET Framework 上。在 Unix 上，
    // 我们预期文件名上限为 255 字节，路径上限为 4096 字节。
    // 
    // 另一个差异是大小写敏感性：Windows 上文件名不区分大小写，但我们希望锁名称区分大小写。
    //
    // 为了可移植性，我们倾向于使用固定长度为 64 个字符的名称，形式为 [clean base name prefix][hash].lock。“clean base name prefix”
    // 是原始名称的前缀，其中的非字母/数字/下划线字符会被替换为下划线。该前缀尽可能长，但不超过
    // 整体名称限制。“hash” 是所提供名称的 UTF8 字节的 Base 32 哈希。这样我们既能获得大小写敏感性，又能在名称于“清理”过程中
    // 被截断或改动时将碰撞概率降到最低。最后，“.lock” 扩展名是一个有用的标识；它和名称
    // 前缀都是为了便于调试。
    //
    // 有时，我们的基础目录会太长，以至于无法使用这种完整的可移植名称格式。此时，我们退回到只使用哈希部分。
    // 如果仍然太长，则退回到只使用哈希的前 12 个字符。如果还是太长，就放弃。

    // 如此选择是因为再短的话碰撞风险就显得太高了。如果有人选择的基础目录长到
    // 使名称被限制在 < 12 个字符，那看起来就像是个错误
    private const int MIN_FILE_NAME_LENGTH = 12;

    // 如此选择是因为它既小于 Windows 允许的 255 个字符，也（由于我们始终包含 Base 32 哈希）小于
    // Unix 允许的 255 字节。希望它也足够短，即使在 Windows 上也很少超过 MAX_PATH
    private const int PORTABLE_FILE_NAME_LENGTH = 64;

    /// <summary>
    /// 根据锁名称生成可移植的锁文件完整路径，并在路径过长时逐级退化为更短的名称形式。
    /// </summary>
    /// <param name="lockFileDirectory">存放锁文件的目录。</param>
    /// <param name="name">锁名称，将参与生成基础名与哈希部分。</param>
    /// <returns>由目录、清理后的基础名前缀、Base32 哈希与 <c>.lock</c> 扩展名组成的锁文件路径。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lockFileDirectory"/> 或 <paramref name="name"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="PathTooLongException">即使退化为最短的哈希前缀形式，路径仍然过长。</exception>
    public static string GetLockFileName(DirectoryInfo lockFileDirectory, string name)
    {
		ArgumentNullException.ThrowIfNull(lockFileDirectory);
		ArgumentNullException.ThrowIfNull(name);
        
        var directoryPath = lockFileDirectory.FullName;
        var directoryPathWithTrailingSeparator = directoryPath[^1] == Path.DirectorySeparatorChar
            ? directoryPath
            : directoryPath + Path.DirectorySeparatorChar;

        var baseName = ConvertToValidBaseName(name);
        var nameHash = ComputeHash(Encoding.UTF8.GetBytes(name));
        const string extension = ".lock";

        // 首先，尝试完整的可移植名称格式
        var portableLockFileName = directoryPathWithTrailingSeparator
                                   + baseName[..Math.Min(PORTABLE_FILE_NAME_LENGTH - nameHash.Length - extension.Length, baseName.Length)]
                                   + nameHash
                                   + extension;
        if (!IsTooLong(portableLockFileName))
        {
            return portableLockFileName;
        }

        // 接下来，尝试仅使用哈希作为名称
        var hashOnlyFileName = directoryPathWithTrailingSeparator + nameHash;
        if (!IsTooLong(hashOnlyFileName))
        {
            return hashOnlyFileName;
        }

        // 最后，尝试仅使用哈希的一部分
        var minimumLengthFileName = directoryPathWithTrailingSeparator + nameHash[..MIN_FILE_NAME_LENGTH];
        if (!IsTooLong(minimumLengthFileName))
        {
            return minimumLengthFileName;
        }

        throw new PathTooLongException(string.Format(Resources.IDS_LOCK_FILE_NAME_TOO_LONG, directoryPathWithTrailingSeparator.Length, MIN_FILE_NAME_LENGTH));
    }

    private static bool IsTooLong(string name)
    {
        try
        {
            Path.GetFullPath(name);
            return false;
        }
        catch (PathTooLongException)
        {
            return true;
        }
    }

    private static string ConvertToValidBaseName(string name)
    {
        const char replacementChar = '_';

        StringBuilder? builder = null;
        for (var i = 0; i < name.Length; ++i)
        {
            var @char = name[i];
            if (!char.IsLetterOrDigit(@char) && @char != replacementChar)
            {
                builder ??= new StringBuilder(name.Length).Append(name, startIndex: 0, count: i);
                builder.Append(replacementChar);
            }
            else if (builder != null)
            {
                builder.Append(@char);
            }
        }

        return builder?.ToString() ?? name;
    }

    // 我们截断到 160 位，即 32 个 Base32 字符。这仍然能提供良好的抗碰撞性，同时允许 64 个字符的
    // 名称包含原始提供名称的很大一部分，这对调试很有帮助。参见
    // https://crypto.stackexchange.com/questions/9435/is-truncating-a-sha512-hash-to-the-first-160-bits-as-secure-as-using-sha1#:~:text=Yes.,time%20is%20still%20pretty%20big
    private const int BASE32_CHAR_BITS = 5;

    /// <summary>
    /// 锁名哈希部分使用的 Base32 字符数，对应截断到 160 位的 SHA-512 摘要。
    /// </summary>
    internal const int HashLengthInChars = 160 / BASE32_CHAR_BITS;

    private static string ComputeHash(byte[] bytes)
    {
        using var sha = SHA512.Create();
        var hashBytes = sha.ComputeHash(bytes);

        // 我们使用 Base32，因为它不区分大小写（这对 Windows 文件很重要），而且比 Base16 更紧凑
        // 来自 https://en.wikipedia.org/wiki/Base32 的 RFC 4648
        const string base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var chars = new char[HashLengthInChars];
        var byteIndex = 0;
        var bitBuffer = 0;
        var bitsRemaining = 0;
        for (var charIndex = 0; charIndex < chars.Length; ++charIndex)
        {
            if (bitsRemaining < BASE32_CHAR_BITS)
            {
                bitBuffer |= hashBytes[byteIndex++] << bitsRemaining;
                bitsRemaining += 8;
            }

            chars[charIndex] = base32Alphabet[bitBuffer & 31];
            bitBuffer >>= BASE32_CHAR_BITS;
            bitsRemaining -= BASE32_CHAR_BITS;
        }

        return new string(chars);
    }
}