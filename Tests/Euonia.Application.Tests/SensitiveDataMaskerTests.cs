using System.Collections;

namespace Nerosoft.Euonia.Application.Tests;

public class SensitiveDataMaskerTests
{
	[Fact]
	public void Mask_Null_ShouldReturnNull()
	{
		Assert.Null(SensitiveDataMasker.Mask(null));
	}

	[Fact]
	public void Mask_StringAndScalars_ShouldPassThrough()
	{
		Assert.Equal("hello", SensitiveDataMasker.Mask("hello"));
		Assert.Equal(42, SensitiveDataMasker.Mask(42));
		Assert.Equal(true, SensitiveDataMasker.Mask(true));
	}

	[Fact]
	public void Mask_Object_ShouldMaskSensitiveProperties()
	{
		var command = new LoginCommand { Username = "alice", Password = "plain" };

		var result = SensitiveDataMasker.Mask(command) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("alice", result[nameof(LoginCommand.Username)]);
		Assert.Equal("###", result[nameof(LoginCommand.Password)]);
	}

	[Fact]
	public void Mask_NestedSensitiveObject_ShouldRecurse()
	{
		var outer = new OuterCommand { Name = "outer" };
		outer.Inner = new InnerCommand { Id = 1, Secret = "s3cret" };

		var result = SensitiveDataMasker.Mask(outer) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("outer", result[nameof(OuterCommand.Name)]);
		var inner = Assert.IsAssignableFrom<IDictionary<string, object>>(result[nameof(OuterCommand.Inner)]);
		Assert.Equal(1, inner[nameof(InnerCommand.Id)]);
		Assert.Equal("###", inner[nameof(InnerCommand.Secret)]);
	}

	[Fact]
	public void Mask_Collection_ShouldMaskEachItem()
	{
		var list = new List<LoginCommand>
		{
			new() { Username = "a", Password = "s1" },
			new() { Username = "b", Password = "s2" },
		};

		var result = SensitiveDataMasker.Mask(list) as IList;

		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		var first = Assert.IsAssignableFrom<IDictionary<string, object>>(result[0]);
		Assert.Equal("a", first[nameof(LoginCommand.Username)]);
		Assert.Equal("###", first[nameof(LoginCommand.Password)]);
	}

	[Fact]
	public void Mask_CyclicReference_ShouldNotInfiniteLoop()
	{
		var node = new CyclicNode { Name = "node" };
		node.Child = node;

		var result = SensitiveDataMasker.Mask(node) as IDictionary<string, object>;

		Assert.NotNull(result);
		Assert.Equal("node", result[nameof(CyclicNode.Name)]);
		Assert.NotNull(result[nameof(CyclicNode.Child)]);
	}

	private sealed class OuterCommand
	{
		public string Name { get; set; }

		public InnerCommand Inner { get; set; }
	}

	private sealed class InnerCommand
	{
		public int Id { get; set; }

		[SensitiveData(Mask = "###")]
		public string Secret { get; set; }
	}

	private sealed class CyclicNode
	{
		public string Name { get; set; }

		public CyclicNode Child { get; set; }
	}
}