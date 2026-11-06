namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务公开上下文的实现。
/// </summary>
public class ServiceExposingContext : IServiceExposingContext
{
	/// <inheritdoc />
	public Type ImplementationType { get; }

	/// <inheritdoc />
	public List<Type> ExposedTypes { get; }

    /// <summary>
    /// 初始化 <see cref="ServiceExposingContext"/> 类的新实例。
    /// </summary>
    /// <param name="implementationType">服务的实现类型，不能为 <see langword="null"/>。</param>
    /// <param name="exposedTypes">该实现要公开的服务类型列表，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="implementationType"/> 或 <paramref name="exposedTypes"/> 为 <see langword="null"/> 时抛出。</exception>
    public ServiceExposingContext(Type implementationType, List<Type> exposedTypes)
    {
        ImplementationType = Check.EnsureNotNull(implementationType, nameof(implementationType));
        ExposedTypes = Check.EnsureNotNull(exposedTypes, nameof(exposedTypes));
    }
}
