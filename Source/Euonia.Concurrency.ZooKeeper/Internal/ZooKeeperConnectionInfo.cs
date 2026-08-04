using Nerosoft.Euonia.Collections;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

internal sealed record ZooKeeperConnectionInfo(string ConnectionString, TimeoutValue ConnectTimeout, TimeoutValue SessionTimeout, EquatableReadOnlyList<ZooKeeperAuthInfo> AuthInfo);