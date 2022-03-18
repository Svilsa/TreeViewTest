using System;
using System.Windows.Input;

namespace TreeViewTest.Infrastructure;

public sealed class DelegateCommand : ICommand
{
    private readonly Action _execute;

    public DelegateCommand(Action execute)
    {
        _execute = execute;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute.Invoke();
    
    public event EventHandler? CanExecuteChanged;
}