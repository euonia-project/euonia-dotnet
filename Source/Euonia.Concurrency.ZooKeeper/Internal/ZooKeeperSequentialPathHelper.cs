using System.Globalization;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// 顺序节点辅助方法：按前缀筛选子节点并按从旧到新排序，以及查询节点的创建时间。
/// </summary>
internal static class ZooKeeperSequentialPathHelper
{
    /// <summary>
    /// 在给定一组子节点名称（<paramref name="childrenNames"/>）的情况下，筛选出仅带有前缀 <paramref name="prefix"/>
    /// 或 <paramref name="alternatePrefix"/> 的顺序节点。
    /// 
    /// 然后，按从最旧到最新的顺序对节点排序。大多数情况下，仅使用序列号即可完成排序。但由于序列号会在
    /// <see cref="int.MaxValue"/> 处回绕，某些场景下需要更复杂的逻辑才能得到正确的排序结果。
    /// </summary>
    public static async ValueTask<(string Path, int SequenceNumber, string Prefix)[]> FilterAndSortAsync(
        string parentNode,
        IEnumerable<string> childrenNames,
        Func<string, Task<long?>> getNodeCreationTimeAsync,
        string prefix,
        string? alternatePrefix = null)
    {
        var ephemeralChildrenWithPrefix = GetEphemeralChildrenWithPrefix();

        if (ephemeralChildrenWithPrefix.Count == 0)
        {
            return Array.Empty<(string, int, string)>();
        }

        // 首先，按无符号值排序
        ephemeralChildrenWithPrefix.Sort((a, b) => a.UnsignedSequenceNumber.CompareTo(b.UnsignedSequenceNumber));

        // 接下来，测量每对相邻序列值之间的间隔，以判断断点是否明显

        // 该值大于 uint 空间的 90%；这样的间隔只可能出现一个，如果存在这样的间隔，
        // 就可以安全地认为该间隔标识了序列的结束
        const uint largeGapSize = 4_000_000_000u;

        var maxGap = 0u;
        var maxGapEndIndex = -1;
        for (var i = 0; i < ephemeralChildrenWithPrefix.Count; ++i)
        {
            var gapEndIndex = (i + 1) % ephemeralChildrenWithPrefix.Count;
            var gap = unchecked(ephemeralChildrenWithPrefix[gapEndIndex].UnsignedSequenceNumber - ephemeralChildrenWithPrefix[i].UnsignedSequenceNumber);
            if (gap > maxGap)
            {
                maxGap = gap;
                maxGapEndIndex = gapEndIndex;
            }
        }

        if (maxGap >= largeGapSize)
        {
            return ReorderByLowestIndex(maxGapEndIndex);
        }

        // 最后，退回到通过创建时间来确定起始点
        var creationTimeTasksByChildPath = ephemeralChildrenWithPrefix.ToDictionary(t => t.Path, t => getNodeCreationTimeAsync(t.Path));
        await Task.WhenAll(creationTimeTasksByChildPath.Values).ConfigureAwait(false);

        ephemeralChildrenWithPrefix.RemoveAll(t => creationTimeTasksByChildPath[t.Path].Result == null); // 移除所有没有创建时间的节点（它们已不存在）
        if (ephemeralChildrenWithPrefix.Count == 0)
        {
            return Array.Empty<(string, int, string)>();
        }

        var oldestChild = ephemeralChildrenWithPrefix.Select((t, index) => (creationTime: creationTimeTasksByChildPath[t.Path].Result!.Value, index))
                                                     .OrderBy(t => t.creationTime)
                                                     .ThenBy(t => t.index)
                                                     .First();
        return ReorderByLowestIndex(oldestChild.index);

        List<(string Path, uint UnsignedSequenceNumber, string Prefix)> GetEphemeralChildrenWithPrefix()
        {
            var result = new List<(string Path, uint UnsignedSequenceNumber, string Prefix)>();
            foreach (var childName in childrenNames)
            {
                int? childSequenceNumber;
                string? childPrefix;
                if (GetSequenceNumberOrDefault(childName, prefix) is { } prefixSequenceNumber)
                {
                    childSequenceNumber = prefixSequenceNumber;
                    childPrefix = prefix;
                }
                else if (alternatePrefix != null
                         && GetSequenceNumberOrDefault(childName, alternatePrefix) is { } alternatePrefixSequenceNumber)
                {
                    childSequenceNumber = alternatePrefixSequenceNumber;
                    childPrefix = alternatePrefix;
                }
                else
                {
                    childSequenceNumber = null;
                    childPrefix = null;
                }

                if (childPrefix != null)
                {
                    result.Add(($"{parentNode.TrimEnd(ZooKeeperPath.Separator)}/{childName}", unchecked((uint)childSequenceNumber!.Value), childPrefix));
                }
            }

            return result;
        }

        (string Path, int SequenceNumber, string Prefix)[] ReorderByLowestIndex(int lowestIndex)
        {
            var result = new (string Path, int SequenceNumber, string Prefix)[ephemeralChildrenWithPrefix.Count];
            for (var i = 0; i < result.Length; ++i)
            {
                var element = ephemeralChildrenWithPrefix[(i + lowestIndex) % result.Length];
                result[i] = (element.Path, unchecked((int)element.UnsignedSequenceNumber), element.Prefix);
            }

            return result;
        }
    }

    /// <summary>
    /// 如果 <paramref name="pathOrName"/> 的形式为 [.../]prefix{序列号}，则返回序列号。否则返回 null。
    /// </summary>
    internal static int? GetSequenceNumberOrDefault(string pathOrName, string prefix)
    {
        Invariant.Require(prefix.Length > 0);

        // 处理子路径名称时，此处应为 -1；这意味着我们期望前缀位于开头
        var prefixStartIndex = pathOrName.LastIndexOf(ZooKeeperPath.Separator) + 1;
        if (pathOrName.IndexOf(prefix, prefixStartIndex, StringComparison.OrdinalIgnoreCase) != prefixStartIndex)
        {
            return null;
        }

        // 摘自 https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html#Sequence+Nodes+--+Unique+Naming
        // “计数器格式为 %010d，即 10 位数字并以 0（零）填充（计数采用这种格式是为了便于排序），
        // 例如 "<path>0000000001"。有关此功能的用法示例，请参见 Queue Recipe。注意：用于存储下一个序列号的计数器
        // 是由父节点维护的有符号 int（4 字节），当递增超过 2147483647 时计数器将溢出（从而产生名称 "<path>-2147483648"）。”
        var counterSuffix = pathOrName[(prefixStartIndex + prefix.Length)..];
        return (
                   (counterSuffix.Length == 10 && counterSuffix[0] != '+') // 10 位数字；不允许前导 +
                   || (counterSuffix.Length == 11 && counterSuffix[0] == '-') // 11 位数字必须以 - 开头，后跟 10 位数字
               )
               && int.TryParse(counterSuffix, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var sequenceNumber)
            ? sequenceNumber
            : default(int?);
    }

    /// <summary>
    /// 获取指定节点的创建时间。
    /// </summary>
    /// <param name="connection">用于查询的 ZooKeeper 连接。</param>
    /// <param name="path">要查询的节点路径。</param>
    /// <returns>节点创建时间的毫秒时间戳；节点不存在时为 <see langword="null"/>。</returns>
    public static async Task<long?> GetNodeCreationTimeAsync(this ZooKeeperConnection connection, string path) =>
        (await connection.ZooKeeper.existsAsync(path).ConfigureAwait(false))?.getCtime();
}