using System.Reactive.Subjects;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 字段数据。
/// </summary>
/// <typeparam name="T">字段值的类型。</typeparam>
public class FieldData<T> : IFieldData<T>
{
	/// <summary>
	/// 存储字段值历史记录的栈，用于支持撤销操作。
	/// </summary>
	private readonly Stack<T> _histories = new();

	/// <summary>
	/// 初始化 <see cref="FieldData{T}"/> 类的新实例。
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

	/// <summary>
	/// 用于发布值更改的行为主题。
	/// </summary>
	private readonly BehaviorSubject<T> _subject = new(default);

	/// <summary>
	/// 当前字段值。
	/// </summary>
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
	/// 获取可观察的值。
	/// </summary>
	public IObservable<T> ObservableValue => _subject;

	/// <summary>
	/// 获取一个值，指示字段数据是否有效。
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
	/// 获取一个值，指示字段数据是否已删除。
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
	/// 获取一个值，指示字段数据是否为新增。
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
	/// 获取一个值，指示字段数据是否可保存。
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
	/// 当繁忙状态改变时发生。
	/// </summary>
	event BusyChangedEventHandler INotifyBusy.BusyChanged
	{
		add => throw new NotImplementedException();
		remove => throw new NotImplementedException();
	}

	/// <summary>
	/// 获取一个值，指示字段数据或其任何子对象是否繁忙。
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