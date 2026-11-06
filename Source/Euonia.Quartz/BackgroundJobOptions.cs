using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// The background job options.
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the quartz job group name.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets job configuration.
    /// </summary>
    public Action<IJobConfigurator<IJob>>? JobConfigure { get; set; }

    /// <summary>
    /// Gets or sets trigger configuration.
    /// </summary>
    public List<Action<ITriggerConfigurator<IJob>>> TriggerConfigure { get; } = new();
}
