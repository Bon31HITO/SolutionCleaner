using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SolutionCleaner;

internal abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed class RelayCommand : ICommand
{
    private readonly Action executeAction;
    private readonly Func<bool>? canExecutePredicate;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action executeAction, Func<bool>? canExecutePredicate = null)
    {
        this.executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        this.canExecutePredicate = canExecutePredicate;
    }

    public bool CanExecute(object? parameter)
    {
        return canExecutePredicate?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        executeAction();
    }
}
