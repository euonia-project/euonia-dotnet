namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 事务性上下文标记接口。
/// </summary>
/// <remarks>
/// 该接口当前不含任何成员，仅作为参与事务性上下文协作的类型标记，
/// 便于基础设施按类型识别需要事务支持的类型。
/// </remarks>
public interface ITransactionalContext
{
	
}