namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供 AES、DES、SHA、MD5、Base64 与 RSA 等密码学辅助实现。
/// </summary>
/// <remarks>
/// <see cref="AES"/> 为兼容既有数据使用 ECB + PKCS7，ECB 不隐藏明文模式；
/// 新数据应使用带随机 IV 的 CBC/GCM，或 <see cref="System.Security.Cryptography.AesGcm"/>。
/// </remarks>
public static partial class Cryptography
{
}
