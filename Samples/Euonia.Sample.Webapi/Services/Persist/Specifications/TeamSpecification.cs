using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Specifications;

/// <summary>团队查询规范。</summary>
internal static class TeamSpecification
{
	public static readonly Specification<Team> All = new DirectSpecification<Team>(t => t.Id != null);

	/// <summary>Id 等于 <paramref name="id"/>。</summary>
	public static Specification<Team> IdEquals(string id)
	{
		return new DirectSpecification<Team>(t => t.Id == id);
	}

	/// <summary>名称包含 <paramref name="keyword"/>。</summary>
	public static Specification<Team> NameContains(string keyword)
	{
		return new DirectSpecification<Team>(t => t.Name.Contains(keyword));
	}
}