using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

internal sealed record ZooKeeperAuthInfo(string Scheme, EquatableReadOnlyList<byte> Auth);