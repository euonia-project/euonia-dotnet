namespace Nerosoft.Euonia;

/// <summary>
/// <see cref="IKeyTransformer"/> 的空实现，原样返回键。
/// </summary>
public sealed class NullKeyTransformer : IKeyTransformer
{
	/// <summary>
	/// 获取共享的单例实例。
	/// </summary>
	public static readonly NullKeyTransformer Instance = new();

	/// <inheritdoc />
	public string Transform(string key)
	{
		return key;
	}
}
