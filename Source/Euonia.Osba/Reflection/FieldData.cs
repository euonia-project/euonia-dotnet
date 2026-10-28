namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 字段数据。
/// </summary>
/// <typeparam name="T">字段值的类型。</typeparam>
/// <remarks>
/// <para>
/// 值本身与撤销历史都保存在本实例内，撤销历史使用<b>环形缓冲区</b>：
/// 入栈、裁剪、撤销都是 O(1)，且历史长度有界。
/// </para>
/// <para>
/// 读写与撤销都在同一把锁内完成。业务对象实例默认不承诺可并发修改，但字段级操作
/// 至少不能损坏历史缓冲区；锁的临界区只包含赋值与索引运算，不调用外部代码。
/// </para>
/// <para>
/// <see cref="ObservableValue"/> 采用按需创建：绝大多数业务对象从不订阅字段值，
/// 不订阅就不分配可观察对象，也不在写入路径上做任何发布。
/// </para>
/// </remarks>
public class FieldData<T> : IFieldData<T>, IFieldDataInternal
{
	/// <summary>
	/// 保护 <see cref="_value"/> 与历史缓冲区的同步锁。
	/// </summary>
	private readonly object _sync = new();

	/// <summary>
	/// 当前字段值。
	/// </summary>
	private T _value;

	/// <summary>
	/// 环形历史缓冲区；长度按需增长，最大不超过 <see cref="HistoryDepth"/>。
	/// </summary>
	private T[] _history;

	/// <summary>
	/// 历史缓冲区中最早一条记录的位置。
	/// </summary>
	private int _historyStart;

	/// <summary>
	/// 历史缓冲区中的记录数量。
	/// </summary>
	private int _historyCount;

	/// <summary>
	/// 历史最大深度；负数表示不设限。
	/// </summary>
	private int _historyDepth = 32;

	/// <summary>
	/// 按需创建的可观察值；未订阅时为 <see langword="null"/>。
	/// </summary>
	private FieldObservable _observable;

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
	/// 获取或设置历史记录的最大深度；超出后丢弃最旧记录。负数表示不设限。
	/// </summary>
	/// <remarks>
	/// 调小深度会立即丢弃最旧的多余记录（保留最近的若干条）；调大深度不会预分配。
	/// </remarks>
	public int HistoryDepth
	{
		get => _historyDepth;
		set
		{
			lock (_sync)
			{
				_historyDepth = value;
				TrimToDepth();
			}
		}
	}

	/// <summary>
	/// 获取当前字段值。
	/// </summary>
	public T Value
	{
		get
		{
			lock (_sync)
			{
				return _value;
			}
		}
		set => SetValue(value, trackHistory: true);
	}

	/// <summary>
	/// 设置字段值，并可选是否记录历史。
	/// </summary>
	/// <param name="value">新值。</param>
	/// <param name="trackHistory">
	/// 是否记录旧值。装载/物化（<c>LoadProperty</c>）应传 <see langword="false"/>：
	/// 装载不是用户修改，不应产生撤销记录，也不应触发一次“写后再清空”的无谓分配。
	/// </param>
	public void SetValue(T value, bool trackHistory)
	{
		T oldValue;
		bool changed;

		lock (_sync)
		{
			changed = !EqualityComparer<T>.Default.Equals(_value, value);
			if (!changed)
			{
				return;
			}

			oldValue = _value;
			if (trackHistory)
			{
				PushHistory(oldValue);
			}

			_value = value;
		}

		_observable?.Publish(value);
	}

	/// <summary>
	/// 获取可观察的值；首次访问时才创建，未订阅不产生任何开销。
	/// </summary>
	public IObservable<T> ObservableValue
	{
		get
		{
			var observable = Volatile.Read(ref _observable);
			if (observable != null)
			{
				return observable;
			}

			var created = new FieldObservable(this);
			return Interlocked.CompareExchange(ref _observable, created, null) ?? created;
		}
	}

	/// <summary>
	/// 将字段标记为未更改，清空撤销历史。
	/// </summary>
	public void MarkAsUnchanged()
	{
		lock (_sync)
		{
			// 释放对旧值的引用，避免历史缓冲区长期持有大对象。
			if (_history != null && _historyCount > 0)
			{
				Array.Clear(_history, 0, _history.Length);
			}

			_historyCount = 0;
			_historyStart = 0;
		}
	}

	/// <summary>
	/// 将值恢复到上一个历史值。
	/// </summary>
	public void Undo()
	{
		T value;
		lock (_sync)
		{
			if (_historyCount == 0)
			{
				return;
			}

			// 使用与 PushHistory 相同的有效容量：深度调小后数组可能更长，
			// 若按数组长度取模会读到已经不在环内的陈旧槽位。
			var capacity = _historyDepth < 0 ? _history.Length : Math.Min(_history.Length, _historyDepth);
			var index = (_historyStart + _historyCount - 1) % capacity;
			value = _history[index];
			_historyCount--;
			_value = value;
		}

		_observable?.Publish(value);
	}

	object IFieldData.Value
	{
		get => Value;
		set => SetValue(value == null ? default : (T)value, trackHistory: true);
	}

	void IFieldDataInternal.SetValue(object value, bool trackHistory)
	{
		SetValue(value == null ? default : (T)value, trackHistory);
	}

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
	public bool IsChanged
	{
		get
		{
			lock (_sync)
			{
				return _historyCount > 0;
			}
		}
	}

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

	#region History

	/// <summary>
	/// 压入一条历史记录。
	/// </summary>
	/// <param name="value">旧值。</param>
	private void PushHistory(T value)
	{
		if (_historyDepth == 0)
		{
			return;
		}

		var capacity = EnsureHistoryCapacity(_historyCount + 1);
		if (_historyDepth > 0)
		{
			capacity = Math.Min(capacity, _historyDepth);
		}

		if (_historyCount < capacity)
		{
			_history[(_historyStart + _historyCount) % capacity] = value;
			_historyCount++;
			return;
		}

		// 已满：覆盖最旧一条，起点后移。
		_history[_historyStart] = value;
		_historyStart = (_historyStart + 1) % capacity;
	}

	/// <summary>
	/// 确保历史缓冲区至少能容纳 <paramref name="needed"/> 条记录。
	/// </summary>
	/// <param name="needed">需要的容量。</param>
	/// <returns>当前可用的历史容量。</returns>
	private int EnsureHistoryCapacity(int needed)
	{
		var limit = _historyDepth < 0 ? int.MaxValue : _historyDepth;

		if (_history == null)
		{
			var initial = Math.Min(4, limit);
			_history = new T[Math.Max(initial, 1)];
			_historyStart = 0;
			_historyCount = 0;
			return _history.Length;
		}

		if (needed <= _history.Length)
		{
			return _history.Length;
		}

		if (_history.Length >= limit)
		{
			return _history.Length;
		}

		var next = _history.Length;
		while (next < needed && next < limit)
		{
			next = next >= int.MaxValue / 2 ? int.MaxValue : next * 2;
		}

		next = Math.Min(next, limit);
		var replacement = new T[next];
		for (var index = 0; index < _historyCount; index++)
		{
			replacement[index] = _history[(_historyStart + index) % _history.Length];
		}

		_history = replacement;
		_historyStart = 0;
		return replacement.Length;
	}

	/// <summary>
	/// 在深度调小后丢弃最旧的多余记录。
	/// </summary>
	private void TrimToDepth()
	{
		if (_historyDepth < 0 || _history == null)
		{
			return;
		}

		if (_historyDepth == 0)
		{
			Array.Clear(_history, 0, _history.Length);
			_historyCount = 0;
			_historyStart = 0;
			return;
		}

		// 深度调小后把最近的记录压缩到长度为 depth 的新环里：
		// 否则 Push 用 min(length, depth) 取模、数组却仍是旧长度，读写会落到不同槽位。
		if (_history.Length <= _historyDepth && _historyCount <= _historyDepth)
		{
			return;
		}

		var keep = Math.Min(_historyCount, _historyDepth);
		var drop = _historyCount - keep;
		var replacement = new T[_historyDepth];
		for (var index = 0; index < keep; index++)
		{
			replacement[index] = _history[(_historyStart + drop + index) % _history.Length];
		}

		_history = replacement;
		_historyStart = 0;
		_historyCount = keep;
	}

	#endregion

	/// <summary>
	/// 按需创建的字段值可观察对象。
	/// </summary>
	private sealed class FieldObservable(FieldData<T> owner) : IObservable<T>
	{
		private readonly object _sync = new();
		private readonly List<IObserver<T>> _observers = [];

		public IDisposable Subscribe(IObserver<T> observer)
		{
			ArgumentNullException.ThrowIfNull(observer);

			lock (_sync)
			{
				_observers.Add(observer);
			}

			observer.OnNext(owner.Value);
			return new Subscription(this, observer);
		}

		public void Publish(T value)
		{
			IObserver<T>[] snapshot;
			lock (_sync)
			{
				if (_observers.Count == 0)
				{
					return;
				}

				snapshot = [.. _observers];
			}

			foreach (var observer in snapshot)
			{
				observer.OnNext(value);
			}
		}

		private void Unsubscribe(IObserver<T> observer)
		{
			lock (_sync)
			{
				_observers.Remove(observer);
			}
		}

		private sealed class Subscription(FieldObservable owner, IObserver<T> observer) : IDisposable
		{
			private int _disposed;

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) == 0)
				{
					owner.Unsubscribe(observer);
				}
			}
		}
	}
}
