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
	/// <remarks>
	/// 每次写入都会 <see cref="Stack{T}.Push"/>，仅 <see cref="MarkAsUnchanged"/> 清空、
	/// <see cref="Undo"/> 逐个弹出——若调用方从不提交/撤销（例如高频遥测字段），
	/// 历史会随对象生命周期无界增长。因此入栈时按 <see cref="HistoryDepth"/> 截断：
	/// 超出深度的最旧记录被丢弃（撤销只能回到有限步，<see cref="IsChanged"/> 语义不变）。
	/// 默认深度 32 对交互式编辑足够；设为负数表示不设限（维持旧行为）。
	/// </remarks>
	private readonly Stack<T> _histories = new();

	/// <summary>
	/// 获取或设置历史记录的最大深度；超出后丢弃最旧记录。负数表示不设限。
	/// </summary>
	public int HistoryDepth { get; set; } = 32;

	/// <summary>
	/// 初始化 <see cref="FieldData{T}"/> 的新实例。
	/// </summary>
	public FieldData()
	{
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

			// 记录旧值以支持撤销操作；超过深度时丢弃最旧记录（栈底），保证内存有界
			_histories.Push(_value);
			if (HistoryDepth >= 0 && _histories.Count > HistoryDepth)
			{
				TrimOldest();
			}

			_value = value;
			_subject.OnNext(value);
		}
	}

	/// <summary>
	/// 丢弃历史栈底（最旧）的一条记录。
	/// </summary>
	private void TrimOldest()
	{
		// Stack 不支持移除底部元素：倒出到数组后跳过最后一条（栈底）再压回。
		// 深度默认 32，倒出成本可忽略；不使用递归弹出以免深栈时的调用开销。
		var items = _histories.ToArray();
		_histories.Clear();

		// ToArray 返回栈顶在前的数组；丢弃最后一条（栈底 = 最旧），其余按原序压回
		for (var index = 0; index < items.Length - 1; index++)
		{
			_histories.Push(items[index]);
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
		if (_histories.TryPop(out var value))
		{
			// 直接恢复值，避免再次写入历史栈
			_value = value;
			_subject.OnNext(value);
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
	private BusyChangedEventHandler _busyChanged;

	event BusyChangedEventHandler INotifyBusy.BusyChanged
	{
		add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
		remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
	}

	/// <summary>
	/// 引发 <see cref="INotifyBusy.BusyChanged"/> 事件。
	/// </summary>
	/// <param name="args">事件参数。</param>
	protected virtual void OnBusyChanged(BusyChangedEventArgs args)
	{
		_busyChanged?.Invoke(this, args);
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