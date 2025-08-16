using System;
using System.Threading;
using System.Threading.Tasks;

namespace Andy.Tui.Core.Bindings;

public interface ICommand
{
    bool CanExecute(object? parameter = null);
    void Execute(object? parameter = null);
    event EventHandler? CanExecuteChanged;
}

public interface IAsyncCommand
{
    bool CanExecute(object? parameter = null);
    Task ExecuteAsync(object? parameter = null, CancellationToken cancellationToken = default);
    event EventHandler? CanExecuteChanged;
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter = null) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter = null)
    {
        if (CanExecute(parameter))
        {
            _execute(parameter);
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand : IAsyncCommand
{
    private readonly Func<object?, CancellationToken, Task> _executeAsync;
    private readonly Func<object?, bool>? _canExecute;

    public AsyncRelayCommand(Func<object?, CancellationToken, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter = null) => _canExecute?.Invoke(parameter) ?? true;

    public async Task ExecuteAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
        if (CanExecute(parameter))
        {
            await _executeAsync(parameter, cancellationToken).ConfigureAwait(false);
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
