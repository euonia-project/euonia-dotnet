using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 表示可被验证的对象。
/// </summary>
/// <typeparam name="TValue">值类型。</typeparam>
public class ValidatableObject<TValue> : IValidatableObject, INotifyPropertyChanged
{
	/// <summary>
	/// 当属性值发生更改时触发。
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	#region Variables

	/// <summary>
	/// 被验证对象的值。
	/// </summary>
	private TValue _value = default!;

	/// <summary>
	/// 是否有效
	/// </summary>
	private bool _isValid = true;

	/// <summary>
	/// 规则集合
	/// </summary>
	private readonly List<IObjectValidator<TValue>> _rules = new();

	#endregion

	#region Properties

	/// <summary>
	/// 获取或设置值。
	/// </summary>
	/// <value>值。</value>
	public TValue Value
	{
		get => _value;
		set
		{
			if (EqualityComparer<TValue>.Default.Equals(_value, value))
			{
				return;
			}

			_value = value;

			RaisePropertyChanged(nameof(Value));
			ValueChangedCallback?.Invoke(Value);

			if (ValidateOnChanged)
			{
				Validate();
			}
		}
	}

	/// <summary>
	/// 如果对象有效则返回 true。
	/// </summary>
	/// <value>如果此实例有效则为 <c>true</c>；否则为 <c>false</c>。</value>
	public bool IsValid
	{
		get => _isValid;
		private set => SetProperty(ref _isValid, value);
	}

	/// <summary>
	/// 获取或设置一个值，指示值更改时是否调用 <see cref="Validate"/> 方法。
	/// </summary>
	public bool ValidateOnChanged { get; set; } = false;

	/// <summary>
	/// 获取错误集合。
	/// </summary>
	/// <value>错误集合。</value>
	public ObservableCollection<string> Errors { get; } = new();

	/// <summary>
	/// 获取规则集合。
	/// </summary>
	/// <value>规则集合。</value>
	public IReadOnlyList<IObjectValidator<TValue>> Rules => _rules;

	/// <summary>
	/// 获取或设置值更改回调。
	/// </summary>
	public Action<TValue>? ValueChangedCallback { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// 验证此实例。
	/// </summary>
	public void Validate()
	{
		Errors.Clear();
		if (Rules.Count == 0)
		{
			IsValid = true;
			return;
		}

		foreach (var rule in Rules)
		{
			var result = rule.Validate(Value);
			if (!result)
			{
				Errors.Add(rule.Message);
			}
		}

		// 校验通过 ⇔ 没有错误。此前写作 Errors.Count > 0，方向完全相反：
		// 合法对象（无错误）会被判定为无效并抛出空的 ValidationException，
		// 而非法对象（有错误）反而被放行——校验同时过度触发与失效。
		IsValid = Errors.Count == 0;
	}

	/// <summary>
	/// 使用指定的验证器。
	/// </summary>
	/// <param name="rule">规则。</param>
	/// <returns>ValidatableObject&lt;TValue&gt;。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="rule"/> 为 <see langword="null"/> 时抛出。</exception>
	public ValidatableObject<TValue> UseValidator(IObjectValidator<TValue> rule)
	{
		if (rule == null)
		{
			throw new ArgumentNullException(nameof(rule));
		}

		_rules.Add(rule);
		return this;
	}

	/// <summary>
	/// 触发属性更改事件。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	protected virtual void RaisePropertyChanged(string? propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// 设置属性值。
	/// </summary>
	/// <typeparam name="T">值类型。</typeparam>
	/// <param name="field">属性字段。</param>
	/// <param name="value">新的属性值。</param>
	/// <param name="propertyName">属性名称。</param>
	protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}

		field = value;
		RaisePropertyChanged(propertyName);
	}

	#endregion
}