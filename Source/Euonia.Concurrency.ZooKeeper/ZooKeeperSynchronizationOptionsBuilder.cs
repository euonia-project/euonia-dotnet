using Nerosoft.Euonia.Collections;
using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;
using org.apache.zookeeper.data;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 用于配置基于 ZooKeeper 的同步原语的选项
/// </summary>
public sealed class ZooKeeperSynchronizationOptionsBuilder
{
    private static readonly IReadOnlyList<ACL> _defaultAcl = new[] { ZooKeeperNodeCreator.PublicAcl };

    /// <summary>
    /// 根据 https://bowenli86.github.io/2016/09/15/distributed%20system/zookeeper/ZooKeeper-Sessions-and-Session-Management/，
    /// 超时最短可为 tick 时间的 2 倍，最长可为 tick 时间的 20 倍。默认 tick 时间为 2 秒，因此此默认值被设置为既足够大、
    /// 只需相对较少的心跳，又足够小，在默认情况下可支持 2 秒或 1 秒的 tick 时间。
    /// </summary>
    private TimeoutValue _sessionTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// 默认值（任意选取）与 SQL Server 的默认连接超时一致
    /// </summary>
    private TimeoutValue _connectTimeout = TimeSpan.FromSeconds(15);

    private readonly List<ZooKeeperAuthInfo> _authInfo = new();
    private readonly List<ACL> _acl = new();

    private ZooKeeperSynchronizationOptionsBuilder()
    {
    }

    /// <summary>
    /// 配置连接到 ZooKeeper 的 <paramref name="sessionTimeout"/>。由于底层 ZooKeeper 客户端会定期续订会话，
    /// 该值通常不会影响行为。较小的值意味着在持有锁的进程崩溃后锁会被更快地释放，但也会增加临时连接问题导致
    /// 锁丢失的风险。
    /// 
    /// 默认为 20 秒。
    /// </summary>
    public ZooKeeperSynchronizationOptionsBuilder SessionTimeout(TimeSpan sessionTimeout)
    {
        var sessionTimeoutValue = new TimeoutValue(sessionTimeout);
        if (sessionTimeoutValue.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionTimeout), ThreadingResources.IDS_VALUE_MUST_BE_POSITIVE);
        }

        if (sessionTimeoutValue.IsInfinite)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionTimeout), ThreadingResources.IDS_CAN_NOT_BE_INFINITE);
        }

        _sessionTimeout = sessionTimeoutValue;
        return this;
    }

    /// <summary>
    /// 配置在因 <see cref="TimeoutException"/> 失败之前，等待与 ZooKeeper 建立连接的时间。
    /// 
    /// 默认为 15 秒。
    /// </summary>
    public ZooKeeperSynchronizationOptionsBuilder ConnectTimeout(TimeSpan connectTimeout)
    {
        _connectTimeout = new TimeoutValue(connectTimeout);
        return this;
    }

    /// <summary>
    /// 指定通过 <see cref="org.apache.zookeeper.ZooKeeper.addAuthInfo"/> 添加到 ZooKeeper 连接的身份验证信息。
    /// 每次调用此方法都会向身份验证信息列表中添加一个条目。有关 ZooKeeper 身份验证的更多信息，请参见
    /// https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html。
    /// 
    /// 默认不添加任何身份验证信息。
    /// </summary>
    public ZooKeeperSynchronizationOptionsBuilder AddAuthInfo(string scheme, IReadOnlyList<byte> auth)
    {
        _authInfo.Add(new ZooKeeperAuthInfo(
            scheme ?? throw new ArgumentNullException(nameof(scheme)),
            new EquatableReadOnlyList<byte>(auth ?? throw new ArgumentNullException(nameof(auth)))
        ));
        return this;
    }

    /// <summary>
    /// 配置所创建 ZooKeeper 节点的访问控制列表（ACL）。每次调用此方法都会向访问控制列表中添加一个条目。
    /// 有关 ZooKeeper ACL 的更多信息，请参见 https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html。
    /// 
    /// 如果未指定任何 ACL 条目，则使用的 ACL 是一个授予 (world, anyone) 所有权限的单元素列表。
    /// </summary>
    public ZooKeeperSynchronizationOptionsBuilder AddAccessControl(string scheme, string id, int permissionFlags)
    {
        _acl.Add(new ACL(permissionFlags, new Id(scheme ?? throw new ArgumentNullException(nameof(scheme)), id ?? throw new ArgumentNullException(nameof(id)))));
        return this;
    }

    /// <summary>
    /// 应用调用方提供的配置委托，返回包含会话超时、连接超时、认证信息与访问控制列表的选项元组。
    /// </summary>
    /// <param name="options">用于设置选项的委托；为 <see langword="null"/> 时全部采用默认值。</param>
    /// <returns>由会话超时、连接超时、认证信息与访问控制列表组成的选项。</returns>
    internal static (TimeoutValue SessionTimeout, TimeoutValue ConnectTimeout, EquatableReadOnlyList<ZooKeeperAuthInfo> AuthInfo, IReadOnlyList<ACL> Acl) GetOptions(Action<ZooKeeperSynchronizationOptionsBuilder>? options)
    {
        var builder = new ZooKeeperSynchronizationOptionsBuilder();
        options?.Invoke(builder);
        return (
            SessionTimeout: builder._sessionTimeout,
            ConnectTimeout: builder._connectTimeout,
            AuthInfo: new EquatableReadOnlyList<ZooKeeperAuthInfo>(builder._authInfo),
            Acl: builder._acl.Count != 0 ? builder._acl.ToArray() : _defaultAcl
        );
    }
}