using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Domain;

/// <summary>
/// The abstract implement of <see cref="ICommand"/>
/// </summary>
public abstract class Command : ICommand
{
	private const string PROPERTY_ID = "nerosoft.euonia.internal.command.id";

	/// <summary>
	/// Initializes a new instance of the <see cref="Command"/> class.
	/// </summary>
	protected Command()
	{
		Properties[PROPERTY_ID] = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();
	}

	/// <summary>
	/// Gets the extended properties of command.
	/// </summary>
	public virtual IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

	/// <summary>
	/// Gets or sets the command property with specified name.
	/// </summary>
	/// <param name="name"></param>
	public virtual string this[string name]
	{
		get => Properties.TryGetValue(name, out var value) ? value : default;
		set => Properties[name] = value;
	}

	/// <summary>
	/// Gets value of property <paramref name="name"/>.
	/// </summary>
	/// <param name="name"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public virtual T GetProperty<T>(string name)
	{
		return TypeHelper.CoerceValue<T, string>(this[name]);
	}

	/// <summary>
	/// Gets or sets the command identifier.
	/// </summary>
	public virtual string CommandId
	{
		get => this[PROPERTY_ID];
		set => this[PROPERTY_ID] = value;
	}
}

/// <summary>
/// The abstract command with extend data.
/// </summary>
/// <typeparam name="TData"></typeparam>
public abstract class Command<TData> : Command
	where TData : class
{
	/// <summary>
	/// Gets or sets the command data.
	/// </summary>
	public virtual TData Data { get; set; }
}