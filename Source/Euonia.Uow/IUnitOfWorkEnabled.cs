namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 标记接口，指示某类型参与工作单元。
/// </summary>
/// <remarks>
/// 需要在工作单元范围内受应用基础设施管理的类（例如需要事务边界的仓储或服务）可实现该接口。
/// 该接口刻意保持为空，仅作为标记，供依赖注入容器或中间件按约定识别。
/// </remarks>
public interface IUnitOfWorkEnabled
{
}