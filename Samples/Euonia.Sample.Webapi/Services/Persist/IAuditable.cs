using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 表示具有审计属性、且主键类型为 <see cref="string"/> 的对象。
/// </summary>
/// <remarks>
/// 该接口是 <see cref="IAuditable{TKey}"/> 以 <see cref="string"/> 作为键类型的特化形式，
/// 用于需要记录创建、修改等审计信息的实体。
/// </remarks>
public interface IAuditable : IAuditable<string>
{
}
