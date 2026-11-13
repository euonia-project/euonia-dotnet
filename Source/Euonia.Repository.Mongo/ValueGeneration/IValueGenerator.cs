namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 用于生成值的接口。
/// </summary>
public interface IValueGenerator
{
    /// <summary>
    /// 生成值。
    /// </summary>
    /// <returns>生成的值。</returns>
    object Generate();
}

/// <summary>
/// 用于生成值的接口。
/// </summary>
/// <typeparam name="TValue">生成值的类型。</typeparam>
public interface IValueGenerator<out TValue> : IValueGenerator
{
    /// <summary>
    /// 生成值。
    /// </summary>
    /// <returns>生成的 <typeparamref name="TValue"/> 值。</returns>
    new TValue Generate();
}