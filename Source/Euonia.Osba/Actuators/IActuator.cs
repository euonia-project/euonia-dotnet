namespace Nerosoft.Euonia.Osba;

public interface IActuator
{
	ActuatorBuilder<TTarget> For<TTarget>()
		where TTarget : EditableObject<TTarget>;
}