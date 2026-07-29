namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// PipelineDelegate
/// </summary>
/// <param name="context">The context.</param>
/// <returns>Task.</returns>
public delegate Task PipelineDelegate(object context);

/// <summary>
/// PipelineDelegate
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <returns></returns>
public delegate Task PipelineDelegate<in TRequest>(TRequest request);

/// <summary>
/// PipelineDelegate
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <returns></returns>
public delegate Task<TResponse> PipelineDelegate<in TRequest, TResponse>(TRequest request);