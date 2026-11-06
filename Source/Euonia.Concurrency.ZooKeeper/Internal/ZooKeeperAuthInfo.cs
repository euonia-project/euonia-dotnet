using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// ZooKeeper 连接使用的身份验证信息。
/// </summary>
/// <param name="Scheme">身份验证方案名称，例如 <c>digest</c>。</param>
/// <param name="Auth">身份验证凭据的原始字节。</param>
internal sealed record ZooKeeperAuthInfo(string Scheme, EquatableReadOnlyList<byte> Auth);