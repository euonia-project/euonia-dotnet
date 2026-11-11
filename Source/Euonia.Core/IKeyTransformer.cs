namespace Nerosoft.Euonia;

/// <summary>
/// 定义缓存键、锁键与幂等键的转换契约。
/// </summary>
/// <remarks>
/// 能力内核不直接引用缓存或应用层。租户等多租户维度通过该钩子统一加到键上，
/// 未启用时注册 <see cref="NullKeyTransformer"/>，行为与当前版本一致。
/// </remarks>
public interface IKeyTransformer
{
	/// <summary>
	/// 转换指定键。
	/// </summary>
	/// <param name="key">原始键。</param>
	/// <returns>转换后的键；无租户等维度时原样返回。</returns>
	string Transform(string key);
}
