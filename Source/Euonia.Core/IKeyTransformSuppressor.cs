namespace Nerosoft.Euonia;

/// <summary>
/// 定义宿主级键转换的逃逸作用域。
/// </summary>
/// <remarks>
/// 管理员跨租户操作、平台配置与租户存储缓存需要在“当前租户已解析”的情况下使用不带
/// 租户前缀的键。通过该作用域临时抑制 <see cref="IKeyTransformer"/> 的租户维度。
/// </remarks>
public interface IKeyTransformSuppressScope : IDisposable
{
}

/// <summary>
/// 定义创建键转换逃逸作用域的契约。
/// </summary>
public interface IKeyTransformSuppressor
{
	/// <summary>
	/// 进入键转换逃逸作用域；释放后恢复。
	/// </summary>
	/// <returns>可恢复的逃逸作用域。</returns>
	IKeyTransformSuppressScope Suppress();
}
