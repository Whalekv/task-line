using System;
using System.Windows.Input;

namespace TaskLine.Commands;

/// <summary>
/// 泛型版 RelayCommand，支持带类型参数的命令执行逻辑。
/// 实现 <see cref="ICommand"/>，将执行委托与可执行条件委托封装为命令对象。
/// </summary>
/// <typeparam name="T">命令参数的类型。</typeparam>
public class RelayCommand<T> : ICommand
{
    #region 私有字段

    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="RelayCommand{T}"/> 的新实例。
    /// </summary>
    /// <param name="execute">命令执行时调用的委托。</param>
    /// <param name="canExecute">判断命令是否可执行的委托；为 <c>null</c> 时始终可执行。</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    #endregion

    #region ICommand

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => _execute((T?)parameter);

    #endregion
}

/// <summary>
/// 非泛型版 RelayCommand，适用于不需要类型化参数的命令场景。
/// 实现 <see cref="ICommand"/>，将执行委托与可执行条件委托封装为命令对象。
/// </summary>
public class RelayCommand : ICommand
{
    #region 私有字段

    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用带参数的委托初始化 <see cref="RelayCommand"/> 的新实例。
    /// </summary>
    /// <param name="execute">命令执行时调用的委托，接收命令参数。</param>
    /// <param name="canExecute">判断命令是否可执行的委托；为 <c>null</c> 时始终可执行。</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// 使用无参数委托初始化 <see cref="RelayCommand"/> 的新实例（便捷重载）。
    /// </summary>
    /// <param name="execute">命令执行时调用的无参数委托。</param>
    /// <param name="canExecute">判断命令是否可执行的无参数委托；为 <c>null</c> 时始终可执行。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    #endregion

    #region ICommand

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => _execute(parameter);

    #endregion
}
