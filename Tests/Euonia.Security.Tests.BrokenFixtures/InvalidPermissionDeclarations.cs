using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests.BrokenFixtures;

// 本文件集中放置「必须让注册期失败」的权限声明。
// 单独放在 BrokenFixtures 程序集，是因为 AddPermission 会校验被扫描程序集里的全部声明：
// 把这些声明放进测试主程序集，会让所有以 Scan(TestAssembly) 注册的既有用例一并变红。

/// <summary>
/// 方法上的 <see cref="PermissionAttribute"/> 权限码为空（且未声明角色）：注册期应报空码。
/// </summary>
public sealed class EmptyCodeDeclaredAsset
{
	/// <summary>
	/// 权限码为空的入口方法；方法体为空。
	/// </summary>
	[Permission("")]
	public void Approve() { }
}

/// <summary>
/// 方法上的权限码使用了框架保留前缀 <c>@</c>：注册期应报保留前缀。
/// </summary>
public sealed class ReservedPrefixDeclaredAsset
{
	/// <summary>
	/// 权限码落在保留命名空间里的入口方法；方法体为空。
	/// </summary>
	[Permission("@reserved:code")]
	public void Approve() { }
}

/// <summary>
/// 同一成员上重复声明同一个权限码：注册期应报重复声明，读取器则应去重成一条。
/// </summary>
public sealed class DuplicateCodeDeclaredAsset
{
	/// <summary>
	/// 同一个权限码声明了两次的入口方法；方法体为空。
	/// </summary>
	[Permission("dup:code")]
	[Permission("dup:code")]
	public void Approve() { }
}
