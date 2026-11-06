namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务标识符。
/// </summary>
internal readonly struct ServiceIdentifier : IEquatable<ServiceIdentifier>
{
	/// <summary>
	/// 获取服务键。
	/// </summary>
	public object? ServiceKey { get; }

	/// <summary>
	/// 获取服务类型。
	/// </summary>
	public Type ServiceType { get; }

	/// <summary>
	/// 初始化 <see cref="ServiceIdentifier"/> 结构的新实例。
	/// </summary>
	/// <param name="serviceType">服务类型。</param>
	public ServiceIdentifier(Type serviceType)
	{
		ServiceType = serviceType;
	}

	/// <summary>
	/// 初始化 <see cref="ServiceIdentifier"/> 结构的新实例。
	/// </summary>
	/// <param name="serviceKey">服务键。</param>
	/// <param name="serviceType">服务类型。</param>
	public ServiceIdentifier(object serviceKey, Type serviceType)
	{
		ServiceKey = serviceKey;
		ServiceType = serviceType;
	}

	/// <summary>
	/// 判断当前标识符与另一个标识符是否指向同一服务。
	/// </summary>
	/// <param name="other">要比较的标识符。</param>
	/// <returns>服务类型相同且服务键相等（同为 <c>null</c> 或值相等）时为 <c>true</c>；否则为 <c>false</c>。</returns>
	public bool Equals(ServiceIdentifier other)
	{
		if (ServiceKey == null && other.ServiceKey == null)
		{
			return ServiceType == other.ServiceType;
		}
		else if (ServiceKey != null && other.ServiceKey != null)
		{
			return ServiceType == other.ServiceType && ServiceKey.Equals(other.ServiceKey);
		}

		return false;
	}

	/// <summary>
	/// 判断指定对象是否为与该标识符相等的 <see cref="ServiceIdentifier"/>。
	/// </summary>
	/// <param name="obj">要比较的对象。</param>
	/// <returns>对象为相等的 <see cref="ServiceIdentifier"/> 时为 <c>true</c>；否则为 <c>false</c>。</returns>
	public override bool Equals(object? obj)
	{
		return obj is ServiceIdentifier identifier && Equals(identifier);
	}

	/// <summary>
	/// 计算标识符的哈希码，仅使用服务类型或在服务键存在时组合两者。
	/// </summary>
	/// <returns>与服务类型和服务键一致的哈希码。</returns>
	public override int GetHashCode()
	{
		if (ServiceKey == null)
		{
			return ServiceType.GetHashCode();
		}

		unchecked
		{
			return (ServiceType.GetHashCode() * 397) ^ ServiceKey.GetHashCode();
		}
	}
}