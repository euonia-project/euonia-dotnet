using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

[DependsOn(typeof(MessageBusModule), typeof(InMemoryBusModule))]
public class HostModule : ModuleContextBase
{
}