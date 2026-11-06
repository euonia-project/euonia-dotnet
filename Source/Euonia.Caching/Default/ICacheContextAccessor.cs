namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Interface ICacheContextAccessor
/// </summary>
public interface ICacheContextAccessor
{
    /// <summary>
    /// Gets or sets the current <see cref="IAcquireContext"/>.
    /// </summary>
    /// <value>The <see cref="IAcquireContext"/> instance.</value>
    IAcquireContext? Current { get; set; }
}