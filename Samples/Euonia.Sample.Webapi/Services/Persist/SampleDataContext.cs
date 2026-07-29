using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Sample.Persist;

[ConnectionString(Name = "Default")]
internal class SampleDataContext : DataContextWithBus<SampleDataContext>
{
	public SampleDataContext(DbContextOptions<SampleDataContext> options, IBus bus, IRequestContextAccessor request)
		: base(options, bus, request)
	{
	}
}