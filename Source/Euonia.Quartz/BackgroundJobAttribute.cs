using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// The background job attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BackgroundJobAttribute : Attribute
{
	/// <summary>
	/// Initialize a new instance of <see cref="BackgroundJobAttribute"/>.
	/// </summary>
	/// <param name="name">The job name.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public BackgroundJobAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the job name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the quartz job group name.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets a description for the job.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Instructs the <see cref="IScheduler" /> whether or not the job should be re-executed if a 'recovery' or 'fail-over' situation is encountered.
    /// </summary>
    public bool? RequestRecovery { get; set; }

    /// <summary>
    /// Whether or not the job should remain stored after it is orphaned (no <see cref="ITrigger" />s point to it).
    /// </summary>
    public bool? StoreDurably { get; set; }
}