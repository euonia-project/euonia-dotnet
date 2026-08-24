namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// DTO 与聚合（aggregate）之间相互映射的基础契约。
/// </summary>
/// <remarks>
/// 该契约适用于"自动"映射器（如 AutoMapper、EmitMapper、ValueInjecter 等），
/// 也适用于临时（adhoc）映射器。
/// </remarks>
public interface ITypeAdapter
{
    /// <summary>
    /// 将源对象适配（映射）为 <typeparamref name="TDestination"/> 类型的新实例。
    /// </summary>
    /// <typeparam name="TSource">源项的类型。</typeparam>
    /// <typeparam name="TDestination">目标项的类型。</typeparam>
    /// <param name="source">要适配的实例。</param>
    /// <returns><paramref name="source"/> 映射为 <typeparamref name="TDestination"/> 后的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    TDestination Adapt<TSource, TDestination>(TSource source)
        where TDestination : class
        where TSource : class;

    /// <summary>
    /// 将源对象适配（映射）到已有的 <paramref name="destination"/> 实例上。
    /// </summary>
    /// <param name="source">要适配的实例。</param>
    /// <param name="destination">目标实例。</param>
    /// <typeparam name="TSource">源项的类型。</typeparam>
    /// <typeparam name="TDestination">目标项的类型。</typeparam>
    /// <returns><paramref name="source"/> 映射为 <typeparamref name="TDestination"/> 后的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        where TDestination : class
        where TSource : class;

    /// <summary>
    /// 将源对象适配（映射）为 <typeparamref name="TDestination"/> 类型的新实例。
    /// </summary>
    /// <typeparam name="TDestination">目标项的类型。</typeparam>
    /// <param name="source">要适配的实例。</param>
    /// <returns><paramref name="source"/> 映射为 <typeparamref name="TDestination"/> 后的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    TDestination Adapt<TDestination>(object source)
        where TDestination : class;

    /// <summary>
    /// 将源对象适配（映射）为 <paramref name="destinationType"/> 所表示类型的新实例。
    /// </summary>
    /// <param name="source">要适配的实例。</param>
    /// <param name="destinationType">目标类型。</param>
    /// <returns><paramref name="source"/> 映射为 <paramref name="destinationType"/> 后的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    object Adapt(object source, Type destinationType);

    /// <summary>
    /// 将源对象适配（映射）到已有的 <paramref name="destination"/> 实例上。
    /// </summary>
    /// <param name="source">要适配的实例。</param>
    /// <param name="destination">目标实例。</param>
    /// <typeparam name="TDestination">目标项的类型。</typeparam>
    /// <returns><paramref name="source"/> 映射为 <typeparamref name="TDestination"/> 后的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
    TDestination Adapt<TDestination>(object source, TDestination destination)
        where TDestination : class;
}