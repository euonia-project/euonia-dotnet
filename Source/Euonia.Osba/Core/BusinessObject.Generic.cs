namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 带变更追踪的业务对象基类。
/// </summary>
/// <remarks>
/// <para>
/// 变更追踪（已更改属性、脏状态、<see cref="AcceptChanges"/>）只属于<b>需要编辑/保存生命周期的对象</b>：
/// <see cref="ObservableObject{T}"/>、<see cref="EditableObject{T}"/>、<see cref="ReadOnlyObject{T}"/>。
/// </para>
/// <para>
/// <see cref="ServiceObject{T}"/> 直接继承 <see cref="ObjectBase{T}"/>，只保留属性与规则能力，
/// 不继承本类，因此没有 <see cref="ChangedProperties"/> / <see cref="HasChangedProperties"/> 的脏语义。
/// </para>
/// </remarks>
/// <typeparam name="T">业务对象的具体类型。</typeparam>
public abstract class BusinessObject<T> : ObjectBase<T>, IBusinessObject
	where T : BusinessObject<T>
{
	/// <summary>
	/// 已更改属性的列表。
	/// </summary>
	private readonly List<IPropertyInfo> _changedProperties = [];

	/// <summary>
	/// 已更改属性的只读快照；集合变化时失效。
	/// </summary>
	private IReadOnlyList<IPropertyInfo> _changedPropertiesSnapshot;

	/// <summary>
	/// 保护 <see cref="_changedProperties"/> 并发访问的同步锁。
	/// </summary>
	private readonly Lock _changedPropertiesLock = new();

	/// <summary>
	/// 将指定属性标记为脏的或已更改。
	/// </summary>
	/// <param name="property">要标记的属性信息。</param>
	protected override void PropertyHasChanged(IPropertyInfo property)
	{
		lock (_changedPropertiesLock)
		{
			if (!_changedProperties.Contains(property))
			{
				_changedProperties.Add(property);
				_changedPropertiesSnapshot = null;
			}
		}

		// 该属性没有规则时退化为普通变更通知——绑定依赖这条通知，
		// 不能因为「进了规则分支但无规则可跑」而丢掉它。
		if (CheckRuleOnPropertyChanged && HasAnyRulesFor(property))
		{
			CheckPropertyRules(property);
			SetValidationState(IsValid ? ValidationState.NotValidated : ValidationState.Invalid);
		}
		else
		{
			SetValidationState(ValidationState.NotValidated);
			OnPropertyChanged(property);
		}
	}

	/// <summary>
	/// 获取保存前需要补跑属性级规则的属性集合。
	/// </summary>
	protected override IReadOnlyList<IPropertyInfo> RulesPropertiesToCheck => ChangedProperties;

	/// <summary>
	/// 获取已更改属性的列表快照，可安全跨线程读取。
	/// </summary>
	public IReadOnlyList<IPropertyInfo> ChangedProperties
	{
		get
		{
			var snapshot = Volatile.Read(ref _changedPropertiesSnapshot);
			if (snapshot != null)
			{
				return snapshot;
			}

			lock (_changedPropertiesLock)
			{
				if (_changedPropertiesSnapshot == null)
				{
					_changedPropertiesSnapshot = _changedProperties.Count == 0
						                             ? Array.Empty<IPropertyInfo>()
						                             : [.. _changedProperties];
				}

				return _changedPropertiesSnapshot;
			}
		}
	}

	/// <summary>
	/// 检查对象是否具有已更改的属性。
	/// </summary>
	public bool HasChangedProperties
	{
		get
		{
			lock (_changedPropertiesLock)
			{
				return _changedProperties.Count > 0;
			}
		}
	}

	/// <summary>
	/// 提交当前更改：清除所有已更改属性的跟踪记录，并使字段的修改历史失效，
	/// 将字段当前值作为新的基线。
	/// </summary>
	/// <remarks>
	/// 通常在对象被成功保存或加载后调用，使对象恢复“干净”状态。
	/// </remarks>
	public override void AcceptChanges()
	{
		lock (_changedPropertiesLock)
		{
			_changedProperties.Clear();
			_changedPropertiesSnapshot = Array.Empty<IPropertyInfo>();
		}

		base.AcceptChanges();
	}
}
