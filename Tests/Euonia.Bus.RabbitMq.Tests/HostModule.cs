using Nerosoft.Euonia.Bus.RabbitMq;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// RabbitMQ 传输的测试宿主模块。
/// </summary>
/// <remarks>
/// 故意不依赖 <see cref="RabbitMqBusModule"/>：该模块会在启动时尝试连接 broker，
/// 在没有可用 RabbitMQ 的环境（如 CI）中会导致宿主启动失败。此时共享测试类
/// <c>ServiceBusTests</c> 会依据 <c>PreventRunTests</c> 显式跳过而非静默通过。
/// 需要在真实 broker 上运行时，取消下面的注释并把 <c>PreventRunTests</c> 设为 <c>false</c>。
/// </remarks>
//[DependsOn(typeof(RabbitMqBusModule))]
public class HostModule : ModuleContextBase
{
}
