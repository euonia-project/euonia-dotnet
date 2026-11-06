// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Class Signals.
/// Implements the <see cref="ICacheSignal" />
/// </summary>
/// <seealso cref="ICacheSignal" />
public class DefaultCacheSignal : ICacheSignal
{
    /// <summary>
    /// The tokens
    /// </summary>
    private readonly Dictionary<object, VolatileToken> _tokens = new();

    /// <summary>
    /// Triggers the specified signal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="signal">The signal.</param>
    public void Trigger<T>(T signal)
    {
        lock (_tokens)
        {
            if (!_tokens.TryGetValue(signal!, out var token))
            {
                return;
            }

            _tokens.Remove(signal!);
            token.Trigger();
        }
    }

    /// <summary>
    /// Whens the specified signal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="signal">The signal.</param>
    /// <returns>IVolatileToken.</returns>
    public IVolatileToken When<T>(T signal)
    {
        lock (_tokens)
        {
            if (_tokens.TryGetValue(signal!, out var token))
            {
                return token;
            }

            token = new VolatileToken();
            _tokens[signal!] = token;

            return token;
        }
    }

    private class VolatileToken : IVolatileToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VolatileToken"/> class.
        /// </summary>
        public VolatileToken()
        {
            IsCurrent = true;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is current.
        /// </summary>
        /// <value><c>true</c> if this instance is current; otherwise, <c>false</c>.</value>
        public bool IsCurrent { get; private set; }

        /// <summary>
        /// Triggers this instance.
        /// </summary>
        public void Trigger()
        {
            IsCurrent = false;
        }
    }
}