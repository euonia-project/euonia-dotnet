namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供 AES、DES、SHA、MD5、Base64 与 RSA 等密码学辅助实现。
/// </summary>
/// <remarks>
/// 新数据请使用 <see cref="AES"/> 的 GCM 方法（如 <see cref="AES.EncryptGcm(string?,string)"/>），
/// 或 <see cref="RSA"/> 默认的 OAEP（SHA-256）填充；
/// ECB + PKCS7、PKCS#1 v1.5 与 <see cref="DES"/> 仅用于兼容遗留数据。
/// </remarks>
public static partial class Cryptography
{
}
