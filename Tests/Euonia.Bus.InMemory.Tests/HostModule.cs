using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 测试宿主模块，依赖消息总线与内存总线模块以提供进程内总线服务。
/// </summary>
[DependsOn(typeof(MessageBusModule), typeof(InMemoryBusModule))]
public class HostModule : ModuleContextBase
{
}