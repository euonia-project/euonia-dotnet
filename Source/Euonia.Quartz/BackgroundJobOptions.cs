using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 后台作业选项。
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// 获取作业名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置 Quartz 作业组名称。
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 获取或设置作业配置。
    /// </summary>
    public Action<IJobConfigurator<IJob>>? JobConfigure { get; set; }

    /// <summary>
    /// 获取或设置触发器配置。
    /// </summary>
    public List<Action<ITriggerConfigurator<IJob>>> TriggerConfigure { get; } = new();
}
