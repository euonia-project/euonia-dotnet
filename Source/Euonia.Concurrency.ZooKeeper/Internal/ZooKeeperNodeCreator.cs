using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// 负责创建 ZooKeeper 节点，并在失败时回滚本次调用所创建的目录。
/// </summary>
internal static class ZooKeeperNodeCreator
{
    /// <summary>
    /// 详见 https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html 中的“Builtin ACL Schemes”。
    /// </summary>
    public static readonly ACL PublicAcl = new(0x1f, new Id("world", "anyone"));

    /// <summary>
    /// 在指定目录下创建一个顺序临时节点，可选择先确保目录存在。
    /// </summary>
    /// <param name="connection">用于创建节点的 ZooKeeper 连接。</param>
    /// <param name="directory">节点的父目录路径。</param>
    /// <param name="namePrefix">顺序节点的名称前缀。</param>
    /// <param name="aclEnumerable">创建节点时使用的访问控制列表。</param>
    /// <param name="ensureDirectoryExists">是否在创建节点前确保父目录存在；为 <see langword="false"/> 时要求目录已存在。</param>
    /// <returns>实际创建的顺序临时节点路径，由服务端追加序列号。</returns>
    /// <exception cref="InvalidOperationException">父目录不存在且未要求自动创建，或创建过程中遇到未预期的无节点错误。</exception>
    public static async Task<string> CreateEphemeralSequentialNode(
        this ZooKeeperConnection connection,
        ZooKeeperPath directory,
        string namePrefix,
        IEnumerable<ACL> aclEnumerable,
        bool ensureDirectoryExists)
    {
        // 如果我们负责确保目录存在，此算法会持续循环，直到 EnsureDirectory 失败或遇到除目录
        // 不存在以外的其他错误。它支持并发的节点创建与目录创建，以及节点创建与目录删除。

        var acl = aclEnumerable.ToList();
        while (true)
        {
            var createdDirectories = ensureDirectoryExists
                ? await EnsureDirectoryAndGetCreatedPathsAsync().ConfigureAwait(false)
                : Array.Empty<ZooKeeperPath>();

            try
            {
                return await connection.ZooKeeper.createAsync($"{directory}{ZooKeeperPath.Separator}{namePrefix}", data: Array.Empty<byte>(), acl, CreateMode.EPHEMERAL_SEQUENTIAL).ConfigureAwait(false);
            }
            catch (KeeperException.NoNodeException ex)
            {
                // 如果我们不负责确保目录存在，则重新抛出更具帮助性的错误消息。否则，
                // 吞掉该错误并再次进入循环。
                if (!ensureDirectoryExists)
                {
                    throw new InvalidOperationException(string.Format(Resources.IDS_NODE_DOES_NOT_EXIST, directory), ex);
                }
            }
            catch
            {
                // 发生未处理的错误时，清理我们创建的所有目录
                await TryCleanUpCreatedDirectoriesAsync(createdDirectories).ConfigureAwait(false);

                throw;
            }
        }

        async Task<IReadOnlyList<ZooKeeperPath>> EnsureDirectoryAndGetCreatedPathsAsync()
        {
            // 此算法会持续循环，直到目录存在，或创建尝试因 NoNodeException、
            // NodeExistsException 之外的原因失败。它支持并发的目录创建，也支持通过乐观并发
            // 进行的并发创建与删除

            var toCreate = new Stack<ZooKeeperPath>();
            toCreate.Push(directory);
            List<ZooKeeperPath>? created = null;
            do
            {
                var directoryToCreate = toCreate.Peek();
                if (directoryToCreate == ZooKeeperPath.Root)
                {
                    throw new InvalidOperationException(string.Format(Resources.IDS_UNEXPECTED_NO_NODE_WHEN_CREATING_CHILD, typeof(KeeperException.NoNodeException), ZooKeeperPath.Root));
                }

                try
                {
                    await connection.ZooKeeper.createAsync(directoryToCreate.ToString(), data: Array.Empty<byte>(), acl, CreateMode.PERSISTENT).ConfigureAwait(false);
                    toCreate.Pop();
                    (created ??= new List<ZooKeeperPath>()).Add(directoryToCreate);
                }
                catch (KeeperException.NodeExistsException) // 其他调用方已创建该节点
                {
                    toCreate.Pop();
                }
                catch (KeeperException.NoNodeException) // 需要先创建父节点
                {
                    toCreate.Push(directoryToCreate.GetDirectory()!.Value);
                }
                catch
                {
                    // 发生未处理的失败时，尝试清理我们的工作
                    if (created != null)
                    {
                        await TryCleanUpCreatedDirectoriesAsync(created).ConfigureAwait(false);
                    }

                    throw;
                }
            }
            while (toCreate.Count != 0);

            return created ?? (IReadOnlyList<ZooKeeperPath>)Array.Empty<ZooKeeperPath>();
        }

        async Task TryCleanUpCreatedDirectoriesAsync(IReadOnlyList<ZooKeeperPath> createdDirectories)
        {
            try
            {
                // 按创建顺序的逆序删除
                for (var i = createdDirectories.Count - 1; i >= 0; --i)
                {
                    await connection.ZooKeeper.deleteAsync(createdDirectories[i].ToString()).ConfigureAwait(false);
                }
            }
            catch
            {
                // 吞掉错误，因为此清理很可能以与创建时相同的方式失败
            }
        }
    }
}