namespace System;

/// <summary>
/// 表示一个空值单例。
/// </summary>
[Serializable]
public sealed class Empty : ISerializable
{
	private Empty()
	{
	}

	/// <summary>
	/// 空值单例实例。
	/// </summary>
	public static readonly Empty Value = new();

	/// <summary>
	/// 返回空字符串表示。
	/// </summary>
	public override string ToString()
	{
		return string.Empty;
	}

	/// <inheritdoc />
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
	}
}