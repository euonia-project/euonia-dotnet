namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 定义用于激活具体业务实例的类型。
/// </summary>
public interface IObjectActivator
{
    /// <summary>
    /// 初始化现有的业务对象实例。
    /// </summary>
    /// <param name="obj">业务对象的引用。</param>
    void InitializeInstance(object obj);

    /// <summary>
    /// 终结现有的业务对象实例。在数据门户操作完成后调用。
    /// </summary>
    /// <param name="obj">业务对象的引用。</param>
    void FinalizeInstance(object obj);
}