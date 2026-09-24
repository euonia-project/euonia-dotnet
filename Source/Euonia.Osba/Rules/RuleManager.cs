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
    /// 保护 <see cref="Rules"/> 列表读写的锁。
    /// </summary>
    /// <remarks>
    /// 刻意<b>不用实例自身</b>作为锁：<c>BusinessObject.InitializeRules</c> 会持有 <c>lock (rules)</c>
    /// 并调用使用方代码（<c>AddRules()</c>），而那可能回调进 <c>Rules</c> 的实例级规则集合；
    /// 规则检查路径则反向（持有 <c>Rules._lockObject</c> 时经通知回调触发 <see cref="Add"/>）。
    /// 两者用同一把锁就会形成锁序环。分开之后，本锁只在极短的列表读写期间被持有，环不存在。
    /// </remarks>
    private readonly Lock _sync = new();

    /// <summary>
    /// 「属性 → 该属性的类型级规则」索引，惰性构建；<see cref="Add"/> 时失效。
    /// </summary>
    /// <remarks>
    /// 键用属性实例本身：类型内的解析一律按<b>引用</b>匹配（见 <see cref="Rules.ResolvePropertyRules"/>），
    /// 而 <c>PropertyInfo&lt;T&gt;</c> 未重写 <c>Equals</c>，字典的默认比较器即引用相等，语义一致。
    /// </remarks>
    private Dictionary<IPropertyInfo, IRuleBase[]> _propertyRules;

    /// <summary>
    /// 向规则列表添加规则。
    /// </summary>
    /// <param name="rule">要添加的规则。</param>
    internal void Add(IRuleBase rule)
    {
        lock (_sync)
        {
            Rules.Add(rule);
            _propertyRules = null;
        }
    }

    /// <summary>
    /// 获取指定属性适用的类型级规则（已按优先级升序），无规则时返回空集合。
    /// </summary>
    /// <param name="property">目标属性。</param>
    /// <returns>规则集合。</returns>
    /// <remarks>
    /// 走索引而不是每次线性过滤：属性 setter 是热路径——属性级规则在属性变更时触发检查，
    /// 这里不能出现「每次赋值都做一遍快照 + LINQ」的开销。命中路径无锁、无分配。
    /// </remarks>
    internal IReadOnlyList<IRuleBase> RulesFor(IPropertyInfo property)
    {
        var index = _propertyRules;

        if (index == null)
        {
            lock (_sync)
            {
                index = _propertyRules ??= BuildPropertyRules();
            }
        }

        return index.TryGetValue(property, out var rules) ? rules : [];
    }

    /// <summary>
    /// 构建「属性 → 类型级规则」索引。
    /// </summary>
    /// <returns>索引。</returns>
    private Dictionary<IPropertyInfo, IRuleBase[]> BuildPropertyRules()
    {
        return Rules.Where(rule => rule.Property != null)
                    .GroupBy(rule => rule.Property)
                    .ToDictionary(group => group.Key, group => group.OrderBy(rule => rule.Priority).ToArray());
    }

    /// <summary>
    /// 获取当前规则列表的线程安全快照。
    /// </summary>
    /// <returns>规则列表的副本。</returns>
    internal IReadOnlyList<IRuleBase> Snapshot()
    {
        lock (_sync)
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