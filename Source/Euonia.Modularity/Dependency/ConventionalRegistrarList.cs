namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 保存按类型发现顺序排列的自动注册器集合，供程序集扫描时依次应用。
/// </summary>
internal class ConventionalRegistrarList : List<IAutomaticRegistration>
{

}
