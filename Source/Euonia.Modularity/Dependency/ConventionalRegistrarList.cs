using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 保存按类型发现顺序排列的自动注册器集合，并记录每个注册器已经扫描过的程序集，
/// 避免同一程序集被重复扫描导致服务重复注册。
/// </summary>
internal class ConventionalRegistrarList : List<IAutomaticRegistration>
{
    private readonly HashSet<(IAutomaticRegistration Registrar, Assembly Assembly)> _scannedAssemblies = new();

    /// <summary>
    /// 判断指定注册器是否已经扫描过指定程序集。
    /// </summary>
    /// <param name="registrar">自动注册器。</param>
    /// <param name="assembly">要检查的程序集。</param>
    /// <returns>已扫描时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    public bool HasScanned(IAutomaticRegistration registrar, Assembly assembly)
    {
        return _scannedAssemblies.Contains((registrar, assembly));
    }

    /// <summary>
    /// 标记指定注册器已经扫描过指定程序集。
    /// </summary>
    /// <param name="registrar">自动注册器。</param>
    /// <param name="assembly">已扫描的程序集。</param>
    public void MarkScanned(IAutomaticRegistration registrar, Assembly assembly)
    {
        _scannedAssemblies.Add((registrar, assembly));
    }
}
