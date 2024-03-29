using System;
using System.Windows.Input;

namespace AnakinRaW.CommonUtilities.Wpf.Input;

public class DelegateCommand<T>(Action<T> execute, Predicate<T?>? canExecute) : IDelegateCommand<T>
{
    private readonly Action<T> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private EventHandler? _canExecuteChanged;

    public DelegateCommand(Action<T?> execute)
        : this(execute, null)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            _canExecuteChanged += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
            _canExecuteChanged -= value;
        }
    }

    public void RaiseCanExecuteChanged()
    {
        _canExecuteChanged?.Invoke(this, null!);
    }

    public bool CanExecute(T? parameter)
    {
        return canExecute == null || canExecute(parameter);
    }

    public void Execute(T parameter)
    {
        _execute(parameter);
    }

    public ICommand Command => this;

    bool ICommand.CanExecute(object? parameter)
    {
        if (canExecute == null)
            return true;
        if (parameter == null)
            return CanExecute(default!);
        return parameter is T parameter1 ? CanExecute(parameter1) : throw new ArgumentException($"{nameof(parameter)} is not of type {typeof(T)}");
    }

    void ICommand.Execute(object? parameter)
    {
        if (parameter == null)
        {
            Execute(default!);
        }
        else
        {
            if (parameter is not T tType)
                throw new ArgumentException($"{nameof(parameter)} is not of type {typeof(T)}");
            Execute(tType);
        }
    }
}