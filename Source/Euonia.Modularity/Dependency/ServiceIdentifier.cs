namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// The service identifier.
/// </summary>
internal readonly struct ServiceIdentifier : IEquatable<ServiceIdentifier>
{
	/// <summary>
	/// Gets the service key.
	/// </summary>
	public object? ServiceKey { get; }

	/// <summary>
	/// Gets the service type.
	/// </summary>
	public Type ServiceType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ServiceIdentifier"/> struct.
	/// </summary>
	/// <param name="serviceType"></param>
	public ServiceIdentifier(Type serviceType)
	{
		ServiceType = serviceType;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ServiceIdentifier"/> struct.
	/// </summary>
	/// <param name="serviceKey"></param>
	/// <param name="serviceType"></param>
	public ServiceIdentifier(object serviceKey, Type serviceType)
	{
		ServiceKey = serviceKey;
		ServiceType = serviceType;
	}

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

	public override bool Equals(object? obj)
	{
		return obj is ServiceIdentifier identifier && Equals(identifier);
	}

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