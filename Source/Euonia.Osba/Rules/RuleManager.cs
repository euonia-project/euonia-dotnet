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
    /// <remarks>
    /// 请勿直接修改此列表：写入请走 <see cref="Add"/>，读取请走 <see cref="Snapshot"/>。
    /// 本列表按类型进程级共享，直接枚举的同时若发生写入会抛出
    /// <see cref="InvalidOperationException"/>（集合已被修改）。
    /// </remarks>
    public List<IRuleBase> Rules { get; }

    /// <summary>
    /// 向规则列表添加规则。
    /// </summary>
    /// <param name="rule">要添加的规则。</param>
    /// <remarks>
    /// 锁对象是 <see cref="RuleManager"/> 实例本身：<c>BusinessObject.InitializeRules</c>
    /// 已在 <c>lock (rules)</c> 内调用本方法，共用同一把锁，不引入新的锁序。
    /// </remarks>
    internal void Add(IRuleBase rule)
    {
        lock (this)
        {
            Rules.Add(rule);
        }
    }

    /// <summary>
    /// 获取当前规则列表的线程安全快照。
    /// </summary>
    /// <returns>规则列表的副本。</returns>
    internal IReadOnlyList<IRuleBase> Snapshot()
    {
        lock (this)
        {
            return [.. Rules];
        }
    }

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