namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 值对象的非泛型标记接口。
/// </summary>
/// <remarks>
/// 用于在泛型约束（如仓储、映射器等）中标识值对象类型，
/// 而不必依赖具体的 <see cref="ValueObject{TValueObject}"/> 基类。
/// </remarks>
public interface IValueObject
{
}