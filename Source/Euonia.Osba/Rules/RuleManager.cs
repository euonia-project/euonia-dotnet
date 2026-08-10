using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 规则管理器。
/// </summary>
public class RuleManager
{
    /// <summary>
    /// 存储类型与其规则管理器映射的并发字典。
    /// </summary>
    private static readonly Lazy<ConcurrentDictionary<Type, RuleManager>> _container = new();

    /// <summary>
    /// 初始化 <see cref="RuleManager"/> 类的新实例。
    /// </summary>
    private RuleManager()
    {
        Rules = new List<IRuleBase>();
    }

    /// <summary>
    /// 获取或设置一个值，指示此 <see cref="RuleManager"/> 是否已初始化。
    /// </summary>
    public bool Initialized { get; set; }

    /// <summary>
    /// 获取规则列表。
    /// </summary>
    public List<IRuleBase> Rules { get; }

    /// <summary>
    /// 获取指定类型的规则。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <returns>指定类型的规则管理器。</returns>
    public static RuleManager GetRules<T>()
    {
        return GetRules(typeof(T));
    }

    /// <summary>
    /// 获取指定类型的规则。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <returns>指定类型的规则管理器。</returns>
    public static RuleManager GetRules(Type type)
    {
	    var result = _container.Value.GetOrAdd(type, _ => new RuleManager());
        return result;
    }

    /// <summary>
    /// 清理指定类型的规则。
    /// </summary>
    /// <param name="type">要清理规则的类型。</param>
    public static void CleanRules(Type type)
    {
        lock (_container)
        {
            _container.Value.TryRemove(type, out var _);
        }
    }
}