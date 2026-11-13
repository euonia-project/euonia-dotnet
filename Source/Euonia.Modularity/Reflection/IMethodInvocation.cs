using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 表示一次方法调用的上下文信息。
/// </summary>
public interface IMethodInvocation
{
    /// <summary>
    /// 获取调用方法时传入的参数数组。
    /// </summary>
    object[] Arguments { get; }

    /// <summary>
    /// 获取以参数名称为键的调用参数字典。
    /// </summary>
    IReadOnlyDictionary<string, object> ArgumentsDictionary { get; }

    /// <summary>
    /// 获取方法泛型参数的具体类型数组。
    /// </summary>
    Type[] GenericArguments { get; }

    /// <summary>
    /// 获取方法调用所针对的目标对象。
    /// </summary>
    object TargetObject { get; }

    /// <summary>
    /// 获取被调用的方法元数据。
    /// </summary>
    MethodInfo Method { get; }

    /// <summary>
    /// 获取或设置方法调用的返回值。
    /// </summary>
    object ReturnValue { get; set; }

    /// <summary>
    /// 继续执行被拦截的方法调用。
    /// </summary>
    /// <returns>表示异步执行的任务。</returns>
    Task ProceedAsync();
}