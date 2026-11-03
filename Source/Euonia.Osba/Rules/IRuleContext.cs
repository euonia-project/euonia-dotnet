namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务规则被调用时提供的上下文信息。
/// </summary>
public interface IRuleContext
{
    /// <summary>
    /// 获取规则对象。
    /// </summary>
    IRuleBase Rule { get; }

    /// <summary>
    /// 获取目标业务对象的引用。
    /// </summary>
    object Target { get; }

    /// <summary>
    /// 获取目标对象当前作用域的服务提供程序；没有业务上下文时返回 <see langword="null"/>。
    /// </summary>
    /// <remarks>
    /// 规则通常在类型级共享，不能持有请求态；需要按请求解析服务时，使用本属性或
    /// <see cref="GetService{T}"/>，不要依赖构造函数注入的 Scoped 服务。
    /// </remarks>
    IServiceProvider ServiceProvider => (Target as IUseBusinessContext)?.BusinessContext?.CurrentServiceProvider;

    /// <summary>
    /// 从目标对象当前作用域解析服务；服务不存在时返回 <see langword="default"/>。
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    /// <returns>解析出的服务；无法解析时为 <see langword="default"/>。</returns>
    T GetService<T>()
    {
        return ServiceProvider?.GetService(typeof(T)) is T service ? service : default;
    }

    /// <summary>
    /// 获取规则检查结果。
    /// </summary>
    IReadOnlyList<RuleResult> Results { get; }

    /// <summary>
    /// 向规则上下文添加一个错误结果。
    /// </summary>
    /// <param name="description">错误描述。</param>
    void AddErrorResult(string description);

    /// <summary>
    /// 向规则上下文添加一个警告结果。
    /// </summary>
    /// <param name="description">警告描述。</param>
    void AddWarningResult(string description);

    /// <summary>
    /// 向规则上下文添加一个信息结果。
    /// </summary>
    /// <param name="description">信息描述。</param>
    void AddInformationResult(string description);

    /// <summary>
    /// 向规则上下文添加一个成功结果。
    /// </summary>
    void AddSuccessResult();

    /// <summary>
    /// 完成规则上下文。
    /// </summary>
    void Complete();
}