using System;
using System.Windows.Input;

namespace AnakinRaW.CommonUtilities.Wpf.Input;

public class DelegateCommand(Action execute, Func<bool>? canExecute) : IDelegateCommand
{
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

    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool> _canExecute = canExecute ?? (() => true);
    private EventHandler? _canExecuteChanged;

    public ICommand Command => this;

    public DelegateCommand(Action execute)
        : this(execute, null)
    {
    }

    public bool CanExecute()
    {
        return _canExecute();
    }

    public void Execute()
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    bool ICommand.CanExecute(object? parameter)
    {
        return CanExecute();
    }

    void ICommand.Execute(object? parameter)
    {
        Execute();
    }
}