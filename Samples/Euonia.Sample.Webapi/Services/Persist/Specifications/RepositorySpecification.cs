using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Persist.Specifications;

/// <summary>仓库查询规范。</summary>
internal static class RepositorySpecification
{
	public static readonly Specification<CodeRepository> All = new DirectSpecification<CodeRepository>(t => t.Id != null);

	/// <summary>Id 等于 <paramref name="id"/>。</summary>
	public static Specification<CodeRepository> IdEquals(string id)
	{
		return new DirectSpecification<CodeRepository>(t => t.Id == id);
	}

	/// <summary>名称包含 <paramref name="keyword"/>。</summary>
	public static Specification<CodeRepository> NameContains(string keyword)
	{
		return new DirectSpecification<CodeRepository>(t => t.Name.Contains(keyword));
	}
}