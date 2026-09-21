using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// Represents the object can be validated.
/// </summary>
/// <typeparam name="TValue">The type of the t value.</typeparam>
public class ValidatableObject<TValue> : IValidatableObject, INotifyPropertyChanged
{
	/// <summary>
	/// Occurs when a property value changes.
	/// </summary>
	/// <returns></returns>
	public event PropertyChangedEventHandler PropertyChanged;

	#region Variables

	/// <summary>
	/// The value
	/// </summary>
	private TValue _value;

	/// <summary>
	/// The is valid
	/// </summary>
	private bool _isValid = true;

	/// <summary>
	/// The rules
	/// </summary>
	private readonly List<IObjectValidator<TValue>> _rules = new();

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the value.
	/// </summary>
	/// <value>The value.</value>
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
	/// Returns true if ... is valid.
	/// </summary>
	/// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
	public bool IsValid
	{
		get => _isValid;
		private set => SetProperty(ref _isValid, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the <see cref="Validate"/> method will be call while value changed or not.
	/// </summary>
	public bool ValidateOnChanged { get; set; } = false;

	/// <summary>
	/// Gets the errors.
	/// </summary>
	/// <value>The errors.</value>
	public ObservableCollection<string> Errors { get; } = new();

	/// <summary>
	/// Gets the rules.
	/// </summary>
	/// <value>The rules.</value>
	public IReadOnlyList<IObjectValidator<TValue>> Rules => _rules;

	/// <summary>
	/// Gets or sets the value changed callback.
	/// </summary>
	public Action<TValue> ValueChangedCallback { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Validates this instance.
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
	/// Withes the validator.
	/// </summary>
	/// <param name="rule">The rule.</param>
	/// <returns>ValidatableObject&lt;TValue&gt;.</returns>
	/// <exception cref="ArgumentNullException">rule</exception>
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
	/// Raises up the property changed event.
	/// </summary>
	/// <param name="propertyName">The property name.</param>
	protected virtual void RaisePropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Set property value.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="field">The property field.</param>
	/// <param name="value">The new property value.</param>
	/// <param name="propertyName">The property name.</param>
	protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
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