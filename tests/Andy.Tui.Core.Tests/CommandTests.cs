using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Core.Bindings;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class CommandTests
{
    [Fact]
    public void RelayCommand_Executes_When_CanExecute_True()
    {
        int executed = 0;
        var cmd = new RelayCommand(_ => executed++);
        Assert.True(cmd.CanExecute());
        cmd.Execute();
        Assert.Equal(1, executed);
    }

    [Fact]
    public void RelayCommand_Respects_CanExecute()
    {
        int executed = 0;
        bool allow = false;
        var cmd = new RelayCommand(_ => executed++, _ => allow);
        Assert.False(cmd.CanExecute());
        cmd.Execute();
        Assert.Equal(0, executed);
        allow = true;
        Assert.True(cmd.CanExecute());
        cmd.Execute();
        Assert.Equal(1, executed);
    }

    [Fact]
    public async Task AsyncRelayCommand_Executes_Async()
    {
        int executed = 0;
        var cmd = new AsyncRelayCommand(async (_, ct) => { await Task.Delay(1, ct); executed++; });
        Assert.True(cmd.CanExecute());
        await cmd.ExecuteAsync();
        Assert.Equal(1, executed);
    }

    [Fact]
    public async Task AsyncRelayCommand_Respects_Cancellation()
    {
        int executed = 0;
        var cmd = new AsyncRelayCommand(async (_, ct) =>
        {
            await Task.Delay(50, ct);
            executed++;
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<TaskCanceledException>(async () => await cmd.ExecuteAsync(cancellationToken: cts.Token));
        Assert.Equal(0, executed);
    }

    [Fact]
    public void RelayCommand_RaiseCanExecuteChanged_Fires()
    {
        var cmd = new RelayCommand(_ => { }, _ => false);
        int raised = 0;
        cmd.CanExecuteChanged += (_, _) => raised++;
        cmd.RaiseCanExecuteChanged();
        Assert.Equal(1, raised);
    }

    [Fact]
    public void AsyncRelayCommand_RaiseCanExecuteChanged_Fires()
    {
        var cmd = new AsyncRelayCommand(async (_, ct) => { await Task.CompletedTask; }, _ => false);
        int raised = 0;
        cmd.CanExecuteChanged += (_, _) => raised++;
        cmd.RaiseCanExecuteChanged();
        Assert.Equal(1, raised);
    }
}
