using System.Security.Cryptography;

namespace System;

internal class GuidGenerator
{
    private static readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

    /// <summary>
    /// 使用指定的 GUID 类型创建新的 <see cref="Guid"/> 值。
    /// </summary>
    /// <param name="type">GUID 类型。</param>
    /// <returns>生成的 <see cref="Guid"/> 值。</returns>
    public static Guid Generate(GuidType type)
    {

        if (type == GuidType.Empty)
        {
            return Guid.Empty;
        }

        if (type == GuidType.Simple)
        {
            return Guid.NewGuid();
        }

        // 我们从 16 字节的加密强随机数据开始。
        var randomBytes = new byte[10];
        _generator.GetBytes(randomBytes);

        // 另一种方法：使用正常创建的 GUID 获取初始随机数据：
        // byte[] randomBytes = Guid.NewGuid().ToByteArray();
        // 这比使用 RNGCryptoServiceProvider 更快，但我不推荐这样做，因为 .NET Framework
        // 不保证 GUID 数据的随机性，未来版本（或不同的实现如 Mono）可能会使用不同的方法。

        // 现在我们有了 GUID 的随机基础。接下来，我们需要创建六字节块作为时间戳。

        // 我们从 DateTime.MinValue 以来经过的毫秒数开始。这将构成时间戳。
        // 由于 DateTime.Now 的精度有限，没有必要比毫秒更精确。

        // 对 48 位时间戳使用毫秒精度，可以在时间戳溢出循环之前提供约 5900 年的时间。
        // 希望这对大多数用途来说足够了。:)
        long timestamp = DateTime.UtcNow.Ticks / 10000L;

        // 然后获取字节
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);

        // 由于是从 Int64 转换，我们需要在小端系统上反转字节序。
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        byte[] guidBytes = new byte[16];

        switch (type)
        {
            case GuidType.SequentialAsString:
            case GuidType.SequentialAsBinary:

                // 对于字符串和字节数组版本，先复制时间戳，再复制随机数据。
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                // 如果格式化为字符串，我们需要补偿 .NET 将 Data1 和 Data2 块
                // 分别视为 Int32 和 Int16 的事实。这意味着它会在小端系统上交换顺序。
                // 所以我们需要再次反转。
                if (type == GuidType.SequentialAsString && BitConverter.IsLittleEndian)
                {
                    Array.Reverse(guidBytes, 0, 4);
                    Array.Reverse(guidBytes, 4, 2);
                }

                break;

            case GuidType.SequentialAtEnd:

                // 对于顺序部分在末尾的版本，先复制随机数据，再复制时间戳。
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}
