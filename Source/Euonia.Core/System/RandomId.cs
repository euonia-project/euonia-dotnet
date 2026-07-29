namespace System;

/// <summary>
/// 生成随机 ID 的工具类。
/// </summary>
internal class RandomId
{
    private static readonly string[] _chars =
    [
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
    ];

    private static string GenerateKey()
    {
        var seek = unchecked((int)DateTime.UtcNow.Ticks);

        var random = new Random(seek);

        for (var i = 0; i < 100000; i++)
        {
            var number = random.Next(1, _chars.Length);
            (_chars[0], _chars[number - 1]) = (_chars[number - 1], _chars[0]);
        }

        return string.Join(string.Empty, _chars);
    }

    /// <summary>
    /// 根据提供的种子生成随机 ID。
    /// </summary>
    /// <param name="seed">随机数种子。</param>
    /// <returns>生成的随机 ID 字符串。</returns>
    public static string Generate(long seed)
    {
        var key = GenerateKey();

        return Mixup(key, seed);
    }

    private static string Convert(string key, long value)
    {
        if (value < 62)
        {
            return key[(int)value].ToString();
        }

        var y = (int)(value % 62);
        var x = value / 62;
        return Convert(key, x) + key[y];
    }

    private static string Mixup(string key, long value)
    {
        var sequence = Convert(key, value);
        var salt = sequence.Aggregate(0, (current, seq) => current + seq);

        var x = salt % sequence.Length;

        var original = sequence.ToCharArray();
        var source = new char[original.Length];
        Array.Copy(original, x, source, 0, sequence.Length - x);
        Array.Copy(original, 0, source, sequence.Length - x, x);
        return source.Aggregate(string.Empty, ((current, @char) => current + @char));
    }
}
