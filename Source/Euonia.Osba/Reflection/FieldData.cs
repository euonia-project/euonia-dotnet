using System.Reactive.Subjects;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The field data.
/// </summary>
/// <typeparam name="T"></typeparam>
public class FieldData<T> : IFieldData<T>
{
	private readonly Stack<T> _histories = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldData{T}"/> class.
	/// </summary>
	public FieldData()
	{
		ObservableValue.Subscribe(value =>
		{
			_histories.Push(value);
		});
	}

	/// <inheritdoc />
	public FieldData(string name)
		: this()
	{
		Name = name;
	}

	/// <inheritdoc />
	public string Name { get; }

	private readonly BehaviorSubject<T> _subject = new(default);

	private T _value;

	/// <inheritdoc />
	public T Value
	{
		get => _subject.Value;
		set
		{
			if (Equals(_value, value))
			{
				return;
			}

			_value = value;
			_subject.OnNext(value);
		}
	}

	/// <inheritdoc />
	public void MarkAsUnchanged()
	{
		_histories.Clear();
	}

	/// <inheritdoc />
	public void Undo()
	{
		if (_histories.Count > 0 && _histories.TryPop(out var value))
		{
			Value = value;
		}
	}

	object IFieldData.Value
	{
		get => Value;
		set => Value = value == null ? default : (T)value;
	}

	/// <summary>
	/// Gets the observable value.
	/// </summary>
	public IObservable<T> ObservableValue => _subject;

	/// <summary>
	/// Gets a value indicating whether the field data is valid.
	/// </summary>
	public bool IsValid
	{
		get
		{
			if (Value is ITrackableObject trackable)
			{
				return trackable.IsValid;
			}

			return true;
		}
	}

	/// <inheritdoc />
	public bool IsChanged => _histories.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the field data is deleted.
	/// </summary>
	public bool IsDeleted 
	{
		get
		{
			if (Value is ITrackableObject trackable)
			{
				return trackable.IsDeleted;
			}

			return false;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the field data is new.
	/// </summary>
	public bool IsNew
	{
		get
		{
			if (Value is ITrackableObject trackable)
			{
				return trackable.IsNew;
			}

			return false;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the field data can be saved.
	/// </summary>
	public bool IsSavable
	{
		get
		{
			if (Value is ITrackableObject trackable)
			{
				return trackable.IsSavable;
			}

			return false;
		}
	}

	/// <summary>
	/// Occurs when the busy status changes.
	/// </summary>
	event BusyChangedEventHandler INotifyBusy.BusyChanged
	{
		add => throw new NotImplementedException();
		remove => throw new NotImplementedException();
	}

	/// <summary>
	/// Gets a value indicating whether the field data, or any of its children, is busy.
	/// </summary>
	public bool IsBusy
	{
		get
		{
			bool isBusy = false;
			if (Value is ITrackableObject trackable)
			{
				isBusy = trackable.IsBusy;
			}

			return isBusy;
		}
	}

	bool INotifyBusy.IsSelfBusy => IsBusy;
}