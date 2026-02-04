using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The rule manager.
/// </summary>
public class RuleManager
{
    private static readonly Lazy<ConcurrentDictionary<Type, RuleManager>> _container = new();

    private RuleManager()
    {
        Rules = new List<IRuleBase>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="RuleManager"/> is initialized.
    /// </summary>
    public bool Initialized { get; set; }

    /// <summary>
    /// Gets the rules.
    /// </summary>
    public List<IRuleBase> Rules { get; }

    /// <summary>
    /// Gets the rules of specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static RuleManager GetRules<T>()
    {
        return GetRules(typeof(T));
    }

    /// <summary>
    /// Gets the rules of specified type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static RuleManager GetRules(Type type)
    {
	    var result = _container.Value.GetOrAdd(type, _ => new RuleManager());
        return result;
    }

    /// <summary>
    /// Cleans the rules of specified type.
    /// </summary>
    /// <param name="type"></param>
    public static void CleanRules(Type type)
    {
        lock (_container)
        {
            _container.Value.TryRemove(type, out var _);
        }
    }
}