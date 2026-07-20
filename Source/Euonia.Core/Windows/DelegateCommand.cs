using System.Windows.Input;

namespace Nerosoft.Euonia.Windows;

/// <summary>
/// 委托命令类（泛型版本）。
/// 实现 <see cref="ICommand" /> 接口
/// 实现 <see cref="System.Windows.Input.ICommand" /> 接口
/// </summary>
/// <typeparam name="T">命令参数的类型。</typeparam>
/// <seealso cref="System.Windows.Input.ICommand" />
/// <seealso cref="ICommand" />
public class DelegateCommand<T> : ICommand
{
    /// <summary>
    /// 用于判断命令是否可以执行的回调函数。
    /// </summary>
    private readonly Func<T, bool> _canExecute;

    /// <summary>
    /// 命令执行时要调用的操作。
    /// </summary>
    private readonly Action<T> _executeAction;

    /// <summary>
    /// 初始化 <see cref="DelegateCommand{T}" /> 类的新实例。
    /// </summary>
    /// <param name="executeAction">命令执行时要调用的操作。</param>
    /// <param name="canExecute">用于判断命令是否可以执行的回调函数。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="executeAction"/> 为 null 时抛出。</exception>
    public DelegateCommand(Action<T> executeAction, Func<T, bool> canExecute = null)
    {
        _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 定义用于确定命令在其当前状态下是否可以执行的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    /// <returns>如果此命令可以执行，则为 true；否则为 false。</returns>
    public bool CanExecute(object parameter)
    {
        var result = true;
        var canExecuteHandler = _canExecute;
        if (canExecuteHandler != null)
        {
            result = canExecuteHandler((T)parameter);
        }

        return result;
    }

    /// <summary>
    /// 当发生影响命令是否应执行的更改时发生。
    /// </summary>
    /// <returns>事件参数，包含与事件相关的数据。</returns>
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件以通知命令的可执行状态已更改。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 定义在调用命令时要调用的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    public void Execute(object parameter)
    {
        _executeAction((T)parameter);
    }
}

/// <summary>
/// 委托命令类（非泛型版本）。
/// 实现 <see cref="ICommand" /> 接口
/// 实现 <see cref="System.Windows.Input.ICommand" /> 接口
/// </summary>
/// <seealso cref="System.Windows.Input.ICommand" />
/// <seealso cref="ICommand" />
public class DelegateCommand : ICommand
{
    /// <summary>
    /// 用于判断命令是否可以执行的回调函数。
    /// </summary>
    private readonly Func<object, bool> _canExecute;

    /// <summary>
    /// 命令执行时要调用的操作。
    /// </summary>
    private readonly Action<object> _executeAction;

    /// <summary>
    /// 初始化 <see cref="DelegateCommand" /> 类的新实例。
    /// </summary>
    /// <param name="executeAction">命令执行时要调用的操作。</param>
    /// <param name="canExecute">用于判断命令是否可以执行的回调函数。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="executeAction"/> 为 null 时抛出。</exception>
    public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecute = null)
    {
        _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 定义用于确定命令在其当前状态下是否可以执行的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    /// <returns>如果此命令可以执行，则为 true；否则为 false。</returns>
    public bool CanExecute(object parameter)
    {
        var result = true;
        var canExecuteHandler = _canExecute;
        if (canExecuteHandler != null)
        {
            result = canExecuteHandler(parameter);
        }

        return result;
    }

    /// <summary>
    /// 当发生影响命令是否应执行的更改时发生。
    /// </summary>
    /// <returns>事件参数，包含与事件相关的数据。</returns>
    public event EventHandler CanExecuteChanged;

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件以通知命令的可执行状态已更改。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        var handler = CanExecuteChanged;
        handler?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 定义在调用命令时要调用的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    public void Execute(object parameter)
    {
        _executeAction(parameter);
    }
}
