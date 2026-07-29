using System.Windows.Input;

namespace Nerosoft.Euonia.Windows;

/// <summary>
/// 通用命令类，通过回调属性来定义命令的执行逻辑。
/// 实现 <see cref="ICommand" /> 接口
/// 实现 <see cref="System.Windows.Input.ICommand" /> 接口
/// </summary>
/// <seealso cref="System.Windows.Input.ICommand" />
/// <seealso cref="ICommand" />
public sealed class GenericCommand : ICommand
{
    /// <summary>
    /// 获取或设置用于判断命令是否可以执行的回调函数。
    /// </summary>
    /// <value>用于判断命令是否可以执行的回调函数。</value>
    public Func<object, bool> CanExecuteCallback { get; set; }

    /// <summary>
    /// 获取或设置命令执行时要调用的回调函数。
    /// </summary>
    /// <value>命令执行时要调用的回调函数。</value>
    public Action<object> ExecuteCallback { get; set; }

    #region ICommand Members

    /// <summary>
    /// 定义用于确定命令在其当前状态下是否可以执行的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    /// <returns>如果此命令可以执行，则为 true；否则为 false。</returns>
    public bool CanExecute(object parameter)
    {
        if (CanExecuteCallback != null)
        {
            return CanExecuteCallback(parameter);
        }

        return true;
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
        handler?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// 定义在调用命令时要调用的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    public void Execute(object parameter)
    {
        ExecuteCallback?.Invoke(parameter);
    }

    #endregion
}

/// <summary>
/// 通用命令类（泛型版本），通过回调属性来定义命令的执行逻辑。
/// </summary>
/// <typeparam name="T">命令参数的类型。</typeparam>
public sealed class GenericCommand<T> : ICommand
{
    /// <summary>
    /// 获取或设置用于判断命令是否可以执行的回调函数。
    /// </summary>
    /// <value>用于判断命令是否可以执行的回调函数。</value>
    public Func<T, bool> CanExecuteCallback { get; set; }

    /// <summary>
    /// 获取或设置命令执行时要调用的回调函数。
    /// </summary>
    /// <value>命令执行时要调用的回调函数。</value>
    public Action<T> ExecuteCallback { get; set; }

    #region ICommand Members

    /// <summary>
    /// 定义用于确定命令在其当前状态下是否可以执行的方法。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递数据，则可以将此对象设置为 null。</param>
    /// <returns>如果此命令可以执行，则为 true；否则为 false。</returns>
    public bool CanExecute(object parameter)
    {
        return CanExecuteCallback?.Invoke((T)parameter) ?? true;
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
        ExecuteCallback?.Invoke((T)parameter);
    }

    #endregion
}
