using System.Reflection;
using System.Reflection.Emit;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Properties;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 扫描器去重键回归：去重必须与运行期判定同口径（码按 <see cref="StringComparer.OrdinalIgnoreCase"/>），
/// 不能用 ToUpperInvariant 自行归一——不变大写会把 ß 展开成 SS，使 <c>straße</c> 与 <c>STRASSE</c>
/// 被误判为重复码，而运行期视其为两个不同的码。
/// </summary>
public class PermissionDeclarationScannerUnicodeTests
{
	/// <summary>
	/// <c>straße</c> 与 <c>STRASSE</c> 两码并存不报重复（修复前 ToUpperInvariant 展开 ß→SS 会误报）。
	/// </summary>
	[Fact]
	public void Unicode_Expansion_Codes_Should_Not_Be_Reported_As_Duplicates()
	{
		var exception = Record.Exception(() => PermissionDeclarationScanner.EnsureValid([typeof(UnicodeExpansionCodeAsset).Assembly]));

		Assert.Null(exception);
	}

	/// <summary>
	/// 运行期读取器同样视两码为不同要求：扫描期与运行期口径一致。
	/// </summary>
	[Fact]
	public void Unicode_Expansion_Codes_Should_Both_Be_Discovered()
	{
		var method = typeof(UnicodeExpansionCodeAsset).GetMethod(nameof(UnicodeExpansionCodeAsset.Run))!;

		var requirements = PermissionRequirements.For(typeof(UnicodeExpansionCodeAsset), method);

		Assert.Equal(["straße", "STRASSE"], requirements.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 正向护栏：OrdinalIgnoreCase 下真正相等的码（仅大小写不同）仍必须报重复，
	/// 防止把比较器“修”成 Ordinal 而放走真正的重复声明。
	/// </summary>
	[Fact]
	public void Case_Insensitive_Duplicates_Should_Still_Be_Reported()
	{
		// 用动态程序集隔离违规声明，避免污染本测试程序集的扫描结果
		var assembly = EmitDuplicateCodeAssembly("case:dup", "CASE:DUP");

		var exception = Assert.Throws<InvalidOperationException>(() => PermissionDeclarationScanner.EnsureValid([assembly]));

		Assert.Contains("DuplicateCodeAsset.Run", exception.Message, StringComparison.Ordinal);
		Assert.Contains(string.Format(Resources.IDS_PERMISSION_CODE_DUPLICATE, "DuplicateCodeAsset.Run", "CASE:DUP"), exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 发射一个只含「同成员两条仅大小写不同的权限码」的动态程序集。
	/// </summary>
	/// <param name="first">第一条权限码。</param>
	/// <param name="second">第二条权限码。</param>
	/// <returns>动态程序集。</returns>
	private static Assembly EmitDuplicateCodeAssembly(string first, string second)
	{
		var assembly = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("DuplicateCodeProbe_" + Guid.NewGuid().ToString("N")),
			AssemblyBuilderAccess.Run);
		var module = assembly.DefineDynamicModule("Main");
		var type = module.DefineType("DuplicateCodeAsset", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
		var method = type.DefineMethod("Run", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
		method.GetILGenerator().Emit(OpCodes.Ret);

		var constructor = typeof(PermissionAttribute).GetConstructor([typeof(string)])!;
		method.SetCustomAttribute(new CustomAttributeBuilder(constructor, [first]));
		method.SetCustomAttribute(new CustomAttributeBuilder(constructor, [second]));

		type.CreateType();
		return assembly;
	}
}

/// <summary>
/// 同一方法上并存 <c>straße</c> 与 <c>STRASSE</c> 两个码的资源：
/// 二者在 OrdinalIgnoreCase 下不相等（ß 不折叠为 SS），是两条合法要求。
/// </summary>
public sealed class UnicodeExpansionCodeAsset
{
	/// <summary>
	/// 并存两个仅在 Unicode 展开意义上“相同”的码。
	/// </summary>
	[Permission("straße")]
	[Permission("STRASSE")]
	public void Run()
	{
	}
}
