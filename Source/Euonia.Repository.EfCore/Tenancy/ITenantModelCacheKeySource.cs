using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 模型缓存键工厂可直接读取的租户来源，避免依赖跨请求的 AsyncLocal 残留。
/// </summary>
public interface ITenantModelCacheKeySource
{
	/// <summary>获取当前上下文对应的租户信息。</summary>
	ITenantInfo? CurrentTenant { get; }
}
