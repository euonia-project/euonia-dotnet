using Nerosoft.Euonia.Collections;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// 描述建立 ZooKeeper 连接所需的连接串、超时与会话认证信息，并作为连接池的缓存键使用。
/// </summary>
/// <param name="ConnectionString">ZooKeeper 连接串。</param>
/// <param name="ConnectTimeout">等待连接建立的最长时间。</param>
/// <param name="SessionTimeout">ZooKeeper 会话超时时间。</param>
/// <param name="AuthInfo">连接使用的身份验证信息集合。</param>
internal sealed record ZooKeeperConnectionInfo(string ConnectionString, TimeoutValue ConnectTimeout, TimeoutValue SessionTimeout, EquatableReadOnlyList<ZooKeeperAuthInfo> AuthInfo);