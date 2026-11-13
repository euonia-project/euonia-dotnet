using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 后台作业特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BackgroundJobAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="BackgroundJobAttribute"/> 的新实例。
	/// </summary>
	/// <param name="name">作业名称；注册作业时若未显式指定名称，则使用该值。</param>
	public BackgroundJobAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 获取作业名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取或设置 Quartz 作业组名称。
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 获取或设置作业的说明。
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 指示当遇到“恢复（recovery）”或“故障转移（fail-over）”情形时，<see cref="IScheduler" /> 是否应重新执行该作业。
    /// </summary>
    public bool? RequestRecovery { get; set; }

    /// <summary>
    /// 作业在成为孤立作业（没有 <see cref="ITrigger" /> 指向它）后是否仍应保留存储。
    /// </summary>
    public bool? StoreDurably { get; set; }
}