using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Specifications;

/// <summary>项目查询规范。</summary>
internal static class ProjectSpecification
{
	public static readonly Specification<Project> All = new DirectSpecification<Project>(t => t.Id != null);

	/// <summary>Id 等于 <paramref name="id"/>。</summary>
	public static Specification<Project> IdEquals(string id)
	{
		return new DirectSpecification<Project>(t => t.Id == id);
	}

	/// <summary>名称包含 <paramref name="keyword"/>。</summary>
	public static Specification<Project> NameContains(string keyword)
	{
		return new DirectSpecification<Project>(t => t.Name.Contains(keyword));
	}
}